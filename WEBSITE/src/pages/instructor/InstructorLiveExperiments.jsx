import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getFunctions, httpsCallable } from "firebase/functions";
import { LiveKitRoom, VideoTrack, useTracks } from "@livekit/components-react";
import { Track } from "livekit-client";
import { ArrowLeft, RefreshCw, Radio, AlertTriangle, MonitorPlay } from "lucide-react";
import "@livekit/components-styles";
import { useAuth } from "../../context/AuthContext";

// The server verifies block ownership before returning ANY VR device metadata.
// This page doesn't need Firestore composite indexes or direct vrDevices read rules.
const POLL_INTERVAL_MS = 10000;

function StreamVideo({ deviceId, onHasVideo }) {
  const tracks = useTracks([Track.Source.Camera], { onlySubscribed: true });
  const track = tracks.find((t) => t.participant?.identity === `vr-${deviceId}`);
  useEffect(() => { onHasVideo(Boolean(track)); }, [track, onHasVideo]);
  if (!track) {
    return <div className="flex h-full items-center justify-center p-5 text-center text-sm text-gray-300">
      Viewer connected. Waiting for the Unity headset camera track. Check LiveKitVrPublisher on the headset.
    </div>;
  }
  return <div className="h-full w-full [&_video]:h-full [&_video]:w-full [&_video]:object-contain">
    <VideoTrack trackRef={track} />
  </div>;
}

function LiveStreamCard({ blockId, device }) {
  const [tokenInfo, setTokenInfo] = useState(null);
  const [error, setError] = useState("");
  const [phase, setPhase] = useState("Requesting viewer token...");
  const [retry, setRetry] = useState(0);
  const [hasVideo, setHasVideo] = useState(false);
  const canView = device.state === "ready";
  const experimentId = device.currentExperimentId;
  const progressId = device.currentProgressId;

  useEffect(() => {
    if (!canView) {
      setTokenInfo(null);
      setHasVideo(false);
      return;
    }
    let canceled = false;
    setTokenInfo(null);
    setError("");
    setPhase("Requesting viewer token...");
    setHasVideo(false);
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
  const stateLabel = hasVideo ? "LIVE VIDEO" : canView ? "SCENE ACTIVE" : device.state === "waiting-vr" ? "WAITING FOR VR" : device.state === "needs-attention" ? "NEEDS ATTENTION" : "IDLE";

  return <article className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
    <div className="flex items-start justify-between gap-3 border-b border-gray-200 p-4">
      <div className="min-w-0">
        <h2 className="font-semibold text-gray-900">{device.vrId || device.id}</h2>
        <p className="mt-1 text-sm text-gray-700">{device.experimentName || "No active experiment"}</p>
        <p className="text-xs text-gray-500">{device.currentSceneName || "No experiment scene reported"}</p>
        {device.currentGroupId && <p className="text-xs text-gray-500">Group: {device.currentGroupId}</p>}
      </div>
      <span className={`shrink-0 text-right text-xs font-semibold ${textColor}`}>{stateLabel}</span>
    </div>
    <div className="aspect-video bg-gray-950 text-white">
      {!canView ? (
        <div className="flex h-full flex-col items-center justify-center gap-3 p-5 text-center text-sm">
          {device.state === "needs-attention" ? <AlertTriangle size={25} /> : <MonitorPlay size={25} />}
          <p>{device.message}</p>
          {!device.publisherRegistered && <p className="text-xs text-amber-300">Start the Unity app to register publisherAuthUid.</p>}
        </div>
      ) : error ? (
        <div className="flex h-full flex-col items-center justify-center gap-3 p-4 text-center text-sm">
          <p>{error}</p>
          <button type="button" onClick={() => setRetry((n) => n + 1)}
            className="rounded bg-white px-3 py-2 text-gray-900">Retry viewer connection</button>
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
        <div className="flex h-full items-center justify-center p-5 text-center text-sm text-gray-300">{phase}</div>
      )}
    </div>
    <div className="space-y-1 px-4 py-3 text-xs text-gray-500">
      <p>Firestore status: {device.rawStatus || "(missing)"} · Unity registered: {device.publisherRegistered ? "Yes" : "No"}</p>
      {device.currentProgressId && <p>Progress: {device.currentProgressId}</p>}
      {canView && !hasVideo && <p>{phase}</p>}
    </div>
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

  useEffect(() => {
    if (!user?.uid || !blockId) {
      setError("Sign in as an instructor and open a course first.");
      setLoading(false);
      return;
    }
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
        setDevices(Array.isArray(reply.data?.devices) ? reply.data.devices : []);
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
    <p className="mb-5 mt-2 text-sm text-gray-500">Updates approximately every 10 seconds. A headset needs an active student session and must enter an experiment scene before video can appear.</p>
    {checkedAt && <p className="mb-3 text-xs text-gray-500">Last checked: {checkedAt.toLocaleTimeString()}</p>}
    {error && <div role="alert" className="mb-5 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
      <strong>Unable to refresh streams:</strong> {error}
    </div>}
    {loading ? <p>Checking available and active VR headsets...</p> : (
      <>
        <p className="mb-5 text-sm text-gray-700">
          {devices.length} headset(s) found · {liveCount} scene(s) ready · {waitingCount} waiting for VR · {warningCount} needing attention
        </p>
        {devices.length === 0 ? (
          <div className="rounded-xl border border-gray-200 p-7 text-gray-600">
            No VR headset is currently associated with this course. Assign at least one registered headset to an experiment, then have a student start a session. If one is already assigned, verify its <code>vrId</code> matches <code>vrDevices</code>.
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
            {devices.map((device) => <LiveStreamCard
              key={`${device.id}-${device.currentProgressId || "none"}-${device.currentExperimentId || "none"}`}
              blockId={blockId} device={device} />)}
          </div>
        )}
      </>
    )}
  </main>;
}
