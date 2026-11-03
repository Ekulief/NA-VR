import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getFunctions, httpsCallable } from "firebase/functions";
import { LiveKitRoom, VideoTrack, useTracks } from "@livekit/components-react";
import { Track } from "livekit-client";
import { ArrowLeft, RefreshCw, Radio, AlertTriangle, MonitorPlay, Maximize2, X } from "lucide-react";
import "@livekit/components-styles";
import { useAuth } from "../../context/AuthContext";

// The server verifies block ownership before returning ANY VR device metadata.
// This page doesn't need Firestore composite indexes or direct vrDevices read rules.
const POLL_INTERVAL_MS = 3000;

function StreamVideo({ deviceId, onHasVideo }) {
  const tracks = useTracks([Track.Source.Camera], { onlySubscribed: true });
  const track = tracks.find((t) => t.participant?.identity === `vr-${deviceId}`);
  useEffect(() => { onHasVideo(Boolean(track)); }, [track, onHasVideo]);
  if (!track) {
    return <div className="flex h-full items-center justify-center p-5 text-center text-sm text-gray-300">
      Viewer connected. Waiting for the Unity headset camera track. Check LiveKitVrPublisher on the headset.
    </div>;
  }
  // object-contain displays the entire video frame instead of cropping it.
  // The larger field of view is produced by Unity's separate spectator camera.
  return <div className="h-full w-full [&_video]:h-full [&_video]:w-full [&_video]:object-contain">
    <VideoTrack trackRef={track} />
  </div>;
}

function LiveStreamCard({ blockId, device, expanded, onExpand, onClose }) {
  const [tokenInfo, setTokenInfo] = useState(null);
  const [error, setError] = useState("");
  const [phase, setPhase] = useState("Requesting viewer token...");
  const [retry, setRetry] = useState(0);
  const [hasVideo, setHasVideo] = useState(false);
  const openButtonRef = useRef(null);
  const closeButtonRef = useRef(null);
  const wasExpanded = useRef(false);
  const canView = device.state === "ready";
  const experimentId = device.currentExperimentId;
  const progressId = device.currentProgressId;

  useEffect(() => {
    if (expanded) closeButtonRef.current?.focus();
    else if (wasExpanded.current) openButtonRef.current?.focus();
    wasExpanded.current = expanded;
  }, [expanded]);

  useEffect(() => {
    let canceled = false;
    // Schedule the visual reset after subscription effects have committed.
    // This avoids cascading renders while preserving viewer-token invalidation.
    Promise.resolve().then(() => {
      if (canceled) return;
      setTokenInfo(null);
      setError("");
      setPhase("Requesting viewer token...");
      setHasVideo(false);
    });
    if (!canView) return () => { canceled = true; };
    (async () => {
      try {
        const call = httpsCallable(getFunctions(), "createLiveStreamToken");
        const reply = await call({ blockId, vrDeviceId: device.id, experimentId, role: "viewer" });
        if (!canceled) {
          setTokenInfo(reply.data);
          setPhase("Connecting to LiveKit...");
        }
      } catch (err) {
        console.error("[Live VR] Viewer token error:", err);
        if (!canceled) setError(err?.message || "Could not request a viewer token.");
      }
    })();
    return () => { canceled = true; };
  }, [blockId, device.id, experimentId, progressId, canView, retry]);

  const textColor = hasVideo ? "text-green-700" : device.state === "needs-attention" ? "text-amber-700" : "text-gray-600";
  const stateLabel = hasVideo ? "LIVE VIDEO" : canView ? "ATTEMPT ACTIVE" : device.state === "waiting-vr" ? "WAITING FOR VR" : device.state === "needs-attention" ? "NEEDS ATTENTION" : "IDLE";
  const titleId = `stream-title-${device.id}`;

  return <article
    role={expanded ? "dialog" : undefined}
    aria-modal={expanded ? true : undefined}
    aria-labelledby={expanded ? titleId : undefined}
    data-stream-dialog={expanded ? "" : undefined}
    className={expanded
      ? "fixed left-1/2 top-1/2 z-50 flex max-h-[calc(100dvh-2rem)] w-[min(96vw,75rem)] -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl"
      : "relative overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm transition-shadow hover:shadow-md focus-within:ring-2 focus-within:ring-indigo-500"}
  >
    <div className={`flex items-start justify-between gap-3 border-b border-gray-200 ${expanded ? "p-5" : "p-4"}`}>
      <div className="min-w-0">
        <h2 id={titleId} className={`font-semibold text-gray-900 ${expanded ? "text-xl" : ""}`}>
          {device.vrId || device.id}{expanded && device.experimentName ? ` — ${device.experimentName}` : ""}
        </h2>
        {!expanded && <p className="mt-1 text-sm text-gray-700">{device.experimentName || "No active experiment"}</p>}
        <p className="mt-1 text-xs text-gray-500">{device.currentSceneName || "VR hub (no experiment scene)"}</p>
        {device.currentGroupId && <p className="mt-1 text-sm text-gray-600">Group: <span className="font-medium text-gray-900">{device.currentGroupName || "Unknown group"}</span></p>}
      </div>
      <div className="flex shrink-0 items-center gap-3">
        <span className={`text-right text-xs font-semibold ${textColor}`}>{stateLabel}</span>
        {expanded ? <button ref={closeButtonRef} type="button" aria-label="Close expanded VR view" onClick={onClose}
          className="relative z-20 rounded-full border border-gray-200 p-2 text-gray-700 hover:bg-gray-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600">
          <X size={20} />
        </button> : <Maximize2 size={17} aria-hidden="true" className="text-gray-400" />}
      </div>
    </div>
    <div className={expanded
      ? "relative flex min-h-0 flex-1 items-center justify-center bg-gray-950 text-white aspect-video max-h-[calc(100dvh-12rem)]"
      : "relative aspect-video bg-gray-950 text-white"}>
      {!canView ? (
        <div className="flex h-full w-full flex-col items-center justify-center gap-3 p-5 text-center text-sm">
          {device.state === "needs-attention" ? <AlertTriangle size={25} /> : <MonitorPlay size={25} />}
          <p className="max-w-xl">{device.message}</p>
        </div>
      ) : error ? (
        <div className="flex h-full w-full flex-col items-center justify-center gap-3 p-4 text-center text-sm">
          <p>{error}</p>
          <button type="button" onClick={() => setRetry((n) => n + 1)}
            className="relative z-20 rounded bg-white px-3 py-2 text-gray-900 hover:bg-gray-100">Retry viewer connection</button>
        </div>
      ) : tokenInfo ? (
        <LiveKitRoom
          key={`${device.id}-${experimentId}-${progressId}-${retry}`}
          serverUrl={tokenInfo.serverUrl}
          token={tokenInfo.token}
          connect
          audio={false}
          video={false}
          onConnected={() => setPhase("Connected. Waiting for the headset video track...")}
          onDisconnected={() => { setHasVideo(false); setPhase("LiveKit disconnected"); }}
          onError={(err) => { setHasVideo(false); setError(err?.message || "LiveKit connection failed"); }}
        >
          <StreamVideo deviceId={device.id} onHasVideo={setHasVideo} />
        </LiveKitRoom>
      ) : (
        <div className="flex h-full w-full items-center justify-center p-5 text-center text-sm text-gray-300">{phase}</div>
      )}
    </div>
    {expanded ? <div className="flex items-center justify-between gap-3 border-t border-gray-200 px-5 py-3 text-sm text-gray-600">
      <span>{hasVideo ? "Live spectator view" : "Headset preview"}</span>
      <span>Press Esc or X to close</span>
    </div> : canView && !hasVideo ? <div className="px-4 py-3 text-xs text-gray-500">{phase}</div> : null}
    {!expanded && <button ref={openButtonRef} type="button" onClick={onExpand}
      aria-label={`Enlarge ${device.vrId || device.id} livestream`}
      className="absolute inset-0 z-10 cursor-zoom-in rounded-xl focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-indigo-600" />}
  </article>;
}

export default function InstructorLiveExperiments() {
  const { blockId } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [courseName, setCourseName] = useState("");
  const [devices, setDevices] = useState([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [checkedAt, setCheckedAt] = useState(null);
  const [refreshVersion, setRefreshVersion] = useState(0);
  const [expandedDeviceId, setExpandedDeviceId] = useState(null);

  // The expanded card stays mounted, so changing size does not reconnect its stream.
  useEffect(() => {
    if (!expandedDeviceId) return undefined;
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const onKeyDown = (event) => {
      if (event.key === "Escape") setExpandedDeviceId(null);
      if (event.key !== "Tab") return;
      const dialog = document.querySelector("[data-stream-dialog]");
      const focusable = dialog?.querySelectorAll('button:not([disabled]), [href], [tabindex]:not([tabindex="-1"])');
      if (!focusable?.length) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = originalOverflow;
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [expandedDeviceId]);

  useEffect(() => {
    if (!user?.uid || !blockId) return undefined;
    let disposed = false;
    let pending = false;
    const fetchOverview = async () => {
      if (pending || disposed) return;
      pending = true;
      setRefreshing(true);
      try {
        const call = httpsCallable(getFunctions(), "getInstructorLiveOverview");
        const reply = await call({ blockId });
        if (disposed) return;
        const nextDevices = Array.isArray(reply.data?.devices) ? reply.data.devices : [];
        setDevices(nextDevices);
        setExpandedDeviceId((id) => id && nextDevices.some((device) => device.id === id) ? id : null);
        setCourseName(reply.data?.courseName || blockId);
        setCheckedAt(new Date());
        setError("");
      } catch (err) {
        console.error("[Live VR] Overview request failed:", err);
        if (!disposed) {
          const message = err?.code === "functions/not-found"
            ? "getInstructorLiveOverview is not deployed. Update WEBSITE/functions/index.js and deploy Firebase Functions."
            : err?.message || "Unable to load the live stream overview.";
          setError(message);
        }
      } finally {
        pending = false;
        if (!disposed) { setLoading(false); setRefreshing(false); }
      }
    };
    fetchOverview();
    const timer = setInterval(fetchOverview, POLL_INTERVAL_MS);
    return () => { disposed = true; clearInterval(timer); };
  }, [user?.uid, blockId, refreshVersion]);

  const authError = !user?.uid || !blockId ? "Sign in as an instructor and open a course first." : "";
  const displayedError = authError || error;
  const liveCount = devices.filter((d) => d.state === "ready").length;
  const warningCount = devices.filter((d) => d.state === "needs-attention").length;
  const waitingCount = devices.filter((d) => d.state === "waiting-vr").length;

  return <main className="mx-auto min-h-screen max-w-7xl px-5 pb-12 pt-24 text-gray-900">
    <button type="button" onClick={() => navigate(`/instructor/course/${blockId}`)}
      className="mb-5 flex items-center gap-2 text-sm text-gray-600 hover:underline">
      <ArrowLeft size={17} /> Back to Course
    </button>
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="flex items-center gap-3 text-2xl font-semibold"><Radio size={25} /> Live Experiments</h1>
        <p className="mt-2 text-gray-600">{courseName || "Course"} — all headsets linked to this course's experiments.</p>
      </div>
      <button type="button" onClick={() => setRefreshVersion((n) => n + 1)} disabled={refreshing}
        className="flex items-center gap-2 rounded-lg border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 disabled:opacity-50">
        <RefreshCw size={16} /> {refreshing ? "Checking..." : "Refresh now"}
      </button>
    </div>
    <p className="mb-5 mt-2 text-sm text-gray-500">Updates approximately every 3 seconds. Streaming begins when the student starts an attempt on the web, even while the headset is in the VR hub.</p>
    {checkedAt && <p className="mb-3 text-xs text-gray-500">Last checked: {checkedAt.toLocaleTimeString()}</p>}
    {displayedError && <div role="alert" className="mb-5 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
      <strong>Unable to refresh streams:</strong> {displayedError}
    </div>}
    {authError ? null : loading ? <p>Checking available and active VR headsets...</p> : (
      <>
        <p className="mb-5 text-sm text-gray-700">
          {devices.length} headset(s) found · {liveCount} active attempt(s) · {waitingCount} waiting for VR · {warningCount} needing attention
        </p>
        {devices.length === 0 ? (
          <div className="rounded-xl border border-gray-200 p-7 text-gray-600">
            No VR headset is currently associated with this course. Assign at least one registered headset to an experiment, then have a student start a session. If one is already assigned, verify its <code>vrId</code> matches <code>vrDevices</code>.
          </div>
        ) : (
          <>
            {expandedDeviceId && <button type="button" aria-label="Close expanded VR overlay"
              onClick={() => setExpandedDeviceId(null)}
              className="fixed inset-0 z-40 cursor-default bg-gray-950/75 backdrop-blur-[2px]" />}
            <div className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
              {devices.map((device) => <LiveStreamCard
                key={`${device.id}-${device.currentProgressId || "none"}-${device.currentExperimentId || "none"}`}
                blockId={blockId} device={device}
                expanded={expandedDeviceId === device.id}
                onExpand={() => setExpandedDeviceId(device.id)}
                onClose={() => setExpandedDeviceId(null)} />)}
            </div>
          </>
        )}
      </>
    )}
  </main>;
}
