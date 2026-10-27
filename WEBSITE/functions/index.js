
const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { defineSecret } = require("firebase-functions/params");
const { initializeApp } = require("firebase-admin/app");
const { getFirestore } = require("firebase-admin/firestore");
const { AccessToken } = require("livekit-server-sdk");

initializeApp();

const db = getFirestore();

const LIVEKIT_API_KEY = defineSecret("LIVEKIT_API_KEY");
const LIVEKIT_API_SECRET = defineSecret("LIVEKIT_API_SECRET");

// Returns a student profile document if the Firebase Auth UID is either
// the user document ID or is stored in that document's authUid field.
async function findUserByAuthUid(uid) {
  const direct = await db.collection("user").doc(uid).get();

  if (direct.exists) {
    return direct;
  }

  const matches = await db
    .collection("user")
    .where("authUid", "==", uid)
    .limit(1)
    .get();

  return matches.empty ? null : matches.docs[0];
}

exports.createLiveStreamToken = onCall(
  {
    secrets: [LIVEKIT_API_KEY, LIVEKIT_API_SECRET],
  },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError(
        "unauthenticated",
        "You must sign in first."
      );
    }

    const uid = request.auth.uid;
    const { blockId, vrDeviceId, role } = request.data || {};

    if (
      typeof blockId !== "string" ||
      typeof vrDeviceId !== "string" ||
      !["publisher", "viewer"].includes(role)
    ) {
      throw new HttpsError(
        "invalid-argument",
        "blockId, vrDeviceId, and a valid role are required."
      );
    }

    const blockRef = db.collection("block").doc(blockId);
    const deviceRef = db.collection("vrDevices").doc(vrDeviceId);

    const [blockSnap, deviceSnap] = await Promise.all([
      blockRef.get(),
      deviceRef.get(),
    ]);

    if (!blockSnap.exists || !deviceSnap.exists) {
      throw new HttpsError(
        "not-found",
        "The class or VR device was not found."
      );
    }

    const block = blockSnap.data();
    const device = deviceSnap.data();

    // Only active devices assigned to this class can be streamed.
    const active =
      device.currentBlockId === blockId &&
      String(device.status || "").toLowerCase() === "in use" &&
      Boolean(device.currentUserId);

    if (!active) {
      throw new HttpsError(
        "failed-precondition",
        "This VR device does not have an active session in this class."
      );
    }

    const studentDocumentId = device.currentUserId;
    const classStudentIds = Array.isArray(block.studentIds)
      ? block.studentIds
      : [];

    if (!classStudentIds.includes(studentDocumentId)) {
      throw new HttpsError(
        "permission-denied",
        "The active student is not enrolled in this class."
      );
    }

    let identity;

    if (role === "viewer") {
      // Instructors may view only classes they own.
      if (block.instructorId !== uid) {
        throw new HttpsError(
          "permission-denied",
          "You do not own this class."
        );
      }

      identity = `instructor-${uid}`;
    } else {
      // A publisher must be the authenticated student assigned to this device.
      const userSnap = await findUserByAuthUid(uid);

      if (!userSnap || userSnap.id !== studentDocumentId) {
        throw new HttpsError(
          "permission-denied",
          "This account is not the student assigned to the VR device."
        );
      }

      const userData = userSnap.data();

      if (
        userData.role &&
        String(userData.role).toLowerCase() !== "student"
      ) {
        throw new HttpsError(
          "permission-denied",
          "Only student accounts can publish a VR stream."
        );
      }

      identity = `vr-${vrDeviceId}`;
    }

    // Both Unity and the instructor page use this exact room name.
    const roomName = `slubt-${blockId}-${vrDeviceId}`;

    const token = new AccessToken(
      LIVEKIT_API_KEY.value(),
      LIVEKIT_API_SECRET.value(),
      {
        identity,
        ttl: "10m",
      }
    );

    token.addGrant({
      roomJoin: true,
      room: roomName,
      canPublish: role === "publisher",
      canSubscribe: role === "viewer",
      canPublishData: false,
    });

    return {
      serverUrl: process.env.LIVEKIT_URL,
      roomName,
      token: await token.toJwt(),
    };
  }
);
