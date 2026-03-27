#  SLUBT Labs VR Simulation

This folder contains the Unity-based Virtual Reality application for SLUBT Labs.

##  Overview
The VR client is the primary interface for students to perform experiments. It is designed to capture high-fidelity interaction data and sync it directly to the web backend.

---

## Features
- **Immersive Experiments:** High-fidelity VR environment for lab testing.
- **Data Logging:** Tracks user movements and experiment choices.
- **Backend Sync:** Automatically pushes session data to the Analytics dashboard.
- **Optimized Controls:** Custom input handling (Front-facing fire only).

---

##  Tech Stack
- **Engine:** Unity 2022.x+ (Universal Render Pipeline)
- **Language:** C#
- **Version Control:** Git (via GitHub) + UVCS (Plastic SCM) Metadata

---

## Setup Instructions
1. Ensure you have **Git LFS** installed on your machine.
2. Open **Unity Hub** and select "Add project from disk."
3. Select this `/VR` folder.
4. Ensure **Assets > External Dependency Manager** (if using Firebase) has resolved all dependencies.