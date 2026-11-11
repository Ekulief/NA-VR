import express from "express";
import { adminAuth, adminDb, adminStorage } from "../config/firebaseAdmin.js";

const router = express.Router();

async function requireInstructor(req, res, next) {
  try {
    const authorization = req.headers.authorization || "";

    if (!authorization.startsWith("Bearer ")) {
      return res.status(401).json({ error: "Missing Firebase authentication token." });
    }

    const token = authorization.slice(7).trim();
    if (!token) {
      return res.status(401).json({ error: "Missing Firebase authentication token." });
    }

    const decodedToken = await adminAuth.verifyIdToken(token);
    const userSnapshot = await adminDb.collection("user").doc(decodedToken.uid).get();
    const role = String(userSnapshot.data()?.role || "").toLowerCase();

    if (!userSnapshot.exists || role !== "instructor") {
      return res.status(403).json({ error: "Instructor access required." });
    }

    req.user = decodedToken;
    return next();
  } catch (error) {
    console.error("Instructor authentication failed:", error);
    return res.status(401).json({ error: "Invalid or expired authentication token." });
  }
}

function getStorageObjectPath(storageUrl) {
  if (typeof storageUrl !== "string" || !storageUrl.trim()) return null;

  const value = storageUrl.trim();
  if (value.startsWith("gs://")) {
    const withoutScheme = value.slice(5);
    const slashIndex = withoutScheme.indexOf("/");
    return slashIndex >= 0 ? withoutScheme.slice(slashIndex + 1) : null;
  }

  try {
    const url = new URL(value);
    const marker = "/o/";
    const markerIndex = url.pathname.indexOf(marker);
    if (markerIndex < 0) return null;
    return decodeURIComponent(url.pathname.slice(markerIndex + marker.length));
  } catch {
    return null;
  }
}

// DELETE /api/courses/:blockId
// Deletes the course and its course-owned Firestore data. Student accounts
// are deleted only when they are not enrolled in another block/course.
router.delete("/:blockId", requireInstructor, async (req, res) => {
  try {
    const { blockId } = req.params;
    if (!blockId || blockId.includes("/")) {
      return res.status(400).json({ error: "Invalid course ID." });
    }

    const blockRef = adminDb.collection("block").doc(blockId);
    const blockSnapshot = await blockRef.get();

    if (!blockSnapshot.exists) {
      return res.status(404).json({ error: "Course not found." });
    }

    const blockData = blockSnapshot.data() || {};
    const ownerFields = ["instructorId", "ownerId", "createdBy"];
    const ownerField = ownerFields.find(
      (field) => blockData[field] !== undefined && blockData[field] !== null && blockData[field] !== ""
    );

    // Fail closed: do not delete a course whose ownership cannot be verified.
    if (!ownerField || blockData[ownerField] !== req.user.uid) {
      return res.status(403).json({ error: "You are not authorized to delete this course." });
    }

    const studentIds = Array.isArray(blockData.studentIds)
      ? [...new Set(blockData.studentIds.filter((id) => typeof id === "string" && id.trim()))]
      : [];

    // Determine which enrolled students still belong to another course.
    const allBlocksSnapshot = await adminDb.collection("block").get();
    const otherCourseStudentIds = new Set();
    allBlocksSnapshot.docs.forEach((courseDoc) => {
      if (courseDoc.id === blockId) return;
      const ids = courseDoc.data()?.studentIds;
      if (Array.isArray(ids)) ids.forEach((id) => otherCourseStudentIds.add(id));
    });

    const retainedStudentIds = studentIds.filter((uid) => otherCourseStudentIds.has(uid));
    const candidateStudentIds = studentIds.filter((uid) => !otherCourseStudentIds.has(uid));

    // Find all top-level documents explicitly associated with this block.
    // This includes experiment/group documents and any result collection that
    // stores blockId, without guessing collection names.
    const topLevelCollections = await adminDb.listCollections();
    const eligibleCollections = topLevelCollections.filter(
      (collectionRef) => collectionRef.id !== "block" && collectionRef.id !== "user"
    );
    const courseOwnedDocumentRefs = new Map();
    const experimentIds = new Set();
    const groupIds = new Set();

    await Promise.all(
      eligibleCollections.map(async (collectionRef) => {
        const matchingDocs = await collectionRef.where("blockId", "==", blockId).get();
        matchingDocs.docs.forEach((document) => {
          courseOwnedDocumentRefs.set(document.ref.path, document.ref);
          if (collectionRef.id === "experiment") experimentIds.add(document.id);
          if (collectionRef.id === "group") groupIds.add(document.id);
        });
      })
    );

    // Also include top-level result/related documents linked by experimentId or
    // groupId instead of blockId. Firestore `in` queries accept up to 30 values.
    const queryByRelatedIds = async (collectionRef, fieldName, ids) => {
      const values = [...ids];
      for (let index = 0; index < values.length; index += 30) {
        const chunk = values.slice(index, index + 30);
        const snapshot = await collectionRef.where(fieldName, "in", chunk).get();
        snapshot.docs.forEach((document) => {
          courseOwnedDocumentRefs.set(document.ref.path, document.ref);
        });
      }
    };

    await Promise.all(
      eligibleCollections.map(async (collectionRef) => {
        await Promise.all([
          queryByRelatedIds(collectionRef, "experimentId", experimentIds),
          queryByRelatedIds(collectionRef, "groupId", groupIds),
        ]);
      })
    );

    const relatedDocumentRefs = [...courseOwnedDocumentRefs.values()];

    // Recursively delete matched documents so their subcollections are removed too.
    await Promise.all(
      relatedDocumentRefs.map((documentRef) => adminDb.recursiveDelete(documentRef))
    );

    // Recursively remove the block itself, including any subcollections.
    await adminDb.recursiveDelete(blockRef);

    // Delete Auth accounts and profiles only for students exclusive to this course.
    let deletedStudentAccounts = 0;
    let skippedMissingProfiles = 0;
    const accountDeletionErrors = [];

    for (const uid of candidateStudentIds) {
      const profileRef = adminDb.collection("user").doc(uid);
      const profileSnapshot = await profileRef.get();

      // Do not delete an account unless its Firestore profile identifies it as a student.
      if (!profileSnapshot.exists || String(profileSnapshot.data()?.role || "").toLowerCase() !== "student") {
        skippedMissingProfiles += 1;
        continue;
      }

      try {
        await adminAuth.deleteUser(uid);
        await profileRef.delete();
        deletedStudentAccounts += 1;
      } catch (error) {
        // If Auth account is already gone, still remove its orphaned student profile.
        if (error.code === "auth/user-not-found") {
          await profileRef.delete();
          deletedStudentAccounts += 1;
        } else {
          console.error(`Unable to delete student account ${uid}:`, error);
          accountDeletionErrors.push(uid);
        }
      }
    }

    // Delete the course image if it is stored in the configured Firebase bucket.
    let courseImageDeleted = false;
    const imagePath = getStorageObjectPath(blockData.imageUrl);
    if (imagePath && adminStorage) {
      try {
        await adminStorage.bucket().file(imagePath).delete();
        courseImageDeleted = true;
      } catch (storageError) {
        if (storageError.code !== 404) {
          console.warn("Unable to delete course image from Firebase Storage:", storageError);
        }
      }
    }

    return res.json({
      message: "Course deletion completed.",
      deleted: {
        course: true,
        relatedDocuments: relatedDocumentRefs.length,
        studentAccounts: deletedStudentAccounts,
        courseImage: courseImageDeleted,
      },
      retainedStudentAccounts: retainedStudentIds.length,
      skippedStudentProfiles: skippedMissingProfiles,
      accountDeletionErrors,
    });
  } catch (error) {
    console.error("Course deletion failed:", error);
    return res.status(500).json({
      error: "Unable to completely delete this course. Check the server logs for details.",
    });
  }
});

export default router;
