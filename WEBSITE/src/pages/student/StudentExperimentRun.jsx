import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  doc,
  getDoc,
  collection,
  query,
  where,
  addDoc,
  updateDoc,
  onSnapshot,
  serverTimestamp,
  orderBy,
  getDocs,
} from "firebase/firestore";

import { db } from "../../config/firebase-config";
import { useAuth } from "../../context/AuthContext";

import {
  ArrowLeft,
  Box,
  Play,
  Pause,
  Square,
  Download,
  Loader2,
} from "lucide-react";

export default function StudentExperimentRun() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();
  const { user } = useAuth();

  const [experiment, setExperiment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Progress document
  const [progress, setProgress] = useState(null); // { id, ...data }
  const [progressLoading, setProgressLoading] = useState(false);

  // Logs
  const [logs, setLogs] = useState([]);

  // Local UI helpers
  const [connecting, setConnecting] = useState(false);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const timerRef = useRef(null);

  // ---------- Load experiment ----------
  useEffect(() => {
    const loadExperiment = async () => {
      try {
        setLoading(true);
        setError("");

        const experimentRef = doc(db, "experiment", experimentId);
        const snap = await getDoc(experimentRef);

        if (!snap.exists()) {
          setError("Experiment not found.");
          return;
        }

        setExperiment({ id: snap.id, ...snap.data() });
      } catch (err) {
        console.error(err);
        setError("Unable to load the experiment.");
      } finally {
        setLoading(false);
      }
    };

    if (experimentId) loadExperiment();
  }, [experimentId]);

  // ---------- Check for existing progress & listen ----------
  useEffect(() => {
    if (!experimentId || !user?.uid) return;

    const q = query(
      collection(db, "experimentProgress"),
      where("experimentId", "==", experimentId),
      where("studentId", "==", user.uid)
    );

    const unsubscribe = onSnapshot(
      q,
      (snapshot) => {
        if (snapshot.empty) {
          setProgress(null);
          return;
        }

        // Take the most recent one if multiple exist
        const docs = snapshot.docs.map((d) => ({ id: d.id, ...d.data() }));
        // Prefer non-ended, otherwise the latest
        const active = docs.find((d) => d.sessionControl !== "ended") || docs[0];
        setProgress(active);
      },
      (err) => {
        console.error("Error listening to progress:", err);
      }
    );

    return () => unsubscribe();
  }, [experimentId, user?.uid]);

  // ---------- Listen to logs for current progress ----------
  useEffect(() => {
    if (!progress?.id) {
      setLogs([]);
      return;
    }

    const q = query(
      collection(db, "logs"),
      where("progressId", "==", progress.id)
      // orderBy("timestamp", "asc") // enable if you have the index
    );

    const unsubscribe = onSnapshot(
      q,
      (snapshot) => {
        const items = snapshot.docs.map((d) => ({
          id: d.id,
          ...d.data(),
        }));

        // Sort client-side by timestamp (handles string ISO timestamps)
        items.sort((a, b) => {
          const ta = a.timestamp ? new Date(a.timestamp).getTime() : 0;
          const tb = b.timestamp ? new Date(b.timestamp).getTime() : 0;
          return ta - tb;
        });

        setLogs(items);
      },
      (err) => {
        console.error("Error listening to logs:", err);
      }
    );

    return () => unsubscribe();
  }, [progress?.id]);

  // ---------- Timer (runs only when sessionControl === "running") ----------
  useEffect(() => {
    if (progress?.sessionControl === "running") {
      timerRef.current = setInterval(() => {
        setElapsedSeconds((prev) => prev + 1);
      }, 1000);
    } else {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
    }

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [progress?.sessionControl]);

  // Reset timer when a new progress is created
  useEffect(() => {
    if (progress?.sessionControl === "idle") {
      setElapsedSeconds(0);
    }
  }, [progress?.id]);

  // ---------- Helpers ----------
  const formatTime = (totalSeconds) => {
    const m = Math.floor(totalSeconds / 60)
      .toString()
      .padStart(2, "0");
    const s = (totalSeconds % 60).toString().padStart(2, "0");
    return `${m}:${s}`;
  };

  const formatLogTime = (ts) => {
    if (!ts) return "";
    try {
      return new Date(ts).toLocaleTimeString();
    } catch {
      return ts;
    }
  };

  // ---------- Actions ----------
 const handleConnectVR = async () => {
  if (!experiment || !user?.uid || connecting || progress) return;

  const confirmed = window.confirm(
    `Connect to VR headset "${experiment.vrId || "Unknown"}"?\n\nThis will create a new session for this experiment.`
  );
  if (!confirmed) return;

  try {
    setConnecting(true);

   // Find the group this student belongs to
  let groupId = null;

  try {
  const groupsQuery = query(
    collection(db, "group"),
    where("blockId", "==", blockId),
    where("studentIds", "array-contains", user.uid)
  );

  const groupsSnap = await getDocs(groupsQuery);

  if (!groupsSnap.empty) {
    // Take the first matching group
    groupId = groupsSnap.docs[0].id;
  }
} catch (err) {
  console.error("Error finding student group:", err);
}

    // 2. Create the experimentProgress document
    const progressData = {
      blockId: blockId || null,
      experimentId,
      studentId: user.uid,
      groupId,                          // ← now correctly resolved
      vrId: experiment.vrId || null,
      sessionControl: "idle",
      completionStatus: "In Progress",
      createdAt: serverTimestamp(),
      updatedAt: serverTimestamp(),
    };

    const docRef = await addDoc(
      collection(db, "experimentProgress"),
      progressData
    );

    console.log("Created progress:", docRef.id, "with groupId:", groupId);
  } catch (err) {
    console.error("Failed to create progress:", err);
    alert("Unable to connect VR device. Please try again.");
  } finally {
    setConnecting(false);
  }
};

  const updateSessionControl = async (newControl) => {
    if (!progress?.id) return;

    try {
      const ref = doc(db, "experimentProgress", progress.id);

      const updates = {
        sessionControl: newControl,
        updatedAt: serverTimestamp(),
      };

      if (newControl === "ended") {
        updates.completionStatus = "Completed";
        updates.completionAt = new Date().toISOString();
      }

      await updateDoc(ref, updates);
    } catch (err) {
      console.error("Failed to update sessionControl:", err);
      alert("Unable to update session. Please try again.");
    }
  };

  const handlePlay = () => updateSessionControl("running");
  const handlePause = () => updateSessionControl("paused");
  const handleEnd = async () => {
    const confirmed = window.confirm(
      "End this experiment session? This cannot be undone."
    );
    if (!confirmed) return;
    await updateSessionControl("ended");
  };

  const handleDownloadResults = () => {
    const payload = {
      experimentName: experiment?.experimentName || "Untitled Experiment",
      vrId: experiment?.vrId,
      progressId: progress?.id,
      durationSeconds: elapsedSeconds,
      duration: formatTime(elapsedSeconds),
      sessionControl: progress?.sessionControl,
      completionStatus: progress?.completionStatus,
      logs: logs,
      completedAt: progress?.completionAt || new Date().toISOString(),
    };

    const blob = new Blob([JSON.stringify(payload, null, 2)], {
      type: "application/json",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `${(payload.experimentName || "results").replace(/\s+/g, "_")}_results.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  // ---------- Render states ----------
  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p>Loading session...</p>
      </div>
    );
  }

  if (error || !experiment) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p className="text-gray-500">{error || "Experiment not found."}</p>
      </div>
    );
  }

  const sessionControl = progress?.sessionControl || null;
  const isIdle = sessionControl === "idle";
  const isRunning = sessionControl === "running";
  const isPaused = sessionControl === "paused";
  const isEnded = sessionControl === "ended";
  const isConnected = !!progress;

  const statusLabel = isEnded
    ? "Completed"
    : isRunning
      ? "Running"
      : isPaused
        ? "Paused"
        : isIdle
          ? "Ready"
          : "Not Connected";

  const statusStyle = isEnded
    ? "bg-indigo-100 text-indigo-700"
    : isRunning
      ? "bg-green-100 text-green-700"
      : isPaused
        ? "bg-yellow-100 text-yellow-700"
        : "bg-gray-100 text-gray-600";

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20 px-5 pb-10 max-w-6xl mx-auto">
        {/* Back */}
        <button
          onClick={() =>
            navigate(`/student/course/${blockId}/experiment/${experimentId}`)
          }
          className="flex items-center gap-2 text-gray-600 hover:text-black transition mb-4"
        >
          <ArrowLeft size={20} />
          <span className="text-lg">Back</span>
        </button>

        {/* Header */}
        <div className="flex items-start justify-between gap-4 mb-2">
          <h1 className="text-2xl font-medium">
            {experiment.experimentName || "Untitled Experiment"}
          </h1>
          <span
            className={`px-3 py-1 rounded-full text-sm whitespace-nowrap ${statusStyle}`}
          >
            {statusLabel}
          </span>
        </div>

        <p className="text-gray-500 mb-1">
          {experiment.moduleDescription ||
            experiment.participantInstructions ||
            experiment.instructions ||
            "Run this experiment in VR."}
        </p>

        {experiment.vrId && (
          <p className="text-sm text-gray-400 mb-6">
            VR Headset: <span className="font-medium">{experiment.vrId}</span>
          </p>
        )}

        <div className="grid lg:grid-cols-3 gap-6 mb-6 items-stretch">
          {/* Left column – controls + timer */}
          <div className="flex flex-col gap-6">
            {/* Session Control */}
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Control</h2>

              {!isConnected ? (
                <button
                  onClick={handleConnectVR}
                  disabled={connecting}
                  className="w-full rounded-lg py-3 text-center border border-gray-300 hover:bg-gray-50 transition disabled:opacity-60 disabled:cursor-wait flex items-center justify-center gap-2"
                >
                  {connecting ? (
                    <>
                      <Loader2 size={18} className="animate-spin" />
                      Connecting...
                    </>
                  ) : (
                    "Connect VR Device"
                  )}
                </button>
              ) : (
                <div className="flex flex-col gap-3">
                  {/* Play / Resume */}
                  {(isIdle || isPaused) && (
                    <button
                      onClick={handlePlay}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 bg-green-700 hover:bg-green-600 text-white transition"
                    >
                      <Play size={18} fill="currentColor" />
                      <span>{isPaused ? "Resume" : "Play"}</span>
                    </button>
                  )}

                  {/* Pause */}
                  {isRunning && (
                    <button
                      onClick={handlePause}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 border border-yellow-600 text-yellow-700 hover:bg-yellow-50 transition"
                    >
                      <Pause size={18} />
                      <span>Pause</span>
                    </button>
                  )}

                  {/* End / Disconnect */}
                  {!isEnded && (
                    <button
                      onClick={handleEnd}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 bg-red-700 hover:bg-red-600 text-white transition"
                    >
                      <Square size={16} fill="currentColor" />
                      <span>End / Disconnect</span>
                    </button>
                  )}

                  {isEnded && (
                    <p className="text-center text-sm text-gray-500 py-2">
                      Session completed
                    </p>
                  )}
                </div>
              )}
            </section>

            {/* Timer */}
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Timer</h2>
              <p className="text-4xl font-medium text-center tabular-nums">
                {formatTime(elapsedSeconds)}
              </p>
              {isPaused && (
                <p className="text-center text-sm text-yellow-600 mt-2">
                  Paused
                </p>
              )}
            </section>
          </div>

          {/* Right – VR Preview placeholder */}
          <div className="lg:col-span-2">
            <section className="border border-gray-300 rounded-xl p-5 h-full flex flex-col">
              <h2 className="text-lg font-medium mb-4">VR Preview</h2>
              <div className="flex-1 min-h-[260px] bg-gray-200 rounded-lg flex flex-col items-center justify-center text-gray-500">
                <Box size={40} className="mb-3" />
                <p>VR environment preview</p>
                <p className="text-sm">
                  {isRunning
                    ? "Session running..."
                    : isPaused
                      ? "Session paused"
                      : isEnded
                        ? "Session ended"
                        : isConnected
                          ? "Ready to start"
                          : "Connect VR device to begin"}
                </p>
              </div>
            </section>
          </div>
        </div>

        {/* Live Logs */}
        <section className="border border-gray-300 rounded-xl p-5 mb-6">
          <h2 className="text-lg font-medium mb-4">
            Live Logs
            {logs.length > 0 && (
              <span className="ml-2 text-sm font-normal text-gray-400">
                ({logs.length})
              </span>
            )}
          </h2>

          {!isConnected ? (
            <p className="text-gray-500 text-sm">
              Connect a VR device to start receiving logs.
            </p>
          ) : logs.length === 0 ? (
            <p className="text-gray-500 text-sm">
              Waiting for logs from the VR headset...
            </p>
          ) : (
            <div className="max-h-80 overflow-y-auto space-y-2">
              {logs.map((log) => (
                <div
                  key={log.id}
                  className="flex gap-3 py-2 border-b border-gray-100 last:border-0 text-sm"
                >
                  <span className="text-gray-400 whitespace-nowrap tabular-nums">
                    {formatLogTime(log.timestamp)}
                  </span>
                  <span className="text-indigo-600 font-medium whitespace-nowrap">
                    {log.eventType || "event"}
                  </span>
                  <span className="text-gray-700 flex-1">
                    {log.message || "—"}
                  </span>
                </div>
              ))}
            </div>
          )}
        </section>

        {/* Export (only when ended) */}
        {isEnded && (
          <section className="border border-gray-300 rounded-xl p-5">
            <h2 className="text-lg font-medium mb-4">Export Data</h2>
            <button
              onClick={handleDownloadResults}
              className="w-full sm:w-auto flex items-center justify-center gap-2 border border-gray-300 hover:bg-gray-50 rounded-lg px-5 py-2.5 transition"
            >
              <Download size={18} />
              <span>Download Results</span>
            </button>
          </section>
        )}
      </main>
    </div>
  );
}