import { useEffect, useRef, useState, useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  doc,
  getDoc,
  collection,
  query,
  where,
  updateDoc,
  onSnapshot,
  serverTimestamp,
  getDocs,
} from "firebase/firestore";

import { db } from "../../config/firebase-config";
import { useAuth } from "../../context/AuthContext";
import { getFunctions, httpsCallable } from "firebase/functions";

import {
  ArrowLeft,
  Box,
  Play,
  Pause,
  Square,
  Download,
  Loader2,
  X,
  CheckCircle2,
  UserPlus,
  RotateCcw,
  Users,
} from "lucide-react";

export default function StudentExperimentRun() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();
  const { user } = useAuth();

  const [experiment, setExperiment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [allProgress, setAllProgress] = useState([]);
  const [activeProgressId, setActiveProgressId] = useState(null);

  const [result, setResult] = useState(null);
  const [logs, setLogs] = useState([]);

  const [connecting, setConnecting] = useState(false);
  const [markingDone, setMarkingDone] = useState(false);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const timerRef = useRef(null);
  const initializedRef = useRef(false);

  const [resolvedGroupId, setResolvedGroupId] = useState(null);
  const [showPreview, setShowPreview] = useState(false);

  // ---------- Derived ----------
  const progress = useMemo(() => {
    if (!activeProgressId) return null;
    return allProgress.find((p) => p.id === activeProgressId) || null;
  }, [allProgress, activeProgressId]);

  const participants = useMemo(() => {
    const map = {};
    allProgress.forEach((p) => {
      const code = p.participantCode || "UNKNOWN";
      if (!map[code]) {
        map[code] = { participantCode: code, attempts: [] };
      }
      map[code].attempts.push(p);
    });

    Object.values(map).forEach((entry) => {
      entry.attempts.sort(
        (a, b) => (a.attemptNumber || 0) - (b.attemptNumber || 0)
      );
    });

    return Object.values(map).sort((a, b) =>
      a.participantCode.localeCompare(b.participantCode, undefined, {
        numeric: true,
      })
    );
  }, [allProgress]);

  const currentParticipantCode = progress?.participantCode || null;
  const currentAttemptNumber = progress?.attemptNumber || null;

  const nextParticipantCode = useMemo(() => {
    if (participants.length === 0) return "P01";
    const nums = participants
      .map((p) => parseInt(String(p.participantCode).replace(/\D/g, ""), 10))
      .filter((n) => !isNaN(n));
    const max = nums.length ? Math.max(...nums) : 0;
    return `P${String(max + 1).padStart(2, "0")}`;
  }, [participants]);

  // Per-group done status (not whole experiment)
  const isDataCollectionDone = Boolean(
    resolvedGroupId &&
      experiment?.dataCollectionByGroup?.[resolvedGroupId]?.status === "done"
  );

  const previewSceneMap = {
    Depth_Perception: "DepthPerception_Preview",
    Odd_Item_Detection: "OddItem_Preview",
    Attentional_Blindness: "AttentionalBlindness_Preview",
    Memory2: "MemoryHouse_Preview",
    Distance_Perception: "DistancePerception_Preview",
    Memory: "MemoryDiner_Preview",
  };

  const getPreviewSceneName = () => {
    if (!experiment) return null;
    const key = experiment.moduleId || experiment.sceneId;
    return previewSceneMap[key] || null;
  };

  // ---------- Load experiment ----------
  useEffect(() => {
    if (!experimentId) return;

    const experimentRef = doc(db, "experiment", experimentId);

    // Live listener so other devices / refresh pick up dataCollectionByGroup
    const unsubscribe = onSnapshot(
      experimentRef,
      (snap) => {
        if (!snap.exists()) {
          setError("Experiment not found.");
          setExperiment(null);
          setLoading(false);
          return;
        }
        setExperiment({ id: snap.id, ...snap.data() });
        setError("");
        setLoading(false);
      },
      (err) => {
        console.error(err);
        setError("Unable to load the experiment.");
        setLoading(false);
      }
    );

    return () => unsubscribe();
  }, [experimentId]);

  // ---------- Resolve group ----------
  useEffect(() => {
    if (!experiment || !user?.uid) return;

    const resolveGroup = async () => {
      const experimentGroupIds = experiment.groupIds || [];
      if (experimentGroupIds.length === 0) {
        setResolvedGroupId(null);
        return;
      }

      for (const groupId of experimentGroupIds) {
        try {
          const groupRef = doc(db, "group", groupId);
          const groupSnap = await getDoc(groupRef);
          if (groupSnap.exists()) {
            const groupData = groupSnap.data();
            const members = groupData.studentIds || [];
            if (
              members.includes(user.uid) ||
              groupData.studentId === user.uid
            ) {
              setResolvedGroupId(groupSnap.id);
              return;
            }
          }
        } catch (err) {
          console.error(`Error fetching group ${groupId}:`, err);
        }
      }

      setResolvedGroupId(experimentGroupIds[0] || null);
    };

    resolveGroup();
  }, [experiment, user?.uid]);

  // ---------- Listen to all progress ----------
  useEffect(() => {
    if (!experimentId) return;

    let q;
    if (resolvedGroupId) {
      q = query(
        collection(db, "experimentProgress"),
        where("experimentId", "==", experimentId),
        where("groupId", "==", resolvedGroupId)
      );
    } else {
      q = query(
        collection(db, "experimentProgress"),
        where("experimentId", "==", experimentId)
      );
    }

    const unsubscribe = onSnapshot(
      q,
      (snapshot) => {
        const docs = snapshot.docs.map((d) => ({ id: d.id, ...d.data() }));

        docs.sort((a, b) => {
          const ta =
            a.createdAt?.toMillis?.() ||
            new Date(a.createdAt || 0).getTime() ||
            0;
          const tb =
            b.createdAt?.toMillis?.() ||
            new Date(b.createdAt || 0).getTime() ||
            0;
          return tb - ta;
        });

        setAllProgress(docs);

        setActiveProgressId((prev) => {
          const prevDoc = prev ? docs.find((d) => d.id === prev) : null;

          if (prevDoc && prevDoc.sessionControl !== "ended") {
            return prev;
          }

          const live = docs.find((d) => d.sessionControl !== "ended");
          if (live) return live.id;

          if (prevDoc) return prev;
          return docs[0]?.id ?? null;
        });
      },
      (err) => {
        console.error("Error listening to progress:", err);
      }
    );

    return () => unsubscribe();
  }, [experimentId, resolvedGroupId]);

  // ---------- Result for active progress ----------
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

  // ---------- Logs for active progress ----------
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
    initializedRef.current = false;
    if (progress?.elapsedSeconds != null) {
      setElapsedSeconds(progress.elapsedSeconds);
      initializedRef.current = true;
    } else {
      setElapsedSeconds(0);
    }
  }, [progress?.id]);

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
    const m = Math.floor(seconds / 60).toString().padStart(2, "0");
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
    result?.durationSeconds != null ? result.durationSeconds : elapsedSeconds;

  // ---------- Create progress ----------
  const createProgress = async ({ participantCode, attemptNumber }) => {
    if (!experiment || !user?.uid || connecting || isDataCollectionDone) return;

    const vrId = experiment.vrId;
    if (!vrId) {
      alert("This experiment has no VR headset assigned.");
      return;
    }

    try {
      setConnecting(true);

      // A trusted callable creates progress and sets assignedInstructorId +
      // currentBlockId from the actual block owner in a single transaction.
      const startSession = httpsCallable(getFunctions(), "startVrExperimentSession");
      const reply = await startSession({
        blockId,
        experimentId,
        groupId: resolvedGroupId,
        vrId,
        participantCode,
        attemptNumber,
      });
      setActiveProgressId(reply.data.progressId);
    } catch (err) {
      console.error("Cannot connect VR:", err);
      alert(err?.code === "functions/already-exists"
        ? "This VR headset is currently in use."
        : err?.message || "Unable to start participant / attempt.");
    } finally {
      setConnecting(false);
    }
  };

  const handleStartNextParticipant = async () => {
    if (isDataCollectionDone) return;

    const confirmed = window.confirm(
      `Start ${nextParticipantCode}?\n\nThis will create Attempt 1 for a new participant.`
    );
    if (!confirmed) return;

    await createProgress({
      participantCode: nextParticipantCode,
      attemptNumber: 1,
    });
  };

  const handleStartNewAttempt = async () => {
    if (!currentParticipantCode || isDataCollectionDone) return;

    const existingAttempts =
      participants.find((p) => p.participantCode === currentParticipantCode)
        ?.attempts || [];

    const nextAttempt =
      existingAttempts.length > 0
        ? Math.max(...existingAttempts.map((a) => a.attemptNumber || 0)) + 1
        : 1;

    const confirmed = window.confirm(
      `Start Attempt ${nextAttempt} for ${currentParticipantCode}?`
    );
    if (!confirmed) return;

    if (progress && progress.sessionControl !== "ended") {
      await updateSessionControl("ended");
      await releaseVrLock(progress.id, progress.vrId || experiment.vrId);
    }

    await createProgress({
      participantCode: currentParticipantCode,
      attemptNumber: nextAttempt,
    });
  };

  // ---------- Session control ----------
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

  const releaseVrLock = async (progressId) => {
    if (!progressId) return;
    try {
      const releaseSession = httpsCallable(getFunctions(), "releaseVrExperimentSession");
      await releaseSession({ progressId });
    } catch (err) {
      console.error("Failed to release VR session:", err);
      alert(err?.message || "Unable to release the VR headset.");
    }
  };

  const handleResume = () => {
    if (progress?.sessionControl !== "paused") return;
    updateSessionControl("running");
  };

  const handlePause = () => {
    if (progress?.sessionControl !== "running") return;
    updateSessionControl("paused");
  };

  const handleEndAttempt = async () => {
    const confirmed = window.confirm(
      "End this attempt? The participant can still do another attempt later."
    );
    if (!confirmed) return;

    await updateSessionControl("ended");
    await releaseVrLock(progress.id, progress.vrId || experiment.vrId);
  };

  // ---------- Mark data collection done (per group) ----------
  const handleMarkDataCollectionDone = async () => {
    if (!experimentId || !user?.uid || !resolvedGroupId || isDataCollectionDone)
      return;

    const hasLive = allProgress.some((p) => p.sessionControl !== "ended");
    if (hasLive) {
      const ok = window.confirm(
        "There is still an active attempt.\n\nMark data collection as done for your group anyway? You should end the current attempt first if possible."
      );
      if (!ok) return;
    } else {
      const ok = window.confirm(
        "Mark data collection as done for your group?\n\nThe instructor will see your group as finished. Other groups assigned to this experiment are not affected. You will not be able to start new participants from this page."
      );
      if (!ok) return;
    }

    try {
      setMarkingDone(true);

      await updateDoc(doc(db, "experiment", experimentId), {
        [`dataCollectionByGroup.${resolvedGroupId}`]: {
          status: "done",
          completedAt: serverTimestamp(),
          completedBy: user.uid,
        },
        updatedAt: serverTimestamp(),
      });

      // Local state updates immediately; onSnapshot will also refresh
      setExperiment((prev) => {
        if (!prev) return prev;
        return {
          ...prev,
          dataCollectionByGroup: {
            ...(prev.dataCollectionByGroup || {}),
            [resolvedGroupId]: {
              status: "done",
              completedBy: user.uid,
            },
          },
        };
      });
    } catch (err) {
      console.error("Failed to mark data collection done:", err);
      alert("Unable to mark as done. Please try again.");
    } finally {
      setMarkingDone(false);
    }
  };

  // ---------- CSV helpers ----------
  const flattenObject = (obj, prefix = "") => {
    const out = {};
    if (obj === null || obj === undefined) return out;

    if (typeof obj !== "object" || Array.isArray(obj)) {
      out[prefix || "value"] = Array.isArray(obj) ? JSON.stringify(obj) : obj;
      return out;
    }

    for (const [k, v] of Object.entries(obj)) {
      const key = prefix ? `${prefix}_${k}` : k;
      if (v !== null && typeof v === "object" && !Array.isArray(v)) {
        Object.assign(out, flattenObject(v, key));
      } else if (Array.isArray(v)) {
        out[key] = JSON.stringify(v);
      } else {
        out[key] = v;
      }
    }
    return out;
  };

  const normalizeForFlatten = (value) => {
    if (
      Array.isArray(value) &&
      value.length === 1 &&
      typeof value[0] === "object"
    ) {
      return value[0];
    }
    return value;
  };

  const exportJamoviCSV = async (attempts, fileSuffix = "jamovi") => {
    try {
      const completed = attempts;

      if (completed.length === 0) {
        alert("No completed attempts to export yet.");
        return;
      }

      const rows = [];

      for (const p of completed) {
        const resultQuery = query(
          collection(db, "experimentResults"),
          where("progressId", "==", p.id)
        );
        const resultSnap = await getDocs(resultQuery);

        let resultData = {};
        if (!resultSnap.empty) {
          const docs = resultSnap.docs.map((d) => d.data());
          docs.sort((a, b) => {
            const ta = a.timestamp ? new Date(a.timestamp).getTime() : 0;
            const tb = b.timestamp ? new Date(b.timestamp).getTime() : 0;
            return tb - ta;
          });
          resultData = docs[0];
        }

        const base = {
          participantCode: p.participantCode || "",
          attemptNumber: p.attemptNumber || 1,
          experimentName: experiment?.experimentName || "",
          vrId: p.vrId || experiment?.vrId || "",
          durationSeconds:
            resultData.durationSeconds ?? p.elapsedSeconds ?? "",
          completedAt:
            resultData.completedAt ||
            resultData.timestamp ||
            p.completionAt ||
            "",
          completionStatus: p.completionStatus || "",
        };

        const configFlat = flattenObject(
          normalizeForFlatten(experiment?.configuration) || {},
          "config"
        );

        const resultsFlat = resultData.experimentalResults
          ? flattenObject(
              normalizeForFlatten(resultData.experimentalResults) || {},
              "result"
            )
          : {};

        rows.push({ ...base, ...configFlat, ...resultsFlat });
      }

      if (rows.length === 0) {
        alert("No data to export.");
        return;
      }

      const EXCLUDE_KEYS = new Set([
        "moduleId",
        "config_globalInstructionDelay",
        "globalInstructionDelay",
        "config_oddItem_AvailableTargets",
        "config_oddItem_RaycastDistance",
        "config_ab_AvailableAudioClips",
        "config_ab_FurnitureList",
      ]);

      const headers = [
        ...new Set(rows.flatMap((r) => Object.keys(r))),
      ].filter((h) => !EXCLUDE_KEYS.has(h));

      const preferred = [
        "participantCode",
        "attemptNumber",
        "experimentName",
        "durationSeconds",
        "completedAt",
        "completionStatus",
        "vrId",
      ];

      const orderedHeaders = [
        ...preferred.filter((h) => headers.includes(h)),
        ...headers.filter((h) => !preferred.includes(h)),
      ];

      const escape = (v) => {
        if (v === null || v === undefined) return "";
        const s = String(v);
        return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
      };

      const csvContent = [
        orderedHeaders.join(","),
        ...rows.map((row) =>
          orderedHeaders.map((h) => escape(row[h])).join(",")
        ),
      ].join("\n");

      const blob = new Blob([csvContent], {
        type: "text/csv;charset=utf-8;",
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `${(experiment?.experimentName || "results").replace(
        /\s+/g,
        "_"
      )}_${fileSuffix}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error("CSV export failed:", err);
      alert("Failed to export CSV. Please try again.");
    }
  };

  // All completed attempts for the group
  const handleDownloadJamoviCSV = () => {
    const completed = allProgress.filter(
      (p) => p.completionStatus === "Completed" || p.sessionControl === "ended"
    );
    return exportJamoviCSV(completed);
  };

  // Current attempt only
  const handleDownloadCurrentAttemptCSV = () => {
    if (!progress) {
      alert("No attempt selected.");
      return;
    }
    if (!result) {
      alert("No results for this attempt yet.");
      return;
    }
    const suffix = `${progress.participantCode || "P"}_A${
      progress.attemptNumber || 1
    }_jamovi`;
    return exportJamoviCSV([progress], suffix);
  };

  // ---------- Render helpers ----------
  const renderDynamicDataNode = (data) => {
    if (data === null || data === undefined)
      return <span className="text-gray-400">—</span>;

    if (Array.isArray(data)) {
      if (data.length === 0)
        return <span className="text-gray-400">Empty list</span>;

      return (
        <div className="space-y-3 w-full">
          {data.map((item, idx) => (
            <div
              key={idx}
              className="bg-gray-50 border border-gray-200 rounded-lg p-3"
            >
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
            <div
              key={key}
              className="bg-white border border-gray-200 rounded-lg p-3 flex flex-col justify-between"
            >
              <span className="text-xs font-medium text-gray-500 capitalize mb-1">
                {key.replace(/([A-Z])/g, " $1")}
              </span>
              <div className="text-sm font-medium text-gray-900 break-words">
                {typeof value === "object"
                  ? renderDynamicDataNode(value)
                  : String(value)}
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
          : "No active attempt";

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
          <div className="flex items-center gap-2">
            {isDataCollectionDone && (
              <span className="px-3 py-1 rounded-full text-sm bg-green-100 text-green-800 whitespace-nowrap">
                Group data collection done
              </span>
            )}
            <span
              className={`px-3 py-1 rounded-full text-sm whitespace-nowrap ${statusStyle}`}
            >
              {statusLabel}
            </span>
          </div>
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

        {/* Main grid */}
        <div className="grid lg:grid-cols-12 gap-6 mb-6 items-stretch">
          {/* Left column */}
          <div className="lg:col-span-4 flex flex-col gap-6">
            {/* Participant & Attempts */}
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4 flex items-center gap-2">
                <Users size={18} />
                Participant & Attempts
              </h2>

              {currentParticipantCode ? (
                <div className="mb-4 p-3 rounded-lg bg-indigo-50 border border-indigo-100">
                  <p className="text-xs text-indigo-600 font-medium uppercase tracking-wider">
                    Current Participant
                  </p>
                  <p className="text-2xl font-semibold text-indigo-900">
                    {currentParticipantCode}
                  </p>
                  <p className="text-sm text-indigo-700 mt-0.5">
                    Attempt {currentAttemptNumber}
                  </p>
                </div>
              ) : (
                <p className="text-sm text-gray-500 mb-4">
                  No active participant. Start one below.
                </p>
              )}

              <div className="flex flex-col gap-2 mb-4">
                <button
                  onClick={handleStartNextParticipant}
                  disabled={connecting || isDataCollectionDone}
                  className="w-full rounded-lg py-2.5 flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white transition disabled:opacity-60"
                >
                  {connecting ? (
                    <Loader2 size={16} className="animate-spin" />
                  ) : (
                    <UserPlus size={16} />
                  )}
                  <span>Start {nextParticipantCode}</span>
                </button>

                {currentParticipantCode && (
                  <button
                    onClick={handleStartNewAttempt}
                    disabled={connecting || isDataCollectionDone}
                    className="w-full rounded-lg py-2.5 flex items-center justify-center gap-2 border border-indigo-300 text-indigo-700 hover:bg-indigo-50 transition disabled:opacity-60"
                  >
                    <RotateCcw size={16} />
                    <span>New Attempt for {currentParticipantCode}</span>
                  </button>
                )}
              </div>

              {participants.length > 0 && (
                <div className="border-t border-gray-200 pt-3">
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                    All Participants
                  </p>
                  <div className="max-h-48 overflow-y-auto space-y-2">
                    {participants.map((p) => (
                      <div key={p.participantCode} className="text-sm">
                        <p className="font-medium text-gray-800">
                          {p.participantCode}
                        </p>
                        <div className="ml-2 space-y-0.5">
                          {p.attempts.map((a) => (
                            <button
                              key={a.id}
                              onClick={() => setActiveProgressId(a.id)}
                              className={`block w-full text-left px-2 py-1 rounded text-xs transition ${
                                a.id === activeProgressId
                                  ? "bg-indigo-100 text-indigo-800 font-medium"
                                  : "text-gray-600 hover:bg-gray-100"
                              }`}
                            >
                              Attempt {a.attemptNumber} ·{" "}
                              {a.sessionControl === "ended"
                                ? "Completed"
                                : a.sessionControl || "—"}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </section>

            {/* Session Control */}
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Control</h2>

              {!isConnected ? (
                <p className="text-sm text-gray-500">
                  Start a participant to begin controlling the session.
                </p>
              ) : (
                <div className="flex flex-col gap-3">
                  {isIdle && (
                    <div className="w-full rounded-lg py-3 px-4 flex items-center justify-center gap-2 bg-gray-100 border border-gray-200 text-gray-500">
                      <Loader2 size={18} className="animate-spin" />
                      <span>Waiting for participant to begin in VR…</span>
                    </div>
                  )}

                  {isPaused && (
                    <button
                      onClick={handleResume}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 bg-green-700 hover:bg-green-600 text-white transition"
                    >
                      <Play size={18} fill="currentColor" />
                      <span>Resume</span>
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
                      onClick={handleEndAttempt}
                      className="w-full rounded-lg py-3 flex items-center justify-center gap-2 bg-red-700 hover:bg-red-600 text-white transition"
                    >
                      <Square size={16} fill="currentColor" />
                      <span>End Attempt</span>
                    </button>
                  )}

                  {isEnded && (
                    <p className="text-center text-sm text-gray-500 py-2">
                      This attempt is completed
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
              ) : isIdle ? (
                <p className="text-center text-sm text-gray-500 mt-2">
                  Starts when the participant presses Begin Experiment
                </p>
              ) : null}
            </section>
          </div>

          {/* Right – VR Preview */}
          <div className="lg:col-span-8">
            <section className="border border-gray-300 rounded-xl p-5 h-full flex flex-col">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-medium">VR Preview</h2>
                <p className="text-sm text-gray-500">
                  Click inside the preview window. To exit, press Esc twice
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

        {/* Results */}
        {result && (
          <section className="border border-gray-300 rounded-xl p-5 mb-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-medium flex items-center gap-2">
                <CheckCircle2 size={20} className="text-indigo-600" />
                <span>
                  Results — {currentParticipantCode} · Attempt{" "}
                  {currentAttemptNumber}
                </span>
              </h2>
              {result.duration && (
                <span className="text-sm bg-gray-100 px-3 py-1 rounded-full text-gray-600">
                  Duration: {result.duration}
                </span>
              )}
            </div>

            <div className="space-y-6">
              {result.experimentalResults && (
                <div>
                  <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                    Experimental Results & Metrics
                  </h3>
                  {renderDynamicDataNode(result.experimentalResults)}
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
              Start a participant to begin receiving logs.
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

        {/* Mark data collection done (this group only) */}
        <section className="border border-gray-300 rounded-xl p-5 mb-6">
          <h2 className="text-lg font-medium mb-2">Data collection</h2>

          {!resolvedGroupId && (
            <p className="text-sm text-amber-700 mb-3">
              No group resolved for your account. Mark as done needs a group
              assignment.
            </p>
          )}

          {isDataCollectionDone ? (
            <div className="rounded-lg bg-green-50 border border-green-200 px-4 py-3 flex items-center gap-2 text-green-800">
              <CheckCircle2 size={18} />
              <div>
                <p className="font-medium">Your group is marked as done</p>
                <p className="text-sm text-green-700">
                  Only your group is finished. Other groups on this experiment
                  are not affected. The instructor can see your group status.
                </p>
              </div>
            </div>
          ) : (
            <>
              <p className="text-sm text-gray-500 mb-3">
                When your group has finished all participants, mark data
                collection as done. This only applies to your group — other
                groups assigned to this experiment can keep running.
              </p>
              <button
                onClick={handleMarkDataCollectionDone}
                disabled={markingDone || !resolvedGroupId}
                className="flex items-center justify-center gap-2 bg-gray-900 hover:bg-black text-white rounded-lg px-5 py-2.5 transition disabled:opacity-60"
              >
                {markingDone ? (
                  <Loader2 size={18} className="animate-spin" />
                ) : (
                  <CheckCircle2 size={18} />
                )}
                <span>Mark my group as done</span>
              </button>
            </>
          )}
        </section>

        {/* Export */}
        <section className="border border-gray-300 rounded-xl p-5">
          <h2 className="text-lg font-medium mb-4">Export Data</h2>
          <div className="flex flex-wrap gap-3">
            <button
              onClick={handleDownloadJamoviCSV}
              className="flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg px-5 py-2.5 transition"
            >
              <Download size={18} />
              <span>Download Jamovi CSV (all attempts)</span>
            </button>

            {isConnected && (
              <button
                onClick={handleDownloadCurrentAttemptCSV}
                className="flex items-center justify-center gap-2 border border-gray-300 hover:bg-gray-50 rounded-lg px-5 py-2.5 transition"
              >
                <Download size={18} />
                <span>Download this attempt (Jamovi CSV)</span>
              </button>
            )}
          </div>
          <p className="text-xs text-gray-500 mt-2">
            CSV contains one row per completed attempt for your group, ready
            for jamovi.
          </p>
        </section>
      </main>
    </div>
  );
}