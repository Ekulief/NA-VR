// functions/index.js — Firebase Cloud Functions v2 (CommonJS).
// Merge these exports into an existing index.js rather than replacing unrelated exports.
const { onCall, onRequest, HttpsError } = require("firebase-functions/v2/https");
const { onDocumentUpdated } = require("firebase-functions/v2/firestore");
const { defineSecret } = require("firebase-functions/params");
const { initializeApp, getApps } = require("firebase-admin/app");
const { getAuth } = require("firebase-admin/auth");
const { getFirestore, FieldValue } = require("firebase-admin/firestore");
const { AccessToken, RoomServiceClient } = require("livekit-server-sdk");
const crypto = require("node:crypto");

if (!getApps().length) initializeApp();
const db = getFirestore();
const adminAuth = getAuth();
const KEY = defineSecret("LIVEKIT_API_KEY");
const SECRET = defineSecret("LIVEKIT_API_SECRET");
const REGION = "us-central1";
const ACTIVE = new Set(["idle", "running", "paused"]);
const isId = v => typeof v === "string" && /^[A-Za-z0-9_-]{1,128}$/.test(v);
const bad = (code, msg) => { throw new HttpsError(code, msg); };
const roomNameFor = progressId => `slubt-progress-${progressId}`;

// SECTION A: Provisioned-device exchange. The shared secret is NEVER in Firestore
// vrDevices; hashes are kept in vrDeviceCredentials (server-only).
exports.deviceBootstrap = onRequest({ region: REGION, cors: false },
  async (req, res) => {
    res.set("Cache-Control", "no-store");
    if (req.method !== "POST") return res.status(405).send("POST required");
    const { vrDeviceId, deviceSecret } = req.body || {};
    if (!isId(vrDeviceId) || typeof deviceSecret !== "string" ||
        deviceSecret.length < 32 || deviceSecret.length > 256)
      return res.status(401).send("Unauthorized");
    try {
      const [deviceSnap, secretSnap] = await Promise.all([
        db.collection("vrDevices").doc(vrDeviceId).get(),
        db.collection("vrDeviceCredentials").doc(vrDeviceId).get()
      ]);
      if (!deviceSnap.exists || !secretSnap.exists ||
          secretSnap.data().enabled !== true ||
          deviceSnap.data().vrId !== vrDeviceId)
        return res.status(401).send("Unauthorized");
      const expectedHex = secretSnap.data().secretHash;
      if (typeof expectedHex !== "string" || !/^[0-9a-f]{64}$/.test(expectedHex))
        return res.status(401).send("Unauthorized");
      const supplied = crypto.createHash("sha256").update(deviceSecret, "utf8").digest();
      const expected = Buffer.from(expectedHex, "hex");
      if (!crypto.timingSafeEqual(supplied, expected))
        return res.status(401).send("Unauthorized");
      const customToken = await adminAuth.createCustomToken(`vrdevice_${vrDeviceId}`, {
        role: "vr_device", vrDeviceId,
        deviceAuthVersion: secretSnap.data().version
      });
      return res.status(200).json({ customToken });
    } catch (err) {
      console.error("Device bootstrap failed:", err?.code || err?.message);
      return res.status(500).send("Bootstrap failed");
    }
  }
);

// SECTION B: Authenticated student reserves a VR session. Use INSTEAD OF the
// current StudentExperimentRun.jsx browser transaction for secure assignment.
exports.reserveVrSession = onCall({ region: REGION }, async (request) => {
  if (!request.auth) bad("unauthenticated", "Student sign-in on the website required");
  if (request.auth.token.role === "vr_device")
    bad("permission-denied", "Device identities cannot reserve a student session");
  const uid = request.auth.uid;
  const { experimentId } = request.data || {};
  if (!isId(experimentId)) bad("invalid-argument", "experimentId required");
  const experimentSnap = await db.collection("experiment").doc(experimentId).get();
  if (!experimentSnap.exists) bad("not-found", "Experiment not found");
  const experiment = experimentSnap.data();
  if (!isId(experiment.blockId) || !isId(experiment.vrId))
    bad("failed-precondition", "Experiment needs a blockId and registered vrId");
  const [blockSnap, studentSnap] = await Promise.all([
    db.collection("block").doc(experiment.blockId).get(),
    db.collection("user").doc(uid).get()
  ]);
  if (!blockSnap.exists || !studentSnap.exists ||
      studentSnap.data().role !== "student" ||
      !(blockSnap.data().studentIds || []).includes(uid))
    bad("permission-denied", "Student not enrolled in this class");
  const allowedGroups = await Promise.all(
    (experiment.groupIds || []).filter(isId).map(id => db.collection("group").doc(id).get())
  );
  const group = allowedGroups.find(s => s.exists &&
    s.data().blockId === experiment.blockId &&
    (s.data().studentIds || []).includes(uid));
  if (!group) bad("permission-denied", "Student not assigned to an allowed experiment group");

  const deviceRef = db.collection("vrDevices").doc(experiment.vrId);
  const progressRef = db.collection("experimentProgress").doc();
  await db.runTransaction(async tx => {
    const deviceSnap = await tx.get(deviceRef);
    if (!deviceSnap.exists || deviceSnap.data().vrId !== experiment.vrId)
      bad("not-found", "VR device not registered");
    const currentId = deviceSnap.data().currentProgressId;
    if (currentId) {
      const current = await tx.get(db.collection("experimentProgress").doc(currentId));
      if (current.exists && current.data().completionStatus === "In Progress" &&
          current.data().sessionControl !== "ended")
        bad("failed-precondition", "VR device already assigned");
    }
    tx.create(progressRef, {
      blockId: experiment.blockId, experimentId, vrId: experiment.vrId,
      studentId: uid, groupId: group.id, sessionControl: "idle",
      completionStatus: "In Progress", elapsedSeconds: 0,
      createdAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp()
    });
    tx.update(deviceRef, {
      currentProgressId: progressRef.id, currentExperimentId: experimentId,
      currentBlockId: experiment.blockId, currentGroupId: group.id,
      currentUserId: uid, currentSceneName: "",
      status: "In Use", updatedAt: FieldValue.serverTimestamp()
    });
  });
  return { progressId: progressRef.id, vrId: experiment.vrId };
});

// SECTION B2: Authenticated release. Replaces browser writes to vrDevices.
exports.releaseVrSession = onCall({ region: REGION }, async request => {
  if (!request.auth) bad("unauthenticated", "Sign-in required");
  const { progressId } = request.data || {};
  if (!isId(progressId)) bad("invalid-argument", "progressId required");
  const ref = db.collection("experimentProgress").doc(progressId);
  const progress = await ref.get();
  if (!progress.exists) bad("not-found", "Session not found");
  const p = progress.data();
  const block = await db.collection("block").doc(p.blockId).get();
  const caller = request.auth.uid;
  if (request.auth.token.role === "vr_device" ||
      (caller !== p.studentId && (!block.exists || block.data().instructorId !== caller)))
    bad("permission-denied", "Only the assigned student or instructor may end");
  const deviceRef = db.collection("vrDevices").doc(p.vrId);
  await db.runTransaction(async tx => {
    const [latest, device] = await Promise.all([tx.get(ref), tx.get(deviceRef)]);
    if (!latest.exists || !device.exists ||
        device.data().currentProgressId !== progressId)
      bad("failed-precondition", "Session no longer owns this device");
    tx.update(ref, {
      sessionControl: "ended", completionStatus: "Completed",
      updatedAt: FieldValue.serverTimestamp()
    });
    tx.update(deviceRef, {
      currentProgressId: null, currentExperimentId: null, currentBlockId: null,
      currentGroupId: null, currentUserId: null, currentSceneName: "",
      status: "Available", updatedAt: FieldValue.serverTimestamp()
    });
  });
  return { released: true };
});

// SECTION C: One room PER progress/experiment session.
// DO NOT accept a caller-supplied room name or publisher/viewer "role".
exports.createLiveStreamToken = onCall(
  { region: REGION, secrets: [KEY, SECRET] },
  async (request) => {
    if (!request.auth) bad("unauthenticated", "Sign-in required");
    const { experimentId, progressId } = request.data || {};
    if (!isId(experimentId) || !isId(progressId))
      bad("invalid-argument", "experimentId and progressId required");
    const progressSnap = await db.collection("experimentProgress").doc(progressId).get();
    const experimentSnap = await db.collection("experiment").doc(experimentId).get();
    if (!progressSnap.exists || !experimentSnap.exists) bad("not-found", "Session or experiment missing");

    const progress = progressSnap.data();
    const experiment = experimentSnap.data();
    const blockId = progress.blockId;
    if (progress.experimentId !== experimentId ||
        experiment.blockId !== blockId ||
        progress.vrId !== experiment.vrId ||
        progress.completionStatus !== "In Progress" ||
        !ACTIVE.has(progress.sessionControl))
      bad("permission-denied", "Session and experiment do not match or are ended");

    const [blockSnap, deviceSnap, groupSnap] = await Promise.all([
      db.collection("block").doc(blockId).get(),
      db.collection("vrDevices").doc(progress.vrId).get(),
      db.collection("group").doc(progress.groupId).get()
    ]);
    if (!blockSnap.exists || !deviceSnap.exists || !groupSnap.exists)
      bad("failed-precondition", "Class, device, or group is missing");

    const block = blockSnap.data(), device = deviceSnap.data(), group = groupSnap.data();
    if (device.vrId !== progress.vrId ||
        device.currentProgressId !== progressId ||
        device.currentExperimentId !== experimentId ||
        device.currentBlockId !== blockId ||
        device.currentUserId !== progress.studentId ||
        device.currentGroupId !== progress.groupId ||
        device.status !== "In Use" ||
        group.blockId !== blockId ||
        !(group.studentIds || []).includes(progress.studentId) ||
        !(experiment.groupIds || []).includes(progress.groupId) ||
        !(block.studentIds || []).includes(progress.studentId))
      bad("permission-denied", "VR assignment is inconsistent");

    const uid = request.auth.uid;
    const isDevice = request.auth.token.role === "vr_device";
    let identity, publishing;
    if (isDevice) {
      const boundId = request.auth.token.vrDeviceId;
      if (boundId !== progress.vrId || uid !== `vrdevice_${boundId}`)
        bad("permission-denied", "Device identity does not match assigned VR");
      const cred = await db.collection("vrDeviceCredentials").doc(boundId).get();
      if (!cred.exists || cred.data().enabled !== true ||
          cred.data().version !== request.auth.token.deviceAuthVersion)
        bad("permission-denied", "Device identity disabled");
      identity = `vr-${boundId}`;
      publishing = true;
    } else {
      const profile = await db.collection("user").doc(uid).get();
      if (!profile.exists || profile.data().role !== "instructor" ||
          block.instructorId !== uid)
        bad("permission-denied", "Not the instructor for this selected experiment");
      // Unique per tab so multiple viewing tabs do not kick one another out.
      identity = `viewer-${crypto.randomBytes(12).toString("hex")}`;
      publishing = false;
    }

    const token = new AccessToken(KEY.value(), SECRET.value(),
      { identity, ttl: "5m" });
    token.addGrant({
      roomJoin: true, room: roomNameFor(progressId),
      canPublish: publishing, canSubscribe: !publishing,
      canPublishData: false,
      ...(publishing ? { canPublishSources: ["camera"] } : {})
    });
    return {
      serverUrl: process.env.LIVEKIT_URL,
      roomName: roomNameFor(progressId),
      token: await token.toJwt()
    };
  }
);

function roomAdmin() {
  return new RoomServiceClient(process.env.LIVEKIT_API_HOST, KEY.value(), SECRET.value());
}
async function closeRoom(progressId) {
  if (!isId(progressId)) return;
  try { await roomAdmin().deleteRoom(roomNameFor(progressId)); }
  catch (e) { // An already-empty / absent room is not a failure.
    console.warn("Room cleanup (possibly already absent)", progressId, e?.message);
  }
}

// SESSION END: forcibly kick already-connected viewers/publisher from that room.
// Expiring a token alone does NOT stop an existing stream.
exports.closeRoomOnProgressEnd = onDocumentUpdated(
  { document: "experimentProgress/{progressId}", region: REGION,
    secrets: [KEY, SECRET] },
  async (event) => {
    const before = event.data?.before?.data();
    const after = event.data?.after?.data();
    if (!before || !after) return;
    const wasActive = before.completionStatus === "In Progress" &&
      ACTIVE.has(before.sessionControl);
    const isActive = after.completionStatus === "In Progress" &&
      ACTIVE.has(after.sessionControl);
    if (wasActive && !isActive) await closeRoom(event.params.progressId);
  }
);

// If a headset is reassigned, its previous room is also closed.
exports.closeRoomOnDeviceReassignment = onDocumentUpdated(
  { document: "vrDevices/{vrDeviceId}", region: REGION,
    secrets: [KEY, SECRET] },
  async event => {
    const oldId = event.data?.before?.data()?.currentProgressId;
    const newId = event.data?.after?.data()?.currentProgressId;
    if (oldId && oldId !== newId) await closeRoom(oldId);
  }
);


// Credential rotation/disable: remove any already-connected publisher and viewers.
// This prevents a previously authenticated device from continuing an old room.
exports.closeRoomOnCredentialChange = onDocumentUpdated(
  { document: "vrDeviceCredentials/{vrDeviceId}", region: REGION,
    secrets: [KEY, SECRET] },
  async event => {
    const a = event.data?.before?.data();
    const b = event.data?.after?.data();
    if (!a || !b || (a.enabled === b.enabled && a.version === b.version)) return;
    const device = await db.collection("vrDevices").doc(event.params.vrDeviceId).get();
    const progressId = device.exists ? device.data().currentProgressId : "";
    if (progressId) await closeRoom(progressId);
  }
);
