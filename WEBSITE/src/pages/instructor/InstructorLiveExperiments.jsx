import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { collection, doc, getDoc, onSnapshot, query, where } from "firebase/firestore";
import { getFunctions, httpsCallable } from "firebase/functions";
import { LiveKitRoom, VideoTrack, useTracks } from "@livekit/components-react";
import { Track } from "livekit-client";
import "@livekit/components-styles";
import { db } from "../../config/firebase-config";
import { useAuth } from "../../context/AuthContext";

function StreamVideo({ deviceId, onHasVideo }) {
  const tracks = useTracks([Track.Source.Camera], { onlySubscribed: true });
  const videoTrack = tracks.find((ref) => ref.participant?.identity === `vr-${deviceId}`);

  useEffect(() => { onHasVideo(Boolean(videoTrack)); }, [onHasVideo, videoTrack]);

  if (!videoTrack) {
    return <div className="flex h-full items-center justify-center p-5 text-center text-gray-300">
      Connected to LiveKit; waiting for a published VR camera track...
    </div>;
  }
  return <div className="h-full w-full [&_video]:h-full [&_video]:w-full [&_video]:object-contain">
    <VideoTrack trackRef={videoTrack} />
  </div>;
}

function LiveStreamCard({ blockId, experimentId, device }) {
  const [tokenInfo, setTokenInfo] = useState(null);
  const [error, setError] = useState("");
  const [phase, setPhase] = useState("Requesting viewer token...");
  const [retry, setRetry] = useState(0);
  const [hasVideo, setHasVideo] = useState(false);

  useEffect(() => {
    let canceled = false;
    async function connectViewer() {
      setTokenInfo(null);
      setError("");
      setPhase("Requesting viewer token...");
      setHasVideo(false);
      try {
        const createToken = httpsCallable(getFunctions(), "createLiveStreamToken");
        const reply = await createToken({ blockId, experimentId, vrDeviceId: device.id, role: "viewer" });
        if (!canceled) {
          setTokenInfo(reply.data);
          setPhase("Joining LiveKit room...");
        }
      } catch (err) {
        console.error("[Live VR] Viewer token failed:", err);
        if (!canceled) setError(err?.message || "Viewer token request failed.");
      }
    }
    connectViewer();
    return () => { canceled = true; };
  }, [blockId, experimentId, device.id, device.currentProgressId, retry]);

  return <article className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
    <div className="flex items-start justify-between gap-2 border-b p-4">
      <div>
        <h2 className="font-semibold">{device.vrId || device.id}</h2>
        <p className="text-xs text-gray-500">{device.currentSceneName || "Experiment scene"}</p>
        <p className="text-xs text-gray-500">Group: {device.currentGroupId || "None"}</p>
      </div>
      <span className={hasVideo ? "text-sm font-medium text-green-700" : "text-sm text-gray-500"}>
        {hasVideo ? "RECEIVING VIDEO" : phase}
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
          key={`${device.id}-${retry}`}
          serverUrl={tokenInfo.serverUrl}
          token={tokenInfo.token}
          connect={true}
          audio={false}
          video={false}
          onConnected={() => setPhase("Connected; awaiting VR video")}
          onDisconnected={() => { setHasVideo(false); setPhase("LiveKit disconnected"); }}
          onError={(err) => { setHasVideo(false); setError(err?.message || "LiveKit connection error"); }}
        >
          <StreamVideo deviceId={device.id} onHasVideo={setHasVideo} />
        </LiveKitRoom>
      ) : <div className="flex h-full items-center justify-center p-3 text-sm text-gray-300">{phase}</div>}
    </div>
    <div className="px-4 py-3 text-xs text-gray-500">
      Progress: {device.currentProgressId || "Not reported"} · Room: {device.streamRoomName || "Not assigned"}
    </div>
  </article>;
}

export default function InstructorLiveExperiments() {
  const { blockId, experimentId } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [experimentName, setExperimentName] = useState("");
  const [devices, setDevices] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!user?.uid || !blockId || !experimentId) {
      setError("Sign in as the instructor and select an experiment.");
      setLoading(false);
      return;
    }
    let stopped = false;
    let unsubscribe = () => {};

    async function start() {
      try {
        setLoading(true);
        setError("");
        const [blockSnap, experimentSnap] = await Promise.all([
          getDoc(doc(db, "block", blockId)),
          getDoc(doc(db, "experiment", experimentId)),
        ]);
        if (!blockSnap.exists() || blockSnap.data().instructorId !== user.uid) {
          throw new Error("This class is not assigned to your instructor account.");
        }
        if (!experimentSnap.exists() || experimentSnap.data().blockId !== blockId) {
          throw new Error("The selected experiment does not belong to this class.");
        }
        if (stopped) return;
        setExperimentName(experimentSnap.data().experimentName || experimentSnap.data().name || experimentId);

        // Include owner equality in the query so Firestore rules can enforce ownership.
        // Firestore may prompt you to create a composite index for this query.
        const devicesQuery = query(collection(db, "vrDevices"),
          where("assignedInstructorId", "==", user.uid),
          where("currentBlockId", "==", blockId),
          where("currentExperimentId", "==", experimentId),
          where("status", "==", "In Use"));

        unsubscribe = onSnapshot(devicesQuery, (snapshot) => {
          if (stopped) return;
          const active = snapshot.docs.map((d) => ({ id: d.id, ...d.data() }))
            .filter((d) => d.currentProgressId && d.streamRoomName)
            .sort((a, b) => (a.vrId || a.id).localeCompare(b.vrId || b.id));
          setDevices(active);
          setLoading(false);
        }, (err) => {
          console.error("[Live VR] Device listener:", err);
          if (!stopped) { setError(err?.message || "Unable to read assigned VR sessions."); setLoading(false); }
        });
      } catch (err) {
        if (!stopped) { setError(err?.message || "Cannot open this experiment."); setLoading(false); }
      }
    }
    start();
    return () => { stopped = true; unsubscribe(); };
  }, [user?.uid, blockId, experimentId]);

  return <main className="mx-auto min-h-screen max-w-7xl px-5 pb-12 pt-24 text-gray-900">
    <button type="button" onClick={() => navigate(`/instructor/course/${blockId}`)}
      className="mb-5 text-sm text-gray-600 hover:underline">← Back to Experiments</button>
    <h1 className="text-2xl font-semibold">Live VR — {experimentName || "Selected Experiment"}</h1>
    <p className="mb-6 mt-2 text-gray-500">Only active VR devices assigned to your account and running this experiment are shown.</p>
    {loading && <p>Loading assigned devices...</p>}
    {error && <p role="alert" className="my-4 text-red-700">{error}</p>}
    {!loading && !error && !devices.length && <p>No assigned VR devices are currently running this experiment.</p>}
    {!error && devices.length > 0 && <div className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
      {devices.map((d) => <LiveStreamCard key={`${d.id}-${d.currentProgressId}`}
        blockId={blockId} experimentId={experimentId} device={d} />)}
    </div>}
  </main>;
}
