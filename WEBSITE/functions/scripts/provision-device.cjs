// functions/scripts/provision-device.cjs
// Run on a trusted admin development machine with GOOGLE_APPLICATION_CREDENTIALS
// or properly authorized Application Default Credentials.
// Writes ONLY a hash to Firestore. Prints the secret ONCE to this terminal.
const { initializeApp, applicationDefault } = require("firebase-admin/app");
const { getFirestore, FieldValue } = require("firebase-admin/firestore");
const crypto = require("node:crypto");
const { getAuth } = require("firebase-admin/auth");
const vrId = process.argv[2];
if (!vrId || !/^[A-Za-z0-9_-]{1,128}$/.test(vrId)) {
  console.error("Usage: node scripts/provision-device.cjs VR-01");
  process.exit(1);
}
initializeApp({ credential: applicationDefault() });
(async () => {
  const db = getFirestore();
  const device = await db.collection("vrDevices").doc(vrId).get();
  if (!device.exists || device.data().vrId !== vrId)
    throw new Error(`vrDevices/${vrId} must exist with vrId="${vrId}"`);
  const raw = crypto.randomBytes(32).toString("base64url");
  const secretHash = crypto.createHash("sha256").update(raw).digest("hex");
  const version = crypto.randomBytes(16).toString("hex");
  await db.collection("vrDeviceCredentials").doc(vrId).set({
    enabled: true, secretHash, version, updatedAt: FieldValue.serverTimestamp()
  });
  try { await getAuth().revokeRefreshTokens(`vrdevice_${vrId}`); }
  catch (e) {
    if (e.code !== "auth/user-not-found") throw e;
  }
  console.log(`\nVR DEVICE ID: ${vrId}`);
  console.log(`DEVICE SECRET (copy securely; NOT stored by server): ${raw}\n`);
  console.log("Revokes the prior secret for this device; restart its Unity publisher.");
})().catch(e => { console.error(e); process.exit(1); });
