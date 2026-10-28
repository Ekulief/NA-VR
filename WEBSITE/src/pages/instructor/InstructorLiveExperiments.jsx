import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { collection, doc, getDoc, onSnapshot, query, where } from "firebase/firestore";
import { getFunctions, httpsCallable } from "firebase/functions";
import { LiveKitRoom, VideoTrack, useTracks } from "@livekit/components-react";
import { Track } from "livekit-client";
import { ArrowLeft, Radio } from "lucide-react";
import "@livekit/components-styles";
import { db } from "../../config/firebase-config";
import { useAuth } from "../../context/AuthContext";

function StreamVideo({ deviceId, onHasVideo }) {
  const tracks = useTracks([Track.Source.Camera], { onlySubscribed: true });
  const videoTrack = tracks.find((track) => track.participant?.identity === `vr-${deviceId}`);

  useEffect(() => {
    onHasVideo(Boolean(videoTrack));
  }, [onHasVideo, videoTrack]);

  if (!videoTrack) {
    return <div className="flex h-full items-center justify-center p-5 text-center text-gray-300">
      Connected. Waiting for the VR camera to publish video...
    </div>;
  }
  return <div className="h-full w-full [&_video]:h-full [&_video]:w-full [&_video]:object-contain">
    <VideoTrack trackRef={videoTrack} />
  </div>;
}

function LiveStreamCard({ blockId, device, experimentName }) {
  const [tokenInfo, setTokenInfo] = useState(null);
  const [error, setError] = useState("");
  const [phase, setPhase] = useState("Requesting viewer token...");
  const [retry, setRetry] = useState(0);
  const [hasVideo, setHasVideo] = useState(false);
  const experimentId = device.currentExperimentId;

  useEffect(() => {
    let canceled = false;
    async function connectViewer() {
      setTokenInfo(null);
      setError("");
      setPhase("Requesting viewer token...");
      setHasVideo(false);
      try {
        const createToken = httpsCallable(getFunctions(), "createLiveStreamToken");
        const reply = await createToken({
          blockId,
          experimentId,
          vrDeviceId: device.id,
          role: "viewer",
        });
        if (!canceled) {
          setTokenInfo(reply.data);
          setPhase("Connecting to LiveKit...");
        }
      } catch (err) {
        console.error("[Live VR] Viewer token failed:", err);
        if (!canceled) setError(err?.message || "Unable to join the VR stream.");
      }
    }
    connectViewer();
    return () => { canceled = true; };
  }, [blockId, experimentId, device.id, device.currentProgressId, retry]);

  return <article className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
    <div className="flex items-start justify-between gap-3 border-b border-gray-200 p-4">
      <div className="min-w-0">
        <h2 className="font-semibold text-gray-900">{device.vrId || device.id}</h2>
        <p className="mt-1 truncate text-sm text-gray-700">{experimentName}</p>
        <p className="text-xs text-gray-500">{device.currentSceneName || "Starting experiment..."}</p>
        <p className="text-xs text-gray-500">Group: {device.currentGroupId || "Unassigned"}</p>
      </div>
      <span className={`shrink-0 text-right text-xs font-medium ${hasVideo ? "text-green-700" : "text-gray-500"}`}>
        {hasVideo ? "LIVE" : "Connecting"}
      </span>
    </div>
    <div className="aspect-video bg-gray-950 text-white">
      {error ? (
        <div className="flex h-full flex-col items-center justify-center gap-3 p-4 text-center text-sm">
          <p>{error}</p>
          <button type="button" onClick={() => setRetry((n) => n + 1)}
            className="rounded bg-white px-3 py-2 text-gray-900">Retry connection</button>
        </div>
      ) : tokenInfo ? (
        <LiveKitRoom
          key={`${device.id}-${experimentId}-${device.currentProgressId}-${retry}`}
          serverUrl={tokenInfo.serverUrl}
          token={tokenInfo.token}
          connect
          audio={false}
          video={false}
          onConnected={() => setPhase("Connected; waiting for VR video...")}
          onDisconnected={() => { setHasVideo(false); setPhase("LiveKit disconnected"); }}
          onError={(err) => { setHasVideo(false); setError(err?.message || "LiveKit connection failed"); }}
        >
          <StreamVideo deviceId={device.id} onHasVideo={setHasVideo} />
        </LiveKitRoom>
      ) : <div className="flex h-full items-center justify-center p-3 text-sm text-gray-300">
        {phase}
      </div>}
    </div>
    <div className="px-4 py-3 text-xs text-gray-500">
      <span>Progress: {device.currentProgressId || "None"}</span>
      <span className="ml-2">{hasVideo ? "Receiving camera video" : phase}</span>
    </div>
  </article>;
}

export default function InstructorLiveExperiments() {
  const { blockId } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [courseName, setCourseName] = useState("");
  const [experimentNames, setExperimentNames] = useState({});
  const [devices, setDevices] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!user?.uid || !blockId) {
      setError("Sign in as an instructor and select a class.");
      setLoading(false);
      return;
    }

    let stopped = false;
    let unsubscribeDevices = () => {};
    let unsubscribeExperiments = () => {};

    async function start() {
      try {
        setLoading(true);
        setError("");
        const blockSnap = await getDoc(doc(db, "block", blockId));
        if (!blockSnap.exists() || blockSnap.data().instructorId !== user.uid) {
          throw new Error("This class does not belong to your instructor account.");
        }
        if (stopped) return;
        setCourseName(blockSnap.data().className || blockSnap.data().classCode || blockId);

        unsubscribeExperiments = onSnapshot(
          query(collection(db, "experiment"), where("blockId", "==", blockId)),
          (snap) => {
            if (stopped) return;
            const names = {};
            snap.forEach((experiment) => {
              const data = experiment.data();
              names[experiment.id] = data.experimentName || data.name || experiment.id;
            });
            setExperimentNames(names);
          },
          (err) => console.error("[Live VR] Cannot load experiment labels:", err),
        );

        // Query by instructor and block; Firebase rules should additionally enforce ownership.
        // Filter active status on the client to avoid a third composite-index field.
        const deviceQuery = query(
          collection(db, "vrDevices"),
          where("assignedInstructorId", "==", user.uid),
          where("currentBlockId", "==", blockId),
        );
        unsubscribeDevices = onSnapshot(deviceQuery, (snap) => {
          if (stopped) return;
          const active = snap.docs.map((d) => ({ id: d.id, ...d.data() }))
            .filter((d) => d.status === "In Use" &&
              d.currentExperimentId && d.currentProgressId &&
              d.streamRoomName === `slubt-${blockId}-${d.id}`)
            .sort((a, b) => (a.vrId || a.id).localeCompare(b.vrId || b.id));
          setDevices(active);
          setLoading(false);
        }, (err) => {
          console.error("[Live VR] Device listener failed:", err);
          if (!stopped) { setError(err?.message || "Unable to read live VR devices."); setLoading(false); }
        });
      } catch (err) {
        if (!stopped) { setError(err?.message || "Unable to open live experiments."); setLoading(false); }
      }
    }

    start();
    return () => { stopped = true; unsubscribeDevices(); unsubscribeExperiments(); };
  }, [blockId, user?.uid]);

  return <main className="mx-auto min-h-screen max-w-7xl px-5 pb-12 pt-24 text-gray-900">
    <button type="button" onClick={() => navigate(`/instructor/course/${blockId}`)}
      className="mb-5 flex items-center gap-2 text-sm text-gray-600 hover:underline">
      <ArrowLeft size={17} /> Back to Course
    </button>
    <h1 className="flex items-center gap-3 text-2xl font-semibold"><Radio size={25} /> Live Experiments</h1>
    <p className="mt-2 text-gray-600">{courseName || "Course"} — All currently active VR experiment streams for this class.</p>
    <p className="mb-6 mt-1 text-sm text-gray-500">
      Streams appear when a student connects a VR device and the headset starts its experiment camera.
    </p>
    {loading && <p>Checking active VR sessions...</p>}
    {error && <p role="alert" className="my-4 text-red-700">{error}</p>}
    {!loading && !error && devices.length === 0 &&
      <div className="rounded-xl border border-gray-200 p-8 text-center text-gray-500">
        No active VR experiment streams in this class yet.
      </div>}
    {!error && devices.length > 0 &&
      <div className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
        {devices.map((device) =>
          <LiveStreamCard
            key={`${device.id}-${device.currentProgressId}-${device.currentExperimentId}`}
            blockId={blockId}
            device={device}
            experimentName={experimentNames[device.currentExperimentId] || device.currentSceneName || "Experiment"}
          />)}
      </div>}
  </main>;
}
