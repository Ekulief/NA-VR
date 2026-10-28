import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  doc,
  getDoc,
  collection,
  onSnapshot,
  query,
  where,
} from "firebase/firestore";

import { db } from "../../config/firebase-config";
import { useAuth } from "../../context/AuthContext";

import {
  ArrowLeft,
  ChevronDown,
  ChevronRight,
  Download,
  Users,
  Settings,
  CheckCircle2,
} from "lucide-react";

/* ------------------------------------------------------------------ */
/* Helpers (CSV logic mirrors StudentExperimentRun's Jamovi export)    */
/* ------------------------------------------------------------------ */

const EXCLUDE_KEYS = new Set([
  "moduleId",
  "config_globalInstructionDelay",
  "globalInstructionDelay",
  "config_oddItem_AvailableTargets",
  "config_oddItem_RaycastDistance",
  "config_ab_AvailableAudioClips",
  "config_ab_FurnitureList",
]);

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

// Backend-only config keys that shouldn't be shown to instructors
const HIDDEN_DISPLAY_KEYS = new Set(
  [
    // Attentional Blindness
    "ab_AvailableAudioClips",
    "ab_FurnitureList",
    "ab_SpecificAudioClipIndex",
    // Depth Perception
    "depth_MinHeightMeters",
    "depth_MaxHeightMeters",
    // Distance Perception
    "distance_MinDistanceMeters",
    "distance_MaxDistanceMeters",
    // Odd Item Detection
    "oddItem_AvailableTargets",
    "oddItem_TargetDisplayName",
  ].map((k) => k.toLowerCase()),
);

const omitHiddenKeys = (value) => {
  const obj = normalizeForFlatten(value);
  if (!obj || typeof obj !== "object" || Array.isArray(obj)) return obj;
  return Object.fromEntries(
    Object.entries(obj).filter(
      ([key]) => !HIDDEN_DISPLAY_KEYS.has(key.toLowerCase()),
    ),
  );
};

const toMillis = (value) => {
  if (!value) return 0;
  if (typeof value.toMillis === "function") return value.toMillis();
  const t = new Date(value).getTime();
  return Number.isNaN(t) ? 0 : t;
};

const formatDateTime = (value) => {
  const ms = toMillis(value);
  if (!ms) return "—";
  return new Date(ms).toLocaleString("en-US", {
    month: "2-digit",
    day: "2-digit",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
};

const formatDuration = (seconds) => {
  if (seconds === null || seconds === undefined || seconds === "") return "—";
  const total = Math.round(Number(seconds));
  if (Number.isNaN(total)) return "—";
  const m = Math.floor(total / 60);
  const s = total % 60;
  return `${m}:${String(s).padStart(2, "0")}`;
};

const csvEscape = (v) => {
  if (v === null || v === undefined) return "";
  const s = String(v);
  return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
};

const downloadCSV = (rows, fileName) => {
  if (rows.length === 0) {
    alert("No data to export.");
    return;
  }

  const headers = [...new Set(rows.flatMap((r) => Object.keys(r)))].filter(
    (h) => !EXCLUDE_KEYS.has(h),
  );

  const preferred = [
    "groupName",
    "participantCode",
    "attemptNumber",
    "experimentName",
    "durationSeconds",
    "completedAt",
    "completionStatus",
    "vrId",
  ];

  const ordered = [
    ...preferred.filter((h) => headers.includes(h)),
    ...headers.filter((h) => !preferred.includes(h)),
  ];

  const csv = [
    ordered.join(","),
    ...rows.map((row) => ordered.map((h) => csvEscape(row[h])).join(",")),
  ].join("\n");

  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
};

const renderDataNode = (data) => {
  if (data === null || data === undefined)
    return <span className="text-gray-400">—</span>;

  if (Array.isArray(data)) {
    if (data.length === 0)
      return <span className="text-gray-400">Empty list</span>;

    // Lists of plain values render as compact chips instead of tall cards
    const allPrimitive = data.every(
      (item) => item === null || typeof item !== "object",
    );

    if (allPrimitive) {
      return (
        <div className="flex flex-wrap gap-2 w-full">
          {data.map((item, idx) => (
            <span
              key={idx}
              className="bg-gray-50 border border-gray-200 rounded-full px-3 py-1 text-sm font-medium text-gray-900"
            >
              {String(item)}
            </span>
          ))}
        </div>
      );
    }

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
            {renderDataNode(item)}
          </div>
        ))}
      </div>
    );
  }

  if (typeof data === "object") {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full items-start">
        {Object.entries(data).map(([key, value]) => (
          <div
            key={key}
            className={`bg-white border border-gray-200 rounded-lg p-3 flex flex-col gap-1 ${
              value !== null && typeof value === "object"
                ? "sm:col-span-2"
                : ""
            }`}
          >
            <span className="text-xs font-medium text-gray-500 capitalize mb-1">
              {key.replace(/([A-Z])/g, " $1")}
            </span>
            <div className="text-sm font-medium text-gray-900 break-words">
              {typeof value === "object" ? renderDataNode(value) : String(value)}
            </div>
          </div>
        ))}
      </div>
    );
  }

  return <span>{String(data)}</span>;
};

/* ------------------------------------------------------------------ */
/* Component                                                           */
/* ------------------------------------------------------------------ */

export default function InstructorExperimentOverview() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();
  const { user } = useAuth();

  const [experiment, setExperiment] = useState(null);
  const [groupDocs, setGroupDocs] = useState({});
  const [allProgress, setAllProgress] = useState([]);
  const [allResults, setAllResults] = useState([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [expandedGroups, setExpandedGroups] = useState({});
  const [expandedAttempts, setExpandedAttempts] = useState({});

  /* ---------- Load experiment + verify ownership ---------- */
  useEffect(() => {
    if (!experimentId || !blockId || !user?.uid) return;

    let unsubscribe = () => {};
    let cancelled = false;

    const init = async () => {
      try {
        setLoading(true);
        setError("");

        const blockSnap = await getDoc(doc(db, "block", blockId));
        if (!blockSnap.exists()) {
          setError("Course not found.");
          setLoading(false);
          return;
        }
        if (blockSnap.data().instructorId !== user.uid) {
          setError("This class is not assigned to your instructor account.");
          setLoading(false);
          return;
        }

        unsubscribe = onSnapshot(
          doc(db, "experiment", experimentId),
          (snap) => {
            if (cancelled) return;
            if (!snap.exists()) {
              setError("Experiment not found.");
              setExperiment(null);
            } else {
              setExperiment({ id: snap.id, ...snap.data() });
              setError("");
            }
            setLoading(false);
          },
          (err) => {
            console.error(err);
            setError("Unable to load the experiment.");
            setLoading(false);
          },
        );
      } catch (err) {
        console.error(err);
        setError("Unable to load the experiment.");
        setLoading(false);
      }
    };

    init();

    return () => {
      cancelled = true;
      unsubscribe();
    };
  }, [experimentId, blockId, user?.uid]);

  /* ---------- Live progress ---------- */
  useEffect(() => {
    if (!experimentId) return;

    const q = query(
      collection(db, "experimentProgress"),
      where("experimentId", "==", experimentId),
    );

    return onSnapshot(
      q,
      (snapshot) =>
        setAllProgress(snapshot.docs.map((d) => ({ id: d.id, ...d.data() }))),
      (err) => console.error("Error listening to progress:", err),
    );
  }, [experimentId]);

  /* ---------- Live results ---------- */
  useEffect(() => {
    if (!experimentId) return;

    const q = query(
      collection(db, "experimentResults"),
      where("experimentId", "==", experimentId),
    );

    return onSnapshot(
      q,
      (snapshot) =>
        setAllResults(snapshot.docs.map((d) => ({ id: d.id, ...d.data() }))),
      (err) => console.error("Error listening to results:", err),
    );
  }, [experimentId]);

  /* ---------- Group ids = assigned groups + any group with progress ---------- */
  const groupIds = useMemo(() => {
    const ids = new Set(experiment?.groupIds || []);
    allProgress.forEach((p) => p.groupId && ids.add(p.groupId));
    return [...ids];
  }, [experiment?.groupIds, allProgress]);

  const groupIdsKey = groupIds.join("|");

  useEffect(() => {
    let cancelled = false;

    const missing = groupIds.filter((id) => !groupDocs[id]);
    if (missing.length === 0) return;

    (async () => {
      const entries = await Promise.all(
        missing.map(async (id) => {
          try {
            const snap = await getDoc(doc(db, "group", id));
            return [id, snap.exists() ? { id, ...snap.data() } : { id }];
          } catch (err) {
            console.error(`Error fetching group ${id}:`, err);
            return [id, { id }];
          }
        }),
      );
      if (!cancelled) {
        setGroupDocs((current) => ({
          ...current,
          ...Object.fromEntries(entries),
        }));
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groupIdsKey]);

  /* ---------- Latest result per progress ---------- */
  const resultByProgressId = useMemo(() => {
    const map = {};
    allResults.forEach((r) => {
      if (!r.progressId) return;
      const existing = map[r.progressId];
      if (!existing || toMillis(r.timestamp) > toMillis(existing.timestamp)) {
        map[r.progressId] = r;
      }
    });
    return map;
  }, [allResults]);

  /* ---------- Build per-group view model ---------- */
  const groups = useMemo(() => {
    return groupIds
      .map((groupId) => {
        const groupDoc = groupDocs[groupId] || { id: groupId };
        const progressList = allProgress.filter((p) => p.groupId === groupId);

        const participantMap = {};
        progressList.forEach((p) => {
          const code = p.participantCode || "UNKNOWN";
          if (!participantMap[code]) {
            participantMap[code] = { participantCode: code, attempts: [] };
          }
          participantMap[code].attempts.push({
            progress: p,
            result: resultByProgressId[p.id] || null,
          });
        });

        const participants = Object.values(participantMap)
          .map((entry) => ({
            ...entry,
            attempts: entry.attempts.sort(
              (a, b) =>
                (a.progress.attemptNumber || 0) -
                (b.progress.attemptNumber || 0),
            ),
          }))
          .sort((a, b) =>
            a.participantCode.localeCompare(b.participantCode, undefined, {
              numeric: true,
            }),
          );

        const isDone =
          experiment?.dataCollectionByGroup?.[groupId]?.status === "done";
        const live = progressList.filter((p) => p.sessionControl !== "ended");
        const running = live.find((p) => p.sessionControl === "running");
        const paused = live.find((p) => p.sessionControl === "paused");

        let status = "Not Started";
        let tone = "bg-gray-100 text-gray-600";

        if (isDone) {
          status = "Done";
          tone = "bg-indigo-100 text-indigo-700";
        } else if (running) {
          status = "Running";
          tone = "bg-green-100 text-green-700";
        } else if (paused) {
          status = "Paused";
          tone = "bg-yellow-100 text-yellow-700";
        } else if (live.length > 0) {
          status = "Ready";
          tone = "bg-blue-100 text-blue-700";
        } else if (progressList.length > 0) {
          status = "Collecting";
          tone = "bg-blue-100 text-blue-700";
        }

        const resultCount = progressList.filter(
          (p) => resultByProgressId[p.id],
        ).length;

        return {
          id: groupId,
          name: groupDoc.groupName || "Unnamed group",
          memberCount: groupDoc.studentIds?.length || 0,
          participants,
          attemptCount: progressList.length,
          resultCount,
          hasResults: resultCount > 0,
          status,
          tone,
        };
      })
      .sort((a, b) =>
        a.name.localeCompare(b.name, undefined, { numeric: true }),
      );
  }, [groupIds, groupDocs, allProgress, resultByProgressId, experiment]);

  const configuration = useMemo(
    () => normalizeForFlatten(experiment?.configuration) || null,
    [experiment?.configuration],
  );

  /* ---------- CSV ---------- */
  const buildRows = (targetGroups) => {
    const configFlat = flattenObject(configuration || {}, "config");
    const rows = [];

    targetGroups.forEach((group) => {
      group.participants.forEach((participant) => {
        participant.attempts.forEach(({ progress: p, result }) => {
          const completed =
            p.completionStatus === "Completed" || p.sessionControl === "ended";
          if (!completed && !result) return;

          const resultData = result || {};

          const base = {
            groupName: group.name,
            participantCode: p.participantCode || "",
            attemptNumber: p.attemptNumber || 1,
            experimentName: experiment?.experimentName || experiment?.name || "",
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

          const resultsFlat = resultData.experimentalResults
            ? flattenObject(
                normalizeForFlatten(resultData.experimentalResults) || {},
                "result",
              )
            : {};

          rows.push({ ...base, ...configFlat, ...resultsFlat });
        });
      });
    });

    return rows;
  };

  const safeName = (experiment?.experimentName || experiment?.name || "results")
    .toString()
    .replace(/\s+/g, "_");

  const handleGroupCSV = (group) => {
    try {
      downloadCSV(
        buildRows([group]),
        `${safeName}_${group.name.replace(/\s+/g, "_")}_jamovi.csv`,
      );
    } catch (err) {
      console.error("CSV export failed:", err);
      alert("Failed to export CSV. Please try again.");
    }
  };

  const handleAllCSV = () => {
    try {
      downloadCSV(
        buildRows(groups.filter((g) => g.hasResults)),
        `${safeName}_all_groups_jamovi.csv`,
      );
    } catch (err) {
      console.error("CSV export failed:", err);
      alert("Failed to export CSV. Please try again.");
    }
  };

  const toggleGroup = (id) =>
    setExpandedGroups((c) => ({ ...c, [id]: !c[id] }));
  const toggleAttempt = (id) =>
    setExpandedAttempts((c) => ({ ...c, [id]: !c[id] }));

  /* ---------- Render ---------- */
  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p className="text-gray-500">Loading experiment...</p>
      </div>
    );
  }

  if (error || !experiment) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p className="text-red-500">{error || "Experiment not found."}</p>
      </div>
    );
  }

  const anyResults = groups.some((g) => g.hasResults);
  const title = experiment.name || experiment.experimentName;

  return (
    <div className="px-60 font-google min-h-screen text-black bg-white">
      <div className="px-6 pt-24 pb-4">
        <button
          onClick={() => navigate(`/instructor/course/${blockId}`)}
          className="flex items-center gap-2 text-gray-600 hover:text-black text-lg transition"
        >
          <ArrowLeft size={20} />
          Back to Experiments
        </button>
      </div>

      <div className="px-6 pb-10">
        {/* Header */}
        <div className="flex items-start justify-between gap-4 mb-2">
          <div>
            <h1 className="text-3xl font-semibold">{title}</h1>
            <p className="text-xl text-gray-600 mt-2">
              {experiment.moduleDescription || "No description available."}
            </p>
          </div>

          {anyResults && (
            <button
              onClick={handleAllCSV}
              className="flex items-center gap-2 px-5 py-3 bg-indigo-800 hover:bg-indigo-700 text-white rounded-lg transition whitespace-nowrap"
            >
              <Download size={20} />
              Download all groups (CSV)
            </button>
          )}
        </div>

        {/* Group statuses */}
        <section className="mt-8">
          <h2 className="text-2xl font-medium flex items-center gap-2 mb-4">
            <Users size={24} />
            Group Status
          </h2>

          {groups.length === 0 && (
            <div className="border border-gray-300 rounded-2xl py-10 text-center text-gray-500">
              No groups are assigned to this experiment.
            </div>
          )}

          <div className="space-y-4">
            {groups.map((group) => {
              const open = !!expandedGroups[group.id];

              return (
                <div
                  key={group.id}
                  className="border border-gray-300 rounded-2xl overflow-hidden"
                >
                  <button
                    type="button"
                    disabled={!group.hasResults}
                    onClick={() => toggleGroup(group.id)}
                    className={`w-full flex items-center justify-between gap-4 px-5 py-5 text-left transition ${
                      group.hasResults
                        ? "hover:bg-gray-50 cursor-pointer"
                        : "cursor-default"
                    }`}
                  >
                    <div className="flex items-center gap-4 flex-wrap">
                      <h3 className="text-xl font-semibold">{group.name}</h3>
                      <span
                        className={`px-3 py-1 rounded-full text-sm ${group.tone}`}
                      >
                        {group.status}
                      </span>
                    </div>

                    <div className="flex items-center gap-8 text-gray-600">
                      <span>Members: {group.memberCount}</span>
                      <span>Participants: {group.participants.length}</span>
                      <span>Attempts: {group.attemptCount}</span>
                      <span>Results: {group.resultCount}</span>
                      {group.hasResults &&
                        (open ? (
                          <ChevronDown size={22} />
                        ) : (
                          <ChevronRight size={22} />
                        ))}
                    </div>
                  </button>

                  {!group.hasResults && (
                    <p className="px-5 pb-4 text-sm text-gray-400 -mt-2">
                      No results yet.
                    </p>
                  )}

                  {group.hasResults && open && (
                    <div className="border-t border-gray-200 bg-gray-50 px-5 py-5">
                      <div className="flex justify-end mb-4">
                        <button
                          onClick={() => handleGroupCSV(group)}
                          className="flex items-center gap-2 px-4 py-2 bg-indigo-800 hover:bg-indigo-700 text-white rounded-lg transition"
                        >
                          <Download size={18} />
                          Download CSV
                        </button>
                      </div>

                      <div className="space-y-5">
                        {group.participants.map((participant) => (
                          <div
                            key={participant.participantCode}
                            className="bg-white border border-gray-200 rounded-xl p-4"
                          >
                            <h4 className="text-lg font-medium mb-3">
                              {participant.participantCode}
                            </h4>

                            <div className="space-y-2">
                              {participant.attempts.map(
                                ({ progress: p, result }) => {
                                  const attemptOpen = !!expandedAttempts[p.id];

                                  return (
                                    <div
                                      key={p.id}
                                      className="border border-gray-200 rounded-lg"
                                    >
                                      <button
                                        type="button"
                                        disabled={!result}
                                        onClick={() => toggleAttempt(p.id)}
                                        className={`w-full flex items-center justify-between gap-4 px-4 py-3 text-left ${
                                          result
                                            ? "hover:bg-gray-50 cursor-pointer"
                                            : "cursor-default"
                                        }`}
                                      >
                                        <div className="flex items-center gap-3">
                                          {result && (
                                            <CheckCircle2
                                              size={18}
                                              className="text-indigo-600"
                                            />
                                          )}
                                          <span className="font-medium">
                                            Attempt {p.attemptNumber || 1}
                                          </span>
                                          <span className="text-sm text-gray-500">
                                            {p.completionStatus ||
                                              (p.sessionControl === "ended"
                                                ? "Completed"
                                                : p.sessionControl || "—")}
                                          </span>
                                        </div>

                                        <div className="flex items-center gap-6 text-sm text-gray-600">
                                          <span>
                                            Duration:{" "}
                                            {formatDuration(
                                              result?.durationSeconds ??
                                                p.elapsedSeconds,
                                            )}
                                          </span>
                                          <span>
                                            {formatDateTime(
                                              result?.completedAt ||
                                                result?.timestamp ||
                                                p.completionAt,
                                            )}
                                          </span>
                                          {p.vrId && <span>{p.vrId}</span>}
                                          {result &&
                                            (attemptOpen ? (
                                              <ChevronDown size={18} />
                                            ) : (
                                              <ChevronRight size={18} />
                                            ))}
                                        </div>
                                      </button>

                                      {result && attemptOpen && (
                                        <div className="border-t border-gray-200 px-4 py-4 space-y-5">
                                          {result.configurations && (
                                            <div>
                                              <h5 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                                                Configurations
                                              </h5>
                                              {renderDataNode(
                                                omitHiddenKeys(
                                                  result.configurations,
                                                ),
                                              )}
                                            </div>
                                          )}

                                          {result.experimentalResults && (
                                            <div>
                                              <h5 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                                                Experimental Results & Metrics
                                              </h5>
                                              {renderDataNode(
                                                result.experimentalResults,
                                              )}
                                            </div>
                                          )}
                                        </div>
                                      )}
                                    </div>
                                  );
                                },
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </section>

        {/* Configuration overview */}
        <section className="mt-10">
          <h2 className="text-2xl font-medium flex items-center gap-2 mb-4">
            <Settings size={24} />
            Configuration Overview
          </h2>

          <div className="border border-gray-300 rounded-2xl p-5 space-y-5">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {[
                ["Environment", experiment.environment],
                ["Module", experiment.moduleId || experiment.sceneId],
                ["Status", experiment.status || "Available"],
              ].map(([label, value]) => (
                <div
                  key={label}
                  className="bg-white border border-gray-200 rounded-lg p-3"
                >
                  <span className="text-xs font-medium text-gray-500 block mb-1">
                    {label}
                  </span>
                  <span className="text-sm font-medium text-gray-900 break-words">
                    {value || "N/A"}
                  </span>
                </div>
              ))}
            </div>

            {configuration ? (
              <div>
                <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                  Settings
                </h3>
                {renderDataNode(omitHiddenKeys(configuration))}
              </div>
            ) : (
              <p className="text-gray-400">
                No configuration has been saved for this experiment.
              </p>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}