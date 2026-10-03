import express from "express";
import { adminAuth, adminDb } from "../config/firebaseAdmin.js";

const router = express.Router();

// Get users
router.get("/", async (req, res) => {
  try {
    const users = [];
    let nextPageToken;

    do {
      const result = await adminAuth.listUsers(1000, nextPageToken);

      for (const user of result.users) {
        let firestoreData = {};

        try {
          const userDoc = await adminDb
            .collection("user")
            .doc(user.uid)
            .get();

          if (userDoc.exists) {
            firestoreData = userDoc.data();
          }
        } catch (error) {
          console.error(
            `Error getting Firestore data for ${user.uid}:`,
            error
          );
        }

        users.push({
          id: user.uid,
          uid: user.uid,

          email: user.email || "",
          displayName: user.displayName || "",
          emailVerified: user.emailVerified,
          disabled: user.disabled,

          createdAt: user.metadata.creationTime,
          lastSignInAt: user.metadata.lastSignInTime,

          ...firestoreData,
        });
      }

      nextPageToken = result.pageToken;
    } while (nextPageToken);

    res.json(users);
  } catch (error) {
    console.error("Error getting users:", error);

    res.status(500).json({
      message: "Unable to retrieve users.",
    });
  }
});

// Create User
router.post("/", async (req, res) => {
  try {
    const {
      name,
      email,
      password,
      role,
      studentId,
      status,
    } = req.body;

    if (!name || !email || !password || !role) {
      return res.status(400).json({
        message: "Name, email, password, and role are required.",
      });
    }

    const nameParts = name.trim().split(/\s+/);

    const firstName = nameParts.shift() || "";
    const lastName = nameParts.join(" ");

    const authUser = await adminAuth.createUser({
      email: email.trim(),
      password,
      displayName: name.trim(),
      disabled: status?.toLowerCase() !== "active",
    });

    await adminDb
      .collection("user")
      .doc(authUser.uid)
      .set({
        firstName,
        lastName,
        name: name.trim(),
        email: email.trim(),
        role,
        studentId: studentId || "",
        status: status || "Active",

        createdAt: new Date(),
        updatedAt: new Date(),
      });

    res.status(201).json({
      message: "User created successfully.",
      uid: authUser.uid,
    });

  } catch (error) {
    console.error("Error creating user:", error);

    res.status(500).json({
      message: error.message || "Unable to create user.",
    });
  }
});

// Edit User
router.patch("/:uid", async (req, res) => {
  try {
    const { uid } = req.params;

    const {
      name,
      email,
      role,
      studentId,
      status,
    } = req.body;

    const authUpdate = {};

    if (email) {
      authUpdate.email = email.trim();
    }

    if (name) {
      authUpdate.displayName = name.trim();
    }

    if (status) {
      authUpdate.disabled =
        status.toLowerCase() !== "active";
    }

    await adminAuth.updateUser(uid, authUpdate);

    const nameParts = (name || "").trim().split(/\s+/);

    const firstName = nameParts.shift() || "";
    const lastName = nameParts.join(" ");

    await adminDb
      .collection("user")
      .doc(uid)
      .set(
        {
          firstName,
          lastName,
          name: name || "",
          email: email || "",
          role: role || "Student",
          studentId: studentId || "",
          status: status || "Active",

          updatedAt: new Date(),
        },
        {
          merge: true,
        }
      );

    res.json({
      message: "User updated successfully.",
    });

  } catch (error) {
    console.error("Error updating user:", error);

    res.status(500).json({
      message: error.message || "Unable to update user.",
    });
  }
});

// Delete User
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
    });

  } catch (error) {
    console.error("Error deleting user:", error);

    res.status(500).json({
      message: error.message || "Unable to delete user.",
    });
  }
});

export default router;