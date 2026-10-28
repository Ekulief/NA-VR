"use strict";

// SLUBT Labs VR LiveKit streams. Unity registers its anonymous publisher UID at
// startup through a callable function; direct client writes to this field are unnecessary.
// IMPORTANT: deviceId is NOT a cryptographic credential. See README before production.
const { randomUUID, createHash } = require("node:crypto");
const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { defineSecret } = require("firebase-functions/params");
const { initializeApp } = require("firebase-admin/app");
const { getFirestore, FieldValue } = require("firebase-admin/firestore");
const { AccessToken } = require("livekit-server-sdk");

initializeApp();
const db = getFirestore();
const LIVEKIT_API_KEY = defineSecret("LIVEKIT_API_KEY");
const LIVEKIT_API_SECRET = defineSecret("LIVEKIT_API_SECRET");

function fail(code, message) { throw new HttpsError(code, message); }
function nonblank(value) { return typeof value === "string" && value.trim().length > 0; }

/**
 * Called automatically by Unity after anonymous Firebase authentication.
 * Reuses the inventory record matching hardware deviceId, or creates an
 * automatic VR-A-xxxxxx record for a new headset. No admin UID pairing.
 * An active stream cannot be stolen by a different anonymous UID.
 * For production, combine with App Check / hardware-backed attestation.
 */
exports.registerVrPublisher = onCall(async (request) => {
  if (!request.auth) fail("unauthenticated", "Firebase authentication is required.");
  const provider = request.auth.token?.firebase?.sign_in_provider;
  if (provider !== "anonymous") {
    fail("permission-denied", "Only the VR anonymous sign-in may register as a publisher.");
  }
  const deviceId = request.data?.deviceId;
  if (!nonblank(deviceId) || deviceId.length > 256) {
    fail("invalid-argument", "A nonempty hardware deviceId is required.");
  }
  // deviceId comes from Unity SystemInfo.deviceUniqueIdentifier.
  const matches = await db.collection("vrDevices")
    .where("deviceId", "==", deviceId).limit(2).get();
  if (matches.size > 1) {
    fail("failed-precondition", "Duplicate vrDevices rows have the same hardware deviceId.");
  }
  // Stable auto ID prevents duplicate records when two startup calls overlap.
  const digest = createHash("sha256").update(deviceId).digest("hex");
  const autoDocId = `auto-${digest.slice(0, 24)}`;
  const autoVrId = `VR-A-${digest.slice(0, 8).toUpperCase()}`;
  const deviceRef = matches.empty
    ? db.collection("vrDevices").doc(autoDocId)
    : matches.docs[0].ref;
  const uid = request.auth.uid;
  const result = await db.runTransaction(async (tx) => {
    const snap = await tx.get(deviceRef);
    if (!snap.exists) {
      tx.create(deviceRef, {
        deviceId,
        vrId: autoVrId,
        status: "Available",
        publisherAuthUid: uid,
        publisherRegisteredAt: FieldValue.serverTimestamp(),
        createdAt: FieldValue.serverTimestamp(),
        updatedAt: FieldValue.serverTimestamp(),
      });
      return { vrDeviceDocumentId: deviceRef.id, vrId: autoVrId };
    }
    if (snap.get("deviceId") !== deviceId || !nonblank(snap.get("vrId"))) {
      fail("failed-precondition", "VR inventory identity is incomplete or changed.");
    }
    const currentUid = snap.get("publisherAuthUid");
    if (nonblank(currentUid) && currentUid !== uid &&
        snap.get("status") === "In Use" && nonblank(snap.get("currentProgressId"))) {
      fail("failed-precondition", "A different publisher owns an active session on this headset. End the session first.");
    }
    tx.update(deviceRef, {
      publisherAuthUid: uid,
      publisherRegisteredAt: FieldValue.serverTimestamp(),
    });
    return { vrDeviceDocumentId: snap.id, vrId: snap.get("vrId") };
  });
  return result;
});

/**
 * Secure student connection: one transaction creates progress and assigns the
 * VR's block/instructor. Students cannot choose assignedInstructorId themselves.
 */
exports.startVrExperimentSession = onCall(async (request) => {
  if (!request.auth || request.auth.token?.firebase?.sign_in_provider === "anonymous") {
    fail("permission-denied", "A signed-in student is required.");
  }
  const { blockId, experimentId, groupId, vrId, participantCode, attemptNumber } = request.data || {};
  if (![blockId, experimentId, groupId, vrId, participantCode].every(nonblank) ||
      !Number.isInteger(attemptNumber) || attemptNumber < 1 || attemptNumber > 10000 ||
      participantCode.length > 100) {
    fail("invalid-argument", "Invalid experiment connection details.");
  }
  const uid = request.auth.uid;
  const [blockSnap, expSnap, groupSnap, deviceQuery] = await Promise.all([
    db.collection("block").doc(blockId).get(),
    db.collection("experiment").doc(experimentId).get(),
    db.collection("group").doc(groupId).get(),
    db.collection("vrDevices").where("vrId", "==", vrId).limit(2).get(),
  ]);
  if (!blockSnap.exists || !expSnap.exists || !groupSnap.exists || deviceQuery.size !== 1) {
    fail("failed-precondition", "Missing or ambiguous course, group, experiment, or VR device.");
  }
  const block = blockSnap.data();
  const experiment = expSnap.data();
  const group = groupSnap.data();
  const userSnap = await db.collection("user").doc(uid).get();
  if (!userSnap.exists || userSnap.get("role") !== "student" ||
      !Array.isArray(block.studentIds) || !block.studentIds.includes(uid) ||
      !nonblank(block.instructorId) ||
      experiment.blockId !== blockId || group.blockId !== blockId ||
      !Array.isArray(experiment.groupIds) || !experiment.groupIds.includes(groupId) ||
      !Array.isArray(group.studentIds) || !group.studentIds.includes(uid)) {
    fail("permission-denied", "Student, group, course or experiment assignment is invalid.");
  }
  const eligibleVrIds = Array.isArray(experiment.vrIds) && experiment.vrIds.length
    ? experiment.vrIds : (nonblank(experiment.vrId) ? [experiment.vrId] : []);
  if (!eligibleVrIds.includes(vrId)) {
    fail("permission-denied", "This VR device is not assigned to the experiment.");
  }

  const deviceRef = deviceQuery.docs[0].ref;
  const progressRef = db.collection("experimentProgress").doc();
  return db.runTransaction(async (tx) => {
    const [deviceSnap, latestBlock, latestExp, latestGroup] = await Promise.all([
      tx.get(deviceRef), tx.get(blockSnap.ref), tx.get(expSnap.ref), tx.get(groupSnap.ref),
    ]);
    if (!deviceSnap.exists || !latestBlock.exists || !latestExp.exists || !latestGroup.exists ||
        deviceSnap.get("vrId") !== vrId ||
        latestBlock.get("instructorId") !== block.instructorId ||
        !latestBlock.get("studentIds")?.includes(uid) ||
        latestExp.get("blockId") !== blockId ||
        !latestExp.get("groupIds")?.includes(groupId) ||
        !latestGroup.get("studentIds")?.includes(uid)) {
      fail("failed-precondition", "Course or VR assignment changed. Retry.");
    }
    const device = deviceSnap.data();
    if (nonblank(device.currentProgressId)) {
      const prior = await tx.get(db.collection("experimentProgress").doc(device.currentProgressId));
      if (prior.exists && prior.get("completionStatus") === "In Progress" &&
          prior.get("sessionControl") !== "ended") {
        fail("already-exists", "This VR headset is currently in use.");
      }
    }
    tx.create(progressRef, {
      blockId, experimentId, studentId: uid, groupId, vrId,
      participantCode, attemptNumber,
      sessionControl: "idle", completionStatus: "In Progress", elapsedSeconds: 0,
      createdAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp(),
    });
    tx.update(deviceRef, {
      currentProgressId: progressRef.id,
      currentExperimentId: experimentId,
      currentGroupId: groupId,
      currentUserId: uid,
      assignedInstructorId: block.instructorId,
      currentBlockId: blockId,
      currentSceneName: "",
      currentModuleId: "",
      streamRoomName: `slubt-${blockId}-${deviceRef.id}`,
      status: "In Use",
      updatedAt: FieldValue.serverTimestamp(),
    });
    return { progressId: progressRef.id, vrDeviceId: deviceRef.id };
  });
});

/** Releases only the caller's completed, pinned attempt; not any other user's VR. */
exports.releaseVrExperimentSession = onCall(async (request) => {
  if (!request.auth || request.auth.token?.firebase?.sign_in_provider === "anonymous") {
    fail("permission-denied", "A signed-in student is required.");
  }
  const progressId = request.data?.progressId;
  if (!nonblank(progressId)) fail("invalid-argument", "progressId is required.");
  const progressRef = db.collection("experimentProgress").doc(progressId);
  const progressSnap = await progressRef.get();
  if (!progressSnap.exists || progressSnap.get("studentId") !== request.auth.uid) {
    fail("permission-denied", "The experiment attempt does not belong to this student.");
  }
  const vrId = progressSnap.get("vrId");
  const matching = await db.collection("vrDevices").where("vrId", "==", vrId).limit(2).get();
  if (matching.size !== 1) fail("failed-precondition", "VR device could not be resolved.");
  const deviceRef = matching.docs[0].ref;
  return db.runTransaction(async (tx) => {
    const [progress, device] = await Promise.all([tx.get(progressRef), tx.get(deviceRef)]);
    if (!progress.exists || progress.get("studentId") !== request.auth.uid) {
      fail("permission-denied", "Session ownership changed.");
    }
    if (progress.get("sessionControl") !== "ended" && progress.get("completionStatus") !== "Completed") {
      fail("failed-precondition", "End this attempt before releasing the device.");
    }
    if (device.get("currentProgressId") !== progressId) return { released: false };
    tx.update(deviceRef, {
      currentProgressId: null,
      currentExperimentId: null,
      currentGroupId: null,
      currentUserId: null,
      currentBlockId: null,
      currentSceneName: "",
      currentModuleId: "",
      streamRoomName: "",
      status: "Available",
      updatedAt: FieldValue.serverTimestamp(),
    });
    return { released: true };
  });
});

/** Headset-owned scene status, without anonymous Firestore write privileges. */
/**
 * Instructor-facing inventory list. Return only safe display fields, not hardware
 * identifiers or anonymous publisher UIDs. A previously unassigned headset is
 * selectable before the first student activation.
 */
exports.listVrDevicesForInstructor = onCall(async (request) => {
  if (!request.auth || request.auth.token?.firebase?.sign_in_provider === "anonymous") {
    fail("permission-denied", "Sign in with an instructor account.");
  }
  const profile = await db.collection("user").doc(request.auth.uid).get();
  if (!profile.exists || profile.get("role") !== "instructor") {
    fail("permission-denied", "Only instructors can list available VR devices.");
  }
  const snapshot = await db.collection("vrDevices").limit(200).get();
  const devices = snapshot.docs.map((d) => {
    const data = d.data();
    if (!nonblank(data.vrId) || !nonblank(data.deviceId)) return null;
    if (nonblank(data.assignedInstructorId) &&
        data.assignedInstructorId !== request.auth.uid && data.status === "In Use") {
      return null;
    }
    return {
      id: d.id,
      vrId: data.vrId,
      status: data.status || "Available",
    };
  }).filter(Boolean);
  return { devices };
});

exports.setVrExperimentSceneStatus = onCall(async (request) => {
  if (!request.auth || request.auth.token?.firebase?.sign_in_provider !== "anonymous") {
    fail("permission-denied", "Only a registered Unity publisher may update VR scene status.");
  }
  const { vrDeviceId, status, experimentId, progressId, blockId, sceneName } = request.data || {};
  if (!nonblank(vrDeviceId) || !["In Use", "Available"].includes(status)) {
    fail("invalid-argument", "VR device ID and valid status are required.");
  }
  if (status === "In Use" &&
      (![blockId, experimentId, progressId].every(nonblank) ||
       typeof sceneName !== "string" || sceneName.length > 160)) {
    fail("invalid-argument", "An active scene requires matching session identifiers.");
  }
  const deviceRef = db.collection("vrDevices").doc(vrDeviceId);
  return db.runTransaction(async (tx) => {
    const deviceSnap = await tx.get(deviceRef);
    if (!deviceSnap.exists || deviceSnap.get("publisherAuthUid") !== request.auth.uid) {
      fail("permission-denied", "This Unity publisher does not own the VR device.");
    }
    if (status === "Available") {
      tx.update(deviceRef, {
        status: "Available", currentSceneName: "", currentModuleId: "",
        streamRoomName: "", currentSessionStart: FieldValue.delete(),
        updatedAt: FieldValue.serverTimestamp(),
      });
      return { updated: true };
    }
    if (deviceSnap.get("currentBlockId") !== blockId ||
        deviceSnap.get("currentExperimentId") !== experimentId ||
        deviceSnap.get("currentProgressId") !== progressId) {
      fail("failed-precondition", "Web-pinned VR experiment session does not match the headset.");
    }
    const [progress, experiment] = await Promise.all([
      tx.get(db.collection("experimentProgress").doc(progressId)),
      tx.get(db.collection("experiment").doc(experimentId)),
    ]);
    if (!progress.exists || !experiment.exists ||
        experiment.get("blockId") !== blockId ||
        (progress.get("experimentId") || progress.get("exeprimentId")) !== experimentId ||
        progress.get("blockId") !== blockId ||
        progress.get("vrId") !== deviceSnap.get("vrId") ||
        progress.get("sessionControl") === "ended" ||
        progress.get("completionStatus") === "Completed") {
      fail("failed-precondition", "The experiment progress is missing, mismatched or completed.");
    }
    tx.update(deviceRef, {
      status: "In Use", currentSceneName: sceneName,
      currentModuleId: experiment.get("moduleId") || "",
      streamRoomName: `slubt-${blockId}-${vrDeviceId}`,
      currentSessionStart: FieldValue.serverTimestamp(),
      updatedAt: FieldValue.serverTimestamp(),
    });
    return { updated: true };
  });
});

exports.createLiveStreamToken = onCall(
  { secrets: [LIVEKIT_API_KEY, LIVEKIT_API_SECRET] },
  async (request) => {
    if (!request.auth) fail("unauthenticated", "Sign in with Firebase before requesting a stream token.");

    const { blockId, vrDeviceId, experimentId, role } = request.data || {};
    if (![blockId, vrDeviceId, experimentId].every(nonblank) ||
        !["publisher", "viewer"].includes(role)) {
      fail("invalid-argument", "blockId, vrDeviceId, experimentId and publisher/viewer role are required.");
    }

    const [blockSnap, deviceSnap, experimentSnap] = await Promise.all([
      db.collection("block").doc(blockId).get(),
      db.collection("vrDevices").doc(vrDeviceId).get(),
      db.collection("experiment").doc(experimentId).get(),
    ]);
    if (!blockSnap.exists || !deviceSnap.exists || !experimentSnap.exists) {
      fail("not-found", "Class, VR device, or experiment does not exist.");
    }

    const block = blockSnap.data();
    const device = deviceSnap.data();
    const experiment = experimentSnap.data();
    const owner = block.instructorId;

    if (!nonblank(owner) || device.assignedInstructorId !== owner ||
        experiment.blockId !== blockId ||
        device.currentBlockId !== blockId ||
        device.currentExperimentId !== experimentId ||
        device.status !== "In Use" ||
        !nonblank(device.currentProgressId)) {
      fail("failed-precondition", "VR session is inactive or does not belong to this instructor and experiment.");
    }

    // Support the builder's multi-device vrIds and legacy single vrId.
    const eligibleVrIds = Array.isArray(experiment.vrIds) && experiment.vrIds.length
      ? experiment.vrIds
      : (nonblank(experiment.vrId) ? [experiment.vrId] : []);
    if (!eligibleVrIds.includes(device.vrId)) {
      fail("permission-denied", "This VR device is not assigned to the selected experiment.");
    }

    const roomName = `slubt-${blockId}-${vrDeviceId}`;
    if (device.streamRoomName !== roomName) {
      fail("failed-precondition", "The device stream room does not match its registered session.");
    }

    // Verify session metadata server-side; never rely on UI-only filters.
    const progressSnap = await db.collection("experimentProgress").doc(device.currentProgressId).get();
    if (!progressSnap.exists) fail("failed-precondition", "Active experiment progress was not found.");
    const progress = progressSnap.data();
    const activeExperiment = progress.experimentId || progress.exeprimentId;
    if (activeExperiment !== experimentId ||
        (nonblank(progress.blockId) && progress.blockId !== blockId) ||
        (nonblank(progress.vrId) && progress.vrId !== device.vrId) ||
        progress.sessionControl === "ended" ||
        progress.completionStatus === "Completed") {
      fail("failed-precondition", "Progress does not match the device's active experiment.");
    }

    const uid = request.auth.uid;
    const provider = request.auth.token?.firebase?.sign_in_provider;
    let identity;
    if (role === "viewer") {
      if (provider === "anonymous" || uid !== owner) {
        fail("permission-denied", "Only the signed-in instructor who owns this class can view this stream.");
      }
      // Unique per browser tab avoids replacing another instructor connection.
      identity = `instructor-${uid}-${randomUUID()}`;
    } else {
      if (provider !== "anonymous" || device.publisherAuthUid !== uid) {
        fail("permission-denied", "This Unity publisher is not the registered anonymous session for the headset.");
      }
      identity = `vr-${vrDeviceId}`;
    }

    const token = new AccessToken(LIVEKIT_API_KEY.value(), LIVEKIT_API_SECRET.value(), {
      identity,
      ttl: "10m", // Token expires for NEW joins; existing connections may persist.
    });
    token.addGrant({
      roomJoin: true, room: roomName,
      canPublish: role === "publisher",
      canSubscribe: role === "viewer",
      canPublishData: false,
    });

    if (!nonblank(process.env.LIVEKIT_URL)) {
      fail("failed-precondition", "LIVEKIT_URL is missing from the Functions environment.");
    }
    return { serverUrl: process.env.LIVEKIT_URL, roomName, token: await token.toJwt() };
  }
);
