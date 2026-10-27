
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  collection,
  doc,
  getDoc,
  onSnapshot,
  query,
  where,
} from "firebase/firestore";

import { getFunctions, httpsCallable } from "firebase/functions";

import { LiveKitRoom, VideoTrack, useTracks } from "@livekit/components-react";
import { Track } from "livekit-client";
import "@livekit/components-styles";

import { db } from "../../config/firebase-config";
import { useAuth } from "../../context/AuthContext";

function StreamVideo() {
  const tracks = useTracks([Track.Source.Camera], {
    onlySubscribed: true,
  });

  if (!tracks.length) {
    return (
      <div className="flex h-full items-center justify-center text-gray-400">
        Waiting for the headset video...
      </div>
    );
  }

  return (
    <div className="live-video h-full w-full">
      {tracks.map((trackRef) => (
        <VideoTrack
          key={trackRef.publication.trackSid}
          trackRef={trackRef}
        />
      ))}
    </div>
  );
}

function LiveStreamCard({ blockId, device }) {
  const [studentName, setStudentName] = useState("Loading student...");
  const [tokenInfo, setTokenInfo] = useState(null);
  const [error, setError] = useState("");
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    let cancelled = false;

    async function loadCard() {
      setError("");
      setTokenInfo(null);

      try {
        const userId = device.currentUserId;

        if (userId) {
          const userSnap = await getDoc(doc(db, "user", userId));

          if (userSnap.exists()) {
            const userData = userSnap.data();
            const name = [
              userData.firstName || "",
              userData.lastName || "",
            ]
              .join(" ")
              .trim();

            if (!cancelled) {
              setStudentName(name || userData.email || userId);
            }
          } else if (!cancelled) {
            setStudentName(userId);
          }
        } else if (!cancelled) {
          setStudentName("No student assigned");
        }

        const functions = getFunctions();
        const createToken = httpsCallable(
          functions,
          "createLiveStreamToken"
        );

        const result = await createToken({
          blockId,
          vrDeviceId: device.id,
          role: "viewer",
        });

        if (!cancelled) {
          setTokenInfo(result.data);
        }
      } catch (err) {
        console.error("Unable to load VR stream:", err);

        if (!cancelled) {
          setError(
            err.message ||
              "Unable to connect to this VR stream. Try refreshing."
          );
        }
      }
    }

    loadCard();

    return () => {
      cancelled = true;
    };
  }, [blockId, device, retry]);

  const deviceLabel = device.vrId || device.id;
  const sceneName =
    device.currentSceneName ||
    device.currentExperimentId ||
    "Experiment not reported";

  return (
    <article className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
      <div className="flex items-start justify-between gap-3 border-b border-gray-200 p-4">
        <div>
          <h2 className="font-semibold text-gray-900">{deviceLabel}</h2>
          <p className="text-sm text-gray-600">{studentName}</p>
          <p className="text-sm text-gray-500">{sceneName}</p>
        </div>

        <span className="rounded-full bg-red-50 px-3 py-1 text-xs font-semibold text-red-700">
          LIVE
        </span>
      </div>

      <div className="aspect-video bg-gray-950">
        {error ? (
          <div className="flex h-full flex-col items-center justify-center gap-3 p-4 text-center text-sm text-white">
            <p>{error}</p>
            <button
              onClick={() => setRetry((value) => value + 1)}
              className="rounded-lg bg-white px-4 py-2 font-medium text-gray-900"
            >
              Retry connection
            </button>
          </div>
        ) : tokenInfo ? (
          <LiveKitRoom
            serverUrl={tokenInfo.serverUrl}
            token={tokenInfo.token}
            connect={true}
            audio={false}
            video={false}
          >
            <StreamVideo />
          </LiveKitRoom>
        ) : (
          <div className="flex h-full items-center justify-center text-sm text-gray-300">
            Connecting to VR stream...
          </div>
        )}
      </div>

      <div className="space-y-1 p-4 text-sm text-gray-500">
        <p>Group: {device.currentGroupId || "Not assigned"}</p>
        <p>Session: {device.currentProgressId || "Not reported"}</p>
      </div>
    </article>
  );
}

export default function InstructorLiveExperiments() {
  const navigate = useNavigate();
  const { blockId } = useParams();
  const { user } = useAuth();

  const [devices, setDevices] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    document.title = "Instructor - Live VR Sessions";
  }, []);

  useEffect(() => {
    if (!user || !blockId) {
      setLoading(false);
      return;
    }

    let unsubscribe = null;
    let cancelled = false;

    async function loadDashboard() {
      setLoading(true);
      setError("");

      try {
        const blockSnap = await getDoc(doc(db, "block", blockId));

        if (!blockSnap.exists()) {
          throw new Error("Class not found.");
        }

        if (blockSnap.data().instructorId !== user.uid) {
          throw new Error("You do not have access to this class.");
        }

        if (cancelled) return;

        const devicesQuery = query(
          collection(db, "vrDevices"),
          where("currentBlockId", "==", blockId),
          where("status", "==", "In Use")
        );

        unsubscribe = onSnapshot(
          devicesQuery,
          (snapshot) => {
            const activeDevices = snapshot.docs
              .map((deviceDoc) => ({
                id: deviceDoc.id,
                ...deviceDoc.data(),
              }))
              .filter(
                (device) =>
                  device.currentUserId &&
                  device.currentBlockId === blockId
              )
              .sort((a, b) =>
                (a.vrId || a.id).localeCompare(b.vrId || b.id)
              );

            setDevices(activeDevices);
            setLoading(false);
          },
          (err) => {
            console.error("Live device listener failed:", err);
            setError("Unable to listen for active VR devices.");
            setLoading(false);
          }
        );
      } catch (err) {
        if (!cancelled) {
          setError(err.message || "Unable to load the live dashboard.");
          setLoading(false);
        }
      }
    }

    loadDashboard();

    return () => {
      cancelled = true;
      if (unsubscribe) unsubscribe();
    };
  }, [blockId, user]);

  return (
    <div className="font-google min-h-screen bg-gray-50 text-gray-900">
      <main className="mx-auto max-w-7xl px-4 pb-12 pt-24 sm:px-6">
        <button
          onClick={() => navigate(`/instructor/course/${blockId}`)}
          className="mb-6 text-sm font-medium text-gray-600 hover:text-black"
        >
          ← Back to Experiments
        </button>

        <div className="mb-8 flex flex-wrap items-end justify-between gap-3">
          <div>
            <h1 className="text-2xl font-semibold">Live VR Sessions</h1>
            <p className="mt-1 text-sm text-gray-500">
              Monitor active headset views for this class.
            </p>
          </div>

          <span className="rounded-lg border bg-white px-3 py-2 text-sm">
            {devices.length} active headset{devices.length === 1 ? "" : "s"}
          </span>
        </div>

        {loading && (
          <p className="py-10 text-center text-gray-500">
            Loading active VR sessions...
          </p>
        )}

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-700">
            {error}
          </div>
        )}

        {!loading && !error && devices.length === 0 && (
          <div className="rounded-xl border border-dashed bg-white px-6 py-16 text-center">
            <h2 className="font-semibold">No active VR sessions</h2>
            <p className="mt-2 text-sm text-gray-500">
              A headset will appear here after it is assigned to this class
              and reports an active session.
            </p>
          </div>
        )}

        {!error && devices.length > 0 && (
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
            {devices.map((device) => (
              <LiveStreamCard
                key={device.id}
                blockId={blockId}
                device={device}
              />
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
