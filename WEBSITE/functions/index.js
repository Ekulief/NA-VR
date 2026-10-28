"use strict";

// SLUBT Labs development streaming: student-free VR, paired anonymous Firebase user.
// Pair a device's publisherAuthUid out-of-band from a trusted admin console.
// Never allow anonymous users to set their own publisherAuthUid or assignedInstructorId.
const { randomUUID } = require("node:crypto");
const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { defineSecret } = require("firebase-functions/params");
const { initializeApp } = require("firebase-admin/app");
const { getFirestore } = require("firebase-admin/firestore");
const { AccessToken } = require("livekit-server-sdk");

initializeApp();
const db = getFirestore();
const LIVEKIT_API_KEY = defineSecret("LIVEKIT_API_KEY");
const LIVEKIT_API_SECRET = defineSecret("LIVEKIT_API_SECRET");

function fail(code, message) { throw new HttpsError(code, message); }
function nonblank(value) { return typeof value === "string" && value.trim().length > 0; }

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

    // Current experiment builder saves a *single* vrId. A future builder can
    // write vrIds: ["VR-01", "VR-02"] to allow multiple assigned devices.
    const eligibleVrIds = Array.isArray(experiment.vrIds)
      ? experiment.vrIds
      : (nonblank(experiment.vrId) ? [experiment.vrId] : []);
    if (eligibleVrIds.length && !eligibleVrIds.includes(device.vrId)) {
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
        fail("permission-denied", "This anonymous Firebase user has not been paired with this VR device.");
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
