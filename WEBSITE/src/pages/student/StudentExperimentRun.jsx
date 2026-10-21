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
  X,
  FileText,
  CheckCircle2,
} from "lucide-react";

export default function StudentExperimentRun() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();
  const { user } = useAuth();

  const [experiment, setExperiment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Progress document
  const [progress, setProgress] = useState(null);

  // Result document (from experimentResults)
  const [result, setResult] = useState(null);

  // Logs
  const [logs, setLogs] = useState([]);

  // Local UI helpers
  const [connecting, setConnecting] = useState(false);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const timerRef = useRef(null);
  const initializedRef = useRef(false);

  // Initialize elapsedSeconds ONLY ONCE when progress first loads
  useEffect(() => {
    if (progress?.elapsedSeconds != null && !initializedRef.current) {
      setElapsedSeconds(progress.elapsedSeconds);
      initializedRef.current = true;
    }
  }, [progress?.elapsedSeconds]);

  // ---------- Preview state ----------
  const [showPreview, setShowPreview] = useState(false);

  // Map moduleId → Unity preview scene name
  const previewSceneMap = {
    Depth_Perception: "DepthPerception_Preview",
    Odd_Item_Detection: "OddItem_Preview",
    Attentional_Blindness: "AttentionalBlindness_Preview",
    Memory2: "MemoryHouse_Preview",
    Distance_Perception: "DistancePerception_Preview",
    Memory: "MemoryDiner_Preview"
    // Add more experiments here later
  };

  // Get the correct preview scene name from the experiment
  const getPreviewSceneName = () => {
    if (!experiment) return null;
    const key = experiment.moduleId || experiment.sceneId;
    return previewSceneMap[key] || null;
  };

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

        const docs = snapshot.docs.map((d) => ({ id: d.id, ...d.data() }));
        const active =
          docs.find((d) => d.sessionControl !== "ended") || docs[0];
        setProgress(active);
      },
      (err) => {
        console.error("Error listening to progress:", err);
      }
    );

    return () => unsubscribe();
  }, [experimentId, user?.uid]);

  // ---------- Listen to experimentResults for this progress ----------
  useEffect(() => {
    if (!progress?.id) {
      setResult(null);
      return;
    }

    const q = query(
      collection(db, "experimentResults"),
      where("progressId", "==", progress.id)
    );

    const unsubscribe = onSnapshot(
      q,
      (snapshot) => {
        if (snapshot.empty) {
          setResult(null);
          return;
        }

        const docs = snapshot.docs.map((d) => ({ id: d.id, ...d.data() }));
        docs.sort((a, b) => {
          const ta = a.timestamp ? new Date(a.timestamp).getTime() : 0;
          const tb = b.timestamp ? new Date(b.timestamp).getTime() : 0;
          return tb - ta;
        });

        setResult(docs[0]);
      },
      (err) => {
        console.error("Error listening to experimentResults:", err);
      }
    );

    return () => unsubscribe();
  }, [progress?.id]);

  // ---------- Listen to logs for current progress ----------
  useEffect(() => {
    if (!progress?.id) {
      setLogs([]);
      return;
    }

    const q = query(
      collection(db, "logs"),
      where("progressId", "==", progress.id)
    );

    const unsubscribe = onSnapshot(
      q,
      (snapshot) => {
        const items = snapshot.docs.map((d) => ({
          id: d.id,
          ...d.data(),
        }));

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

  // ---------- Timer ----------
  useEffect(() => {
    if (result?.durationSeconds != null) {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
      return;
    }

    if (progress?.sessionControl === "running") {
      if (timerRef.current) clearInterval(timerRef.current);

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
  }, [progress?.sessionControl, result?.durationSeconds]);

  useEffect(() => {
    if (progress?.sessionControl === "idle") {
      setElapsedSeconds(0);
      initializedRef.current = false;
    }
  }, [progress?.id]);

  // ---------- SAVE ON UNMOUNT EFFECT ----------
  useEffect(() => {
    const progressId = progress?.id;
    
    return () => {
      if (progressId) {
        const ref = doc(db, "experimentProgress", progressId);
        updateDoc(ref, {
          elapsedSeconds: elapsedSeconds,
          updatedAt: serverTimestamp(),
        }).catch((err) => {
          console.error("Failed to save elapsed time on unmount:", err);
        });
      }
    };
  }, [progress?.id]);

  // ---------- Helpers ----------
  const formatTime = (totalSeconds) => {
    const seconds = Math.max(0, Math.round(Number(totalSeconds) || 0));
    const m = Math.floor(seconds / 60)
      .toString()
      .padStart(2, "0");
    const s = (seconds % 60).toString().padStart(2, "0");
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

  const displaySeconds =
    result?.durationSeconds != null
      ? result.durationSeconds
      : elapsedSeconds;

  // ---------- Actions ----------
  const handleConnectVR = async () => {
    if (!experiment || !user?.uid || connecting || progress) return;

    const confirmed = window.confirm(
      `Connect to VR headset "${experiment.vrId || "Unknown"}"?\n\nThis will create a new session for this experiment.`
    );
    if (!confirmed) return;

    try {
      setConnecting(true);

      let groupId = null;

      try {
        const groupsQuery = query(
          collection(db, "group"),
          where("blockId", "==", blockId),
          where("studentIds", "array-contains", user.uid)
        );

        const groupsSnap = await getDocs(groupsQuery);

        if (!groupsSnap.empty) {
          groupId = groupsSnap.docs[0].id;
        }
      } catch (err) {
        console.error("Error finding student group:", err);
      }

      const progressData = {
        blockId: blockId || null,
        experimentId,
        studentId: user.uid,
        groupId,
        vrId: experiment.vrId || null,
        sessionControl: "idle",
        completionStatus: "In Progress",
        elapsedSeconds: 0,
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
        elapsedSeconds: elapsedSeconds,
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
      durationSeconds:
        result?.durationSeconds != null
          ? result.durationSeconds
          : elapsedSeconds,
      duration:
        result?.duration ||
        formatTime(
          result?.durationSeconds != null
            ? result.durationSeconds
            : elapsedSeconds
        ),
      sessionControl: progress?.sessionControl,
      completionStatus: progress?.completionStatus,
      configurations: result?.configurations || null,
      experimentalResults: result?.experimentalResults || null,
      logs: logs,
      startedAt: result?.startedAt || null,
      completedAt:
        result?.completedAt ||
        result?.timestamp ||
        progress?.completionAt ||
        new Date().toISOString(),
    };

    const blob = new Blob([JSON.stringify(payload, null, 2)], {
      type: "application/json",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `${(payload.experimentName || "results").replace(
      /\s+/g,
      "_"
    )}_results.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  // ---------- Render helpers for dynamic config/results blocks ----------
  const renderDynamicDataNode = (data) => {
    if (data === null || data === undefined) return <span className="text-gray-400">—</span>;

    if (Array.isArray(data)) {
      if (data.length === 0) return <span className="text-gray-400">Empty list</span>;
      
      return (
        <div className="space-y-3 w-full">
          {data.map((item, idx) => (
            <div key={idx} className="bg-gray-50 border border-gray-200 rounded-lg p-3">
              <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider block mb-2">
                Item #{idx + 1}
              </span>
              {renderDynamicDataNode(item)}
            </div>
          ))}
        </div>
      );
    }

    if (typeof data === "object") {
      return (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full">
          {Object.entries(data).map(([key, value]) => (
            <div key={key} className="bg-white border border-gray-200 rounded-lg p-3 flex flex-col justify-between">
              <span className="text-xs font-medium text-gray-500 capitalize mb-1">
                {key.replace(/([A-Z])/g, " $1")}
              </span>
              <div className="text-sm font-medium text-gray-900 break-words">
                {typeof value === "object" ? renderDynamicDataNode(value) : String(value)}
              </div>
            </div>
          ))}
        </div>
      );
    }

    return <span>{String(data)}</span>;
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

  const previewSceneName = getPreviewSceneName();

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20 px-5 pb-10 max-w-7xl mx-auto">
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

        {/* Expanded 12-column grid layout with a larger preview */}
        <div className="grid lg:grid-cols-12 gap-6 mb-6 items-stretch">
          {/* Left column – controls + timer (Span 4) */}
          <div className="lg:col-span-4 flex flex-col gap-6">
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
                  {(isIdle || isPaused) && (
                    <button
                      onClick={handlePlay}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 bg-green-700 hover:bg-green-600 text-white transition"
                    >
                      <Play size={18} fill="currentColor" />
                      <span>{isPaused ? "Resume" : "Play"}</span>
                    </button>
                  )}

                  {isRunning && (
                    <button
                      onClick={handlePause}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 border border-yellow-600 text-yellow-700 hover:bg-yellow-50 transition"
                    >
                      <Pause size={18} />
                      <span>Pause</span>
                    </button>
                  )}

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
                {formatTime(displaySeconds)}
              </p>
              {result?.durationSeconds != null ? (
                <p className="text-center text-sm text-indigo-600 mt-2">
                  Final duration from results
                </p>
              ) : isPaused ? (
                <p className="text-center text-sm text-yellow-600 mt-2">
                  Paused
                </p>
              ) : null}
            </section>
          </div>

          {/* Right – VR Preview (Span 8 - Much Bigger View) */}
          <div className="lg:col-span-8">
            <section className="border border-gray-300 rounded-xl p-5 h-full flex flex-col">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-medium">VR Preview</h2>
                <p className="text-sm text-gray-500">
                  Click inside the preview window, To exit, press Esc key twice
                </p>
                {showPreview && (
                  <button
                    onClick={() => setShowPreview(false)}
                    className="text-gray-500 hover:text-black transition"
                  >
                    <X size={20} />
                  </button>
                )}
              </div>

              <div className="flex-1 min-h-[500px] bg-gray-100 rounded-lg overflow-hidden relative">
                {!showPreview ? (
                  <button
                    onClick={() => setShowPreview(true)}
                    disabled={!previewSceneName}
                    className="w-full h-full flex flex-col items-center justify-center text-gray-500 hover:bg-gray-200 transition disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <Box size={48} className="mb-3" />
                    <p className="font-medium text-lg">
                      {previewSceneName
                        ? "Click to Preview Environment"
                        : "Preview not available"}
                    </p>
                    <p className="text-sm mt-1">
                      {previewSceneName
                        ? experiment.experimentName || "3D Scene"
                        : "No preview scene mapped for this experiment"}
                    </p>
                  </button>
                ) : (
                  <iframe
                    src={`/previews/all-experiments/index.html?scene=${previewSceneName}`}
                    className="w-full h-full border-0"
                    allow="fullscreen"
                    title="Experiment Preview"
                    onError={(e) => e.preventDefault()}
                  />
                )}
              </div>
            </section>
          </div>
        </div>

        {/* Experiment Results & Configurations Section */}
        {result && (
          <section className="border border-gray-300 rounded-xl p-5 mb-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-medium flex items-center gap-2">
                <CheckCircle2 size={20} className="text-indigo-600" />
                <span>Captured Experiment Results</span>
              </h2>
              {result.duration && (
                <span className="text-sm bg-gray-100 px-3 py-1 rounded-full text-gray-600">
                  Duration: {result.duration}
                </span>
              )}
            </div>

            <div className="space-y-6">
              {/* Configurations block (if available) */}
              {result.configurations && (
                <div>
                  <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                    Configurations
                  </h3>
                  {renderDynamicDataNode(result.configurations)}
                </div>
              )}

              {/* Experimental Results block (if available) */}
              {result.experimentalResults && (
                <div>
                  <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                    Experimental Results & Metrics
                  </h3>
                  {renderDynamicDataNode(result.experimentalResults)}
                </div>
              )}

              {/* Fallback general keys if custom structures aren't matched */}
              {Object.keys(result).filter(
                (k) =>
                  !["id", "progressId", "experimentId", "experimentName", "blockId", "groupId", "userId", "studentId", "configurations", "experimentalResults", "logs", "timestamp", "startedAt", "completedAt", "duration", "durationSeconds", "completionStatus"].includes(k)
              ).length > 0 && (
                <div>
                  <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                    Additional Parameters
                  </h3>
                  {renderDynamicDataNode(
                    Object.fromEntries(
                      Object.entries(result).filter(
                        ([k]) =>
                          !["id", "progressId", "experimentId", "experimentName", "blockId", "groupId", "userId", "studentId", "configurations", "experimentalResults", "logs", "timestamp", "startedAt", "completedAt", "duration", "durationSeconds", "completionStatus"].includes(k)
                      )
                    )
                  )}
                </div>
              )}
            </div>
          </section>
        )}

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