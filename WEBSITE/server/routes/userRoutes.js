import express from "express";
import { adminAuth, adminDb } from "../config/firebaseAdmin.js";
import { FieldValue } from "firebase-admin/firestore";

const router = express.Router();

const requireInstructor = async (req, res, next) => {
  try {
    const authHeader = req.headers.authorization || "";

    if (!authHeader.startsWith("Bearer ")) {
      return res.status(401).json({
        error: "Missing Firebase authentication token.",
      });
    }

    const idToken = authHeader.substring(7).trim();

    if (!idToken) {
      return res.status(401).json({
        error: "Missing Firebase authentication token.",
      });
    }

    const decodedToken = await adminAuth.verifyIdToken(idToken);

    req.user = decodedToken;

    const userDoc = await adminDb
      .collection("user")
      .doc(decodedToken.uid)
      .get();

    if (!userDoc.exists) {
      return res.status(403).json({
        error: "User profile not found.",
      });
    }

    const userData = userDoc.data();

    if (
      !userData.role ||
      userData.role.toString().toLowerCase() !== "instructor"
    ) {
      return res.status(403).json({
        error: "Only instructors can create student accounts.",
      });
    }

    req.userData = userData;

    next();
  } catch (error) {
    console.error("Instructor authentication error:", error);

    return res.status(401).json({
      error: "Invalid or expired authentication token.",
    });
  }
};


router.get("/", async (req, res) => {
  try {
    const listUsersResult = await adminAuth.listUsers();

    const users = await Promise.all(
      listUsersResult.users.map(async (authUser) => {
        const userDoc = await adminDb
          .collection("user")
          .doc(authUser.uid)
          .get();

        const firestoreData = userDoc.exists ? userDoc.data() : {};

        return {
          id: authUser.uid,
          uid: authUser.uid,
          firstName:
            firestoreData.firstName ||
            authUser.displayName?.split(" ")[0] ||
            "",
          lastName:
            firestoreData.lastName ||
            authUser.displayName?.split(" ").slice(1).join(" ") ||
            "",
          name:
            firestoreData.name ||
            authUser.displayName ||
            "",
          email: authUser.email || "",
          role: firestoreData.role || "",
          studentId: firestoreData.studentId || "",
          studentNumber: firestoreData.studentNumber || "",
          status:
            firestoreData.status ||
            (authUser.disabled ? "Inactive" : "Active"),
          disabled: authUser.disabled,
          createdAt:
            firestoreData.createdAt ||
            authUser.metadata?.creationTime ||
            null,
          updatedAt:
            firestoreData.updatedAt ||
            authUser.metadata?.lastSignInTime ||
            null,
        };
      })
    );

    res.json(users);
  } catch (error) {
    console.error("Error fetching users:", error);

    res.status(500).json({
      error: "Failed to fetch users.",
    });
  }
});

router.post("/", async (req, res) => {
  try {
    const {
      firstName,
      lastName,
      name,
      email,
      password,
      role,
      studentId,
      studentNumber,
      status,
    } = req.body;

    if (!email || !password) {
      return res.status(400).json({
        error: "Email and password are required.",
      });
    }

    const authUser = await adminAuth.createUser({
      email: email.trim(),
      password,
      displayName:
        name ||
        `${firstName || ""} ${lastName || ""}`.trim(),
      disabled: false,
    });

    const now = new Date();

    await adminDb
      .collection("user")
      .doc(authUser.uid)
      .set({
        firstName: firstName || "",
        lastName: lastName || "",
        name:
          name ||
          `${firstName || ""} ${lastName || ""}`.trim(),
        email: email.trim(),
        role: role || "",
        studentId: studentId || "",
        studentNumber: studentNumber || "",
        status: status || "Active",
        createdAt: now,
        updatedAt: now,
      });

    res.status(201).json({
      id: authUser.uid,
      uid: authUser.uid,
      firstName: firstName || "",
      lastName: lastName || "",
      name:
        name ||
        `${firstName || ""} ${lastName || ""}`.trim(),
      email: email.trim(),
      role: role || "",
      studentId: studentId || "",
      studentNumber: studentNumber || "",
      status: status || "Active",
    });
  } catch (error) {
    console.error("Error creating user:", error);

    if (error.code === "auth/email-already-exists") {
      return res.status(400).json({
        error: "An account with this email already exists.",
      });
    }

    if (error.code === "auth/invalid-email") {
      return res.status(400).json({
        error: "The email address is invalid.",
      });
    }

    if (error.code === "auth/invalid-password") {
      return res.status(400).json({
        error: "The password does not meet Firebase requirements.",
      });
    }

    res.status(500).json({
      error: "Failed to create user.",
    });
  }
});

// Create Student Accounts (Instructor)
router.post("/students", requireInstructor, async (req, res) => {
  try {
    const { blockId, password, students } = req.body;


    if (!blockId) {
      return res.status(400).json({
        error: "blockId is required.",
      });
    }

    if (!password) {
      return res.status(400).json({
        error: "A default password is required.",
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
    const blockDoc = await blockRef.get();

    if (!blockDoc.exists) {
      return res.status(404).json({
        error: "Block not found.",
      });
    }

    const blockData = blockDoc.data() || {};

    const possibleOwnerFields = [
      "instructorId",
      "ownerId",
      "createdBy",
    ];

    const ownerField = possibleOwnerFields.find(
      (field) =>
        blockData[field] !== undefined &&
        blockData[field] !== null &&
        blockData[field] !== ""
    );

    if (
      ownerField &&
      blockData[ownerField] !== req.user.uid
    ) {
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


      if (!firstName) {
        failed.push({
          ...student,
          error: "First name is required.",
        });

        continue;
      }

      if (!lastName) {
        failed.push({
          ...student,
          error: "Last name is required.",
        });

        continue;
      }

      if (!email) {
        failed.push({
          ...student,
          error: "Email is required.",
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


        await adminDb
          .collection("user")
          .doc(authUser.uid)
          .set({
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
        console.error(
          `Error creating student ${email}:`,
          error
        );

        if (authUser?.uid) {
          try {
            await adminAuth.deleteUser(authUser.uid);
          } catch (cleanupError) {
            console.error(
              `Failed to clean up Auth user ${authUser.uid}:`,
              cleanupError
            );
          }
        }

        let errorMessage = "Failed to create student account.";

        if (error.code === "auth/email-already-exists") {
          errorMessage =
            "An account with this email already exists.";
        } else if (error.code === "auth/invalid-email") {
          errorMessage = "The email address is invalid.";
        } else if (error.code === "auth/invalid-password") {
          errorMessage =
            "The password does not meet Firebase requirements.";
        } else if (error.message) {
          errorMessage = error.message;
        }

        failed.push({
          ...student,
          error: errorMessage,
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
    console.error(
      "Error creating student accounts:",
      error
    );

    return res.status(500).json({
      error: "Failed to create student accounts.",
      details: error.message,
    });
  }
});


router.patch("/:uid", async (req, res) => {
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
      disabled,
    } = req.body;


    const authUpdate = {};

    if (email !== undefined) {
      authUpdate.email = email.trim();
    }

    if (name !== undefined) {
      authUpdate.displayName = name;
    } else if (
      firstName !== undefined ||
      lastName !== undefined
    ) {
      authUpdate.displayName =
        `${firstName || ""} ${lastName || ""}`.trim();
    }

    if (disabled !== undefined) {
      authUpdate.disabled = disabled;
    }

    if (Object.keys(authUpdate).length > 0) {
      await adminAuth.updateUser(uid, authUpdate);
    }


    const firestoreUpdate = {
      updatedAt: new Date(),
    };

    if (firstName !== undefined) {
      firestoreUpdate.firstName = firstName;
    }

    if (lastName !== undefined) {
      firestoreUpdate.lastName = lastName;
    }

    if (name !== undefined) {
      firestoreUpdate.name = name;
    }

    if (email !== undefined) {
      firestoreUpdate.email = email.trim();
    }

    if (role !== undefined) {
      firestoreUpdate.role = role;
    }

    if (studentId !== undefined) {
      firestoreUpdate.studentId = studentId;
    }

    if (studentNumber !== undefined) {
      firestoreUpdate.studentNumber = studentNumber;
    }

    if (status !== undefined) {
      firestoreUpdate.status = status;
    }

    await adminDb
      .collection("user")
      .doc(uid)
      .set(firestoreUpdate, { merge: true });

    const updatedAuthUser = await adminAuth.getUser(uid);
    const updatedFirestoreDoc = await adminDb
      .collection("user")
      .doc(uid)
      .get();

    res.json({
      id: uid,
      uid,
      ...updatedFirestoreDoc.data(),
      disabled: updatedAuthUser.disabled,
    });
  } catch (error) {
    console.error("Error updating user:", error);

    if (error.code === "auth/user-not-found") {
      return res.status(404).json({
        error: "User not found.",
      });
    }

    if (error.code === "auth/email-already-exists") {
      return res.status(400).json({
        error: "An account with this email already exists.",
      });
    }

    res.status(500).json({
      error: "Failed to update user.",
    });
  }
});


router.delete("/:uid", async (req, res) => {
  try {
    const { uid } = req.params;

    await adminAuth.deleteUser(uid);

    await adminDb
      .collection("user")
      .doc(uid)
      .delete();

    res.json({
      message: "User deleted successfully.",
      uid,
    });
  } catch (error) {
    console.error("Error deleting user:", error);

    if (error.code === "auth/user-not-found") {
      try {
        await adminDb
          .collection("user")
          .doc(req.params.uid)
          .delete();
      } catch (firestoreError) {
        console.error(
          "Error deleting Firestore user document:",
          firestoreError
        );
      }

      return res.status(404).json({
        error: "User not found in Firebase Authentication.",
      });
    }

    res.status(500).json({
      error: "Failed to delete user.",
    });
  }
});

export default router;
