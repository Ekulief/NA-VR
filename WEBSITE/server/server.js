import express from "express";
import cors from "cors";
import dotenv from "dotenv";

import userRoutes from "./routes/userRoutes.js";
import adminRoutes from "./routes/adminRoutes.js";
import courseRoutes from "./routes/courseRoutes.js";

dotenv.config();

const app = express();
const PORT = process.env.PORT || 5000;

app.use(
  cors({
    origin: process.env.CLIENT_URL || "http://localhost:5173",
    credentials: true,
  })
);

app.use(express.json({ limit: "2mb" }));

app.get("/", (_req, res) => {
  res.json({
    message: "Na-VR backend is running.",
  });
});

app.use("/api/users", userRoutes);
app.use("/api/admin", adminRoutes);
app.use("/api/courses", courseRoutes);

// Handle unknown API endpoints.
app.use((_req, res) => {
  res.status(404).json({
    error: "Endpoint not found.",
  });
});

// Central error handler.
app.use((error, _req, res, _next) => {
  console.error("Unhandled server error:", error);

  res.status(500).json({
    error: "An unexpected server error occurred.",
  });
});

app.listen(PORT, () => {
  console.log(`Na-VR backend running on port ${PORT}`);
});