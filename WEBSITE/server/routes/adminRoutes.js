import express from "express";
import { adminAuth, adminDb } from "../config/firebaseAdmin.js";

const router = express.Router();

async function requireAdmin(req, res, next) {
  try {
    const authorization = req.headers.authorization || "";

    if (!authorization.startsWith("Bearer ")) {
      return res.status(401).json({
        error: "Missing Firebase authentication token.",
      });
    }

    const token = authorization.slice(7).trim();

    if (!token) {
      return res.status(401).json({
        error: "Missing Firebase authentication token.",
      });
    }

    const decodedToken = await adminAuth.verifyIdToken(token);

    const userProfile = await adminDb
      .collection("user")
      .doc(decodedToken.uid)
      .get();

    const role = String(userProfile.data()?.role || "").toLowerCase();

    if (!userProfile.exists || role !== "admin") {
      return res.status(403).json({
        error: "Administrator access required.",
      });
    }

    req.user = decodedToken;
    next();
  } catch (error) {
    console.error("Admin authentication failed:", error);

    return res.status(401).json({
      error: "Invalid or expired authentication token.",
    });
  }
}

// Protect every endpoint in this router.
router.use(requireAdmin);

// GET /api/admin/collections
// List Firestore collections.
router.get("/collections", async (_req, res) => {
  try {
    const collections = await adminDb.listCollections();

    return res.json(
      collections.map((collection) => ({
        id: collection.id,
      }))
    );
  } catch (error) {
    console.error("List collections failed:", error);

    return res.status(500).json({
      error: "Unable to list Firestore collections.",
    });
  }
});

// GET /api/admin/collections/:collectionId/documents
// List up to 250 documents in the selected collection.
router.get("/collections/:collectionId/documents", async (req, res) => {
  try {
    const collectionId = req.params.collectionId;

    if (!collectionId || collectionId.includes("/")) {
      return res.status(400).json({
        error: "Invalid collection ID.",
      });
    }

    const snapshot = await adminDb
      .collection(collectionId)
      .limit(250)
      .get();

    return res.json(
      snapshot.docs.map((document) => ({
        id: document.id,
        data: document.data(),
      }))
    );
  } catch (error) {
    console.error("List documents failed:", error);

    return res.status(500).json({
      error: "Unable to list documents.",
    });
  }
});

// POST /api/admin/collections/:collectionId/documents
// Create a document using a specified ID or an automatically generated ID.
router.post("/collections/:collectionId/documents", async (req, res) => {
  try {
    const { collectionId } = req.params;
    const { id, data } = req.body || {};

    if (!collectionId || collectionId.includes("/")) {
      return res.status(400).json({
        error: "Invalid collection ID.",
      });
    }

    if (
      !data ||
      typeof data !== "object" ||
      Array.isArray(data)
    ) {
      return res.status(400).json({
        error: "Document data must be a JSON object.",
      });
    }

    const collection = adminDb.collection(collectionId);

    const documentRef =
      typeof id === "string" && id.trim()
        ? collection.doc(id.trim())
        : collection.doc();

    await documentRef.create(data);

    return res.status(201).json({
      id: documentRef.id,
      message: "Document created successfully.",
    });
  } catch (error) {
    console.error("Create document failed:", error);

    if (error.code === 6 || error.code === "already-exists") {
      return res.status(409).json({
        error: "That document ID already exists.",
      });
    }

    return res.status(500).json({
      error: "Unable to create document.",
    });
  }
});

// PUT /api/admin/collections/:collectionId/documents/:documentId
// Replace the fields in an existing document.
router.put(
  "/collections/:collectionId/documents/:documentId",
  async (req, res) => {
    try {
      const { collectionId, documentId } = req.params;
      const { data } = req.body || {};

      if (
        !collectionId ||
        collectionId.includes("/") ||
        !documentId ||
        documentId.includes("/")
      ) {
        return res.status(400).json({
          error: "Invalid collection or document ID.",
        });
      }

      if (
        !data ||
        typeof data !== "object" ||
        Array.isArray(data)
      ) {
        return res.status(400).json({
          error: "Document data must be a JSON object.",
        });
      }

      const documentRef = adminDb
        .collection(collectionId)
        .doc(documentId);

      const existingDocument = await documentRef.get();

      if (!existingDocument.exists) {
        return res.status(404).json({
          error: "Document not found.",
        });
      }

      // This replaces the document's current fields.
      await documentRef.set(data);

      return res.json({
        message: "Document saved successfully.",
      });
    } catch (error) {
      console.error("Save document failed:", error);

      return res.status(500).json({
        error: "Unable to save document.",
      });
    }
  }
);

// DELETE /api/admin/collections/:collectionId/documents/:documentId
router.delete(
  "/collections/:collectionId/documents/:documentId",
  async (req, res) => {
    try {
      const { collectionId, documentId } = req.params;

      if (
        !collectionId ||
        collectionId.includes("/") ||
        !documentId ||
        documentId.includes("/")
      ) {
        return res.status(400).json({
          error: "Invalid collection or document ID.",
        });
      }

      const documentRef = adminDb
        .collection(collectionId)
        .doc(documentId);

      const existingDocument = await documentRef.get();

      if (!existingDocument.exists) {
        return res.status(404).json({
          error: "Document not found.",
        });
      }

      await documentRef.delete();

      return res.json({
        message: "Document deleted successfully.",
      });
    } catch (error) {
      console.error("Delete document failed:", error);

      return res.status(500).json({
        error: "Unable to delete document.",
      });
    }
  }
);

export default router;