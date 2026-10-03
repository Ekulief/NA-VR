import express from "express";
import cors from "cors";
import dotenv from "dotenv";

import userRoutes from "./routes/userRoutes.js";

dotenv.config();

const app = express();

const PORT = process.env.PORT || 5000;

app.use(
  cors({
    origin: process.env.CLIENT_URL || "http://localhost:5173",
    credentials: true,
  }),
);

app.use(express.json());

app.get("/", (req, res) => {
  res.json({
    message: "SLUBT Labs backend is running.",
  });
});

/*
  User routes
*/
app.use("/api/users", userRoutes);

/*
  Start server
*/
app.listen(PORT, () => {
  console.log(`SLUBT Labs backend running on port ${PORT}`);
});