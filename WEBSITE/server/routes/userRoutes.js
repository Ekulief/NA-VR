import express from "express";
import { adminAuth, adminDb } from "../config/firebaseAdmin.js";
import { FieldValue } from "firebase-admin/firestore";

const router = express.Router();

// Verify the Firebase ID token and require an administrator.
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

    const decoded = await adminAuth.verifyIdToken(token);

    const profile = await adminDb
      .collection("user")
      .doc(decoded.uid)
      .get();

    const role = String(profile.data()?.role || "").toLowerCase();

    if (!profile.exists || role !== "admin") {
      return res.status(403).json({
        error: "Administrator access required.",
      });
    }

    req.user = decoded;
    req.userData = profile.data();

    next();
  } catch (error) {
    console.error("Admin authorization error:", error);

    return res.status(401).json({
      error: "Invalid or expired authentication token.",
    });
  }
}

// Verify the Firebase ID token and require an instructor.
async function requireInstructor(req, res, next) {
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

    const decoded = await adminAuth.verifyIdToken(token);

    const profile = await adminDb
      .collection("user")
      .doc(decoded.uid)
      .get();

    const role = String(profile.data()?.role || "").toLowerCase();

    if (!profile.exists || role !== "instructor") {
      return res.status(403).json({
        error: "Only instructors can create student accounts.",
      });
    }

    req.user = decoded;
    req.userData = profile.data();

    next();
  } catch (error) {
    console.error("Instructor authorization error:", error);

    return res.status(401).json({
      error: "Invalid or expired authentication token.",
    });
  }
}

function normalizeStatus(status) {
  return String(status || "Active").toLowerCase() === "inactive"
    ? "Inactive"
    : "Active";
}

function getErrorMessage(error) {
  if (error.code === "auth/email-already-exists") {
    return "An account with this email already exists.";
  }

  if (error.code === "auth/invalid-email") {
    return "The email address is invalid.";
  }

  if (error.code === "auth/invalid-password") {
    return "The password does not meet Firebase requirements.";
  }

  return error.message || "The request could not be completed.";
}

// GET /api/users
// List Firebase Authentication accounts and their Firestore profiles.
router.get("/", requireAdmin, async (_req, res) => {
  try {
    const users = [];
    let nextPageToken;

    // Read all pages from Firebase Authentication.
    do {
      const result = await adminAuth.listUsers(1000, nextPageToken);

      for (const authUser of result.users) {
        const profile = await adminDb
          .collection("user")
          .doc(authUser.uid)
          .get();

        const data = profile.exists ? profile.data() : {};
        const displayName = authUser.displayName || "";
        const nameParts = displayName.split(" ");

        users.push({
          id: authUser.uid,
          uid: authUser.uid,
          firstName: data.firstName || nameParts[0] || "",
          lastName:
            data.lastName || nameParts.slice(1).join(" ") || "",
          name: data.name || displayName,
          email: authUser.email || data.email || "",
          role: data.role || "",
          studentId: data.studentId || "",
          studentNumber: data.studentNumber || "",
          status: data.status || (authUser.disabled ? "Inactive" : "Active"),
          disabled: authUser.disabled,
          createdAt: data.createdAt || authUser.metadata.creationTime || null,
          updatedAt: data.updatedAt || authUser.metadata.lastSignInTime || null,
        });
      }

      nextPageToken = result.pageToken;
    } while (nextPageToken);

    return res.json(users);
  } catch (error) {
    console.error("Error fetching users:", error);

    return res.status(500).json({
      error: "Failed to fetch users.",
    });
  }
});

// POST /api/users
// Create an account in Firebase Authentication and a profile in Firestore.
// This endpoint is administrator-only.
router.post("/", requireAdmin, async (req, res) => {
  let authUser;

  try {
    const {
      firstName = "",
      lastName = "",
      name,
      email,
      password,
      role,
      studentId = "",
      studentNumber = "",
      status = "Active",
    } = req.body || {};

    if (
      typeof email !== "string" ||
      !email.trim() ||
      typeof password !== "string" ||
      password.length < 6
    ) {
      return res.status(400).json({
        error: "A valid email and a password of at least 6 characters are required.",
      });
    }

    const normalizedEmail = email.trim().toLowerCase();
    const fullName =
      (typeof name === "string" && name.trim()) ||
      `${firstName} ${lastName}`.trim();

    const normalizedRole = String(role || "Student").trim();

    if (!["student", "instructor", "admin"].includes(normalizedRole.toLowerCase())) {
      return res.status(400).json({
        error: "Role must be Student, Instructor, or Admin.",
      });
    }

    authUser = await adminAuth.createUser({
      email: normalizedEmail,
      password,
      displayName: fullName,
      disabled: normalizeStatus(status) === "Inactive",
    });

    const now = new Date();

    await adminDb.collection("user").doc(authUser.uid).set({
      firstName: String(firstName || "").trim(),
      lastName: String(lastName || "").trim(),
      name: fullName,
      email: normalizedEmail,
      role: normalizedRole,
      studentId: String(studentId || ""),
      studentNumber: String(studentNumber || ""),
      status: normalizeStatus(status),
      createdAt: now,
      updatedAt: now,
    });

    return res.status(201).json({
      id: authUser.uid,
      uid: authUser.uid,
      firstName: String(firstName || "").trim(),
      lastName: String(lastName || "").trim(),
      name: fullName,
      email: normalizedEmail,
      role: normalizedRole,
      studentId,
      studentNumber,
      status: normalizeStatus(status),
    });
  } catch (error) {
    console.error("Error creating user:", error);

    // If the Firestore write failed after creating the Auth account,
    // try to remove the newly created Auth account to avoid an orphan.
    if (authUser?.uid) {
      try {
        await adminAuth.deleteUser(authUser.uid);
      } catch (cleanupError) {
        console.error("Failed to clean up Auth account:", cleanupError);
      }
    }

    const statusCode =
      error.code === "auth/email-already-exists" ||
      error.code === "auth/invalid-email" ||
      error.code === "auth/invalid-password"
        ? 400
        : 500;

    return res.status(statusCode).json({
      error: getErrorMessage(error),
    });
  }
});

// POST /api/users/students
// Instructor-only endpoint for creating student accounts in bulk.
router.post("/students", requireInstructor, async (req, res) => {
  try {
    const { blockId, password, students } = req.body || {};

    if (!blockId || typeof blockId !== "string") {
      return res.status(400).json({
        error: "blockId is required.",
      });
    }

    if (typeof password !== "string" || password.length < 6) {
      return res.status(400).json({
        error: "The password must be at least 6 characters long.",
      });
    }

    if (!Array.isArray(students) || students.length === 0) {
      return res.status(400).json({
        error: "At least one student is required.",
      });
    }

    if (students.length > 500) {
      return res.status(400).json({
        error: "You can create a maximum of 500 student accounts at once.",
      });
    }

    const blockRef = adminDb.collection("block").doc(blockId);
    const blockSnapshot = await blockRef.get();

    if (!blockSnapshot.exists) {
      return res.status(404).json({
        error: "Block not found.",
      });
    }

    const blockData = blockSnapshot.data() || {};
    const ownerFields = ["instructorId", "ownerId", "createdBy"];

    const ownerField = ownerFields.find(
      (field) =>
        blockData[field] !== undefined &&
        blockData[field] !== null &&
        blockData[field] !== ""
    );

    if (ownerField && blockData[ownerField] !== req.user.uid) {
      return res.status(403).json({
        error: "You are not authorized to add students to this block.",
      });
    }

    const created = [];
    const failed = [];

    for (const student of students) {
      const firstName =
        typeof student.firstName === "string"
          ? student.firstName.trim()
          : "";

      const lastName =
        typeof student.lastName === "string"
          ? student.lastName.trim()
          : "";

      const email =
        typeof student.email === "string"
          ? student.email.trim().toLowerCase()
          : "";

      const studentNumber =
        typeof student.studentNumber === "string"
          ? student.studentNumber.trim()
          : "";

      if (!firstName || !lastName || !email) {
        failed.push({
          ...student,
          error: "First name, last name, and email are required.",
        });
        continue;
      }

      let authUser = null;

      try {
        authUser = await adminAuth.createUser({
          email,
          password,
          displayName: `${firstName} ${lastName}`.trim(),
          disabled: false,
        });

        const now = new Date();

        await adminDb.collection("user").doc(authUser.uid).set({
          firstName,
          lastName,
          name: `${firstName} ${lastName}`.trim(),
          email,
          studentNumber,
          role: "student",
          status: "Active",
          createdAt: now,
          updatedAt: now,
        });

        await blockRef.update({
          studentIds: FieldValue.arrayUnion(authUser.uid),
          updatedAt: now,
        });

        created.push({
          id: authUser.uid,
          uid: authUser.uid,
          firstName,
          lastName,
          name: `${firstName} ${lastName}`.trim(),
          email,
          studentNumber,
          role: "student",
          status: "Active",
        });
      } catch (error) {
        console.error(`Error creating student ${email}:`, error);

        if (authUser?.uid) {
          try {
            await adminAuth.deleteUser(authUser.uid);
            await adminDb.collection("user").doc(authUser.uid).delete();
          } catch (cleanupError) {
            console.error("Failed to clean up student account:", cleanupError);
          }
        }

        failed.push({
          ...student,
          error: getErrorMessage(error),
        });
      }
    }

    if (created.length === 0) {
      return res.status(400).json({
        error: "No student accounts were created.",
        created,
        failed,
      });
    }

    return res.status(201).json({
      message: `${created.length} student account(s) created successfully.`,
      created,
      failed,
    });
  } catch (error) {
    console.error("Error creating student accounts:", error);

    return res.status(500).json({
      error: "Failed to create student accounts.",
    });
  }
});

// PATCH /api/users/:uid
// Update Firebase Authentication details and the Firestore profile.
router.patch("/:uid", requireAdmin, async (req, res) => {
  try {
    const { uid } = req.params;

    const {
      firstName,
      lastName,
      name,
      email,
      role,
      studentId,
      studentNumber,
      status,
    } = req.body || {};

    const authUpdate = {};

    if (email !== undefined) {
      if (typeof email !== "string" || !email.trim()) {
        return res.status(400).json({
          error: "A valid email address is required.",
        });
      }

      authUpdate.email = email.trim().toLowerCase();
    }

    if (name !== undefined) {
      authUpdate.displayName = String(name);
    } else if (firstName !== undefined || lastName !== undefined) {
      authUpdate.displayName =
        `${firstName || ""} ${lastName || ""}`.trim();
    }

    if (status !== undefined) {
      authUpdate.disabled = normalizeStatus(status) === "Inactive";
    }

    // Check the account exists before updating.
    await adminAuth.getUser(uid);

    if (Object.keys(authUpdate).length > 0) {
      await adminAuth.updateUser(uid, authUpdate);
    }

    const firestoreUpdate = {
      updatedAt: new Date(),
    };

    if (firstName !== undefined) firestoreUpdate.firstName = firstName;
    if (lastName !== undefined) firestoreUpdate.lastName = lastName;
    if (name !== undefined) firestoreUpdate.name = name;
    if (email !== undefined) firestoreUpdate.email = email.trim().toLowerCase();
    if (role !== undefined) firestoreUpdate.role = role;
    if (studentId !== undefined) firestoreUpdate.studentId = studentId;
    if (studentNumber !== undefined) firestoreUpdate.studentNumber = studentNumber;
    if (status !== undefined) firestoreUpdate.status = normalizeStatus(status);

    await adminDb.collection("user").doc(uid).set(firestoreUpdate, {
      merge: true,
    });

    const updatedAuthUser = await adminAuth.getUser(uid);
    const updatedProfile = await adminDb.collection("user").doc(uid).get();

    return res.json({
      id: uid,
      uid,
      ...updatedProfile.data(),
      email: updatedAuthUser.email || "",
      disabled: updatedAuthUser.disabled,
    });
  } catch (error) {
    console.error("Error updating user:", error);

    if (error.code === "auth/user-not-found") {
      return res.status(404).json({
        error: "User not found.",
      });
    }

    const statusCode =
      error.code === "auth/email-already-exists" ||
      error.code === "auth/invalid-email"
        ? 400
        : 500;

    return res.status(statusCode).json({
      error: getErrorMessage(error),
    });
  }
});

// DELETE /api/users/:uid
// Delete a Firebase Authentication account and its Firestore profile.
router.delete("/:uid", requireAdmin, async (req, res) => {
  try {
    const { uid } = req.params;

    if (uid === req.user.uid) {
      return res.status(400).json({
        error: "You cannot delete your own administrator account here.",
      });
    }

    try {
      await adminAuth.deleteUser(uid);
    } catch (error) {
      if (error.code !== "auth/user-not-found") {
        throw error;
      }
    }

    await adminDb.collection("user").doc(uid).delete();

    return res.json({
      message: "User deleted successfully.",
      uid,
    });
  } catch (error) {
    console.error("Error deleting user:", error);

    return res.status(500).json({
      error: "Failed to delete user.",
    });
  }
});

export default router;