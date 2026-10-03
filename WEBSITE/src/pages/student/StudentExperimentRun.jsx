import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { doc, getDoc } from "firebase/firestore";

import { db } from "../../config/firebase-config";

import { ArrowLeft, Box, Square, Download } from "lucide-react";

const EMPTY_METRICS = {
  headRotation: { x: null, y: null, z: null },
  position: { x: null, y: null, z: null },
  heartRate: null,
};

export default function StudentExperimentRun() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();

  const [experiment, setExperiment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [vrIdentifier, setVrIdentifier] = useState("");
  const [connecting, setConnecting] = useState(false);
  const [vrConnected, setVrConnected] = useState(false);

  // "idle" | "running" | "completed"
  const [sessionState, setSessionState] = useState("idle");

  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const [metrics, setMetrics] = useState(EMPTY_METRICS);

  const timerRef = useRef(null);
  const connectTimeoutRef = useRef(null);

  useEffect(() => {
    const getExperiment = async () => {
      try {
        setLoading(true);
        setError("");

        const experimentRef = doc(db, "experiment", experimentId);

        const experimentSnap = await getDoc(experimentRef);

        if (!experimentSnap.exists()) {
          setError("Experiment not found.");
          return;
        }

        setExperiment({
          id: experimentSnap.id,
          ...experimentSnap.data(),
        });
      } catch (error) {
        console.error("Error getting experiment:", error);

        setError("Unable to load the experiment.");
      } finally {
        setLoading(false);
      }
    };

    if (experimentId) {
      getExperiment();
    }
  }, [experimentId]);

  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearInterval(timerRef.current);
      }

      if (connectTimeoutRef.current) {
        clearTimeout(connectTimeoutRef.current);
      }
    };
  }, []);

  const formatTime = (totalSeconds) => {
    const minutes = Math.floor(totalSeconds / 60)
      .toString()
      .padStart(2, "0");

    const seconds = (totalSeconds % 60).toString().padStart(2, "0");

    return `${minutes}:${seconds}`;
  };

  const formatMetric = (value, suffix = "") =>
    value === null || value === undefined ? "--" : `${value}${suffix}`;

  // Step 1: connect the VR device.
  // TODO (backend): replace this timeout with the real connect call
  // (e.g. POST /sessions/connect or a WebSocket handshake with vrIdentifier),
  // and only call setVrConnected(true) once the backend confirms the link.
  const handleConnectDevice = () => {
    if (vrConnected || connecting) {
      return;
    }

    if (!vrIdentifier.trim()) {
      alert("Please enter a VR identifier.");
      return;
    }

    setConnecting(true);

    connectTimeoutRef.current = setTimeout(() => {
      setConnecting(false);
      setVrConnected(true);
    }, 900);
  };

  // Step 2: start the session.
  // TODO (backend): tell the backend to start the session here, and
  // subscribe to its live feed (WebSocket/polling) to push real values
  // into setMetrics as they arrive, instead of leaving them blank.
  const handleStartSession = () => {
    if (!vrConnected || sessionState === "running") {
      return;
    }

    setSessionState("running");
    setElapsedSeconds(0);
    setMetrics(EMPTY_METRICS);

    timerRef.current = setInterval(() => {
      setElapsedSeconds((previous) => previous + 1);
    }, 1000);
  };

  // Step 3: stop the session.
  // TODO (backend): tell the backend to end the session and unsubscribe
  // from the live feed here.
  const handleStopSession = () => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }

    setSessionState("completed");
  };

  // TODO (backend): once results are stored server-side, this should
  // fetch/export the real session record instead of the local snapshot.
  const handleDownloadResults = () => {
    const payload = {
      experimentName: experiment?.experimentName || "Untitled Experiment",
      vrIdentifier,
      durationSeconds: elapsedSeconds,
      duration: formatTime(elapsedSeconds),
      metrics,
      completedAt: new Date().toISOString(),
    };

    const blob = new Blob([JSON.stringify(payload, null, 2)], {
      type: "application/json",
    });

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.download = `${payload.experimentName.replace(/\s+/g, "_")}_results.json`;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    URL.revokeObjectURL(url);
  };

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

  const isRunning = sessionState === "running";
  const isCompleted = sessionState === "completed";

  const statusLabel = isRunning ? "Running" : isCompleted ? "Completed" : "Ready";

  const statusStyle = isCompleted
    ? "bg-indigo-100 text-indigo-700"
    : "bg-green-100 text-green-700";

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20 px-5 pb-10 max-w-6xl mx-auto">
        <button
          onClick={() =>
            navigate(`/student/course/${blockId}/experiment/${experimentId}`)
          }
          className="
            flex
            items-center
            gap-2
            text-gray-600
            hover:text-black
            transition
            mb-4
          "
        >
          <ArrowLeft size={20} />

          <span className="text-lg">Back</span>
        </button>

        <div className="flex items-start justify-between gap-4">
          <h1 className="text-2xl font-medium">
            {experiment.experimentName || "Untitled Experiment"}
          </h1>

          <span
            className={`
              px-3
              py-1
              rounded-full
              text-sm
              whitespace-nowrap
              ${statusStyle}
            `}
          >
            {statusLabel}
          </span>
        </div>

        <p className="text-gray-500 mb-6">
          {experiment.description ||
            experiment.participantInstructions ||
            experiment.instructions ||
            "Run this experiment in VR."}
        </p>

        <div className="grid lg:grid-cols-3 gap-6 mb-6 items-stretch">
          <div className="flex flex-col gap-6">
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Control</h2>

              <label className="block text-sm text-gray-600 mb-1">
                VR Identifier
              </label>

              <input
                type="text"
                value={vrIdentifier}
                onChange={(e) => setVrIdentifier(e.target.value)}
                disabled={vrConnected || connecting}
                placeholder="Enter VR identifier"
                className="
                  w-full
                  bg-gray-100
                  border
                  border-gray-300
                  rounded-lg
                  px-3
                  py-2
                  mb-4
                  outline-none
                  focus:ring-2
                  focus:ring-indigo-500
                  disabled:text-gray-500
                "
              />

              {!vrConnected ? (
                <button
                  onClick={handleConnectDevice}
                  disabled={connecting}
                  className="
                    w-full
                    rounded-lg
                    py-3
                    text-center
                    border
                    border-gray-300
                    hover:bg-gray-50
                    transition
                    disabled:opacity-60
                    disabled:cursor-wait
                  "
                >
                  {connecting ? "Connecting..." : "Connect VR Device"}
                </button>
              ) : (
                <button
                  onClick={handleStartSession}
                  disabled={isRunning}
                  title={
                    isRunning
                      ? "Session is running"
                      : "Start a new session with this device"
                  }
                  className={`
                    w-full
                    rounded-lg
                    py-3
                    text-center
                    border
                    border-green-600
                    text-green-700
                    transition
                    ${isRunning ? "bg-green-50 cursor-default" : "bg-white hover:bg-green-50"}
                  `}
                >
                  VR Device Connected
                </button>
              )}

              {isRunning && (
                <button
                  onClick={handleStopSession}
                  className="
                    w-full
                    mt-3
                    rounded-lg
                    py-3
                    flex
                    items-center
                    justify-center
                    gap-2
                    bg-red-700
                    hover:bg-red-600
                    text-white
                    transition
                  "
                >
                  <Square size={14} fill="currentColor" />

                  <span>Stop Session</span>
                </button>
              )}
            </section>

            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Timer</h2>

              <p className="text-4xl font-medium text-center tabular-nums">
                {formatTime(elapsedSeconds)}
              </p>
            </section>
          </div>

          <div className="lg:col-span-2">
            <section className="border border-gray-300 rounded-xl p-5 h-full flex flex-col">
              <h2 className="text-lg font-medium mb-4">VR Preview</h2>

              {/*
                TODO (backend): once a live feed exists, render it here
                (e.g. a <video>/<canvas> fed by the VR stream) while
                isRunning is true. Left as the static placeholder for
                every state until that's wired up.
              */}
              <div
                className="
                  flex-1
                  min-h-[260px]
                  bg-gray-200
                  rounded-lg
                  flex
                  flex-col
                  items-center
                  justify-center
                  text-gray-500
                "
              >
                <Box size={40} className="mb-3" />

                <p>VR environment preview</p>

                <p className="text-sm">
                  {isRunning ? "Waiting for VR feed..." : "Awaiting session start"}
                </p>
              </div>
            </section>
          </div>
        </div>

        <section className="border border-gray-300 rounded-xl p-5 mb-6">
          <h2 className="text-lg font-medium mb-4">Live Metrics</h2>

          {/*
            TODO (backend): these render "--" until setMetrics is
            populated from the real feed (see handleStartSession).
          */}
          <div className="mb-4">
            <p className="text-sm text-gray-500 mb-1">Head Rotation</p>

            <div className="flex flex-col gap-1 text-gray-700">
              <p>X: {formatMetric(metrics.headRotation.x, "°")}</p>
              <p>Y: {formatMetric(metrics.headRotation.y, "°")}</p>
              <p>Z: {formatMetric(metrics.headRotation.z, "°")}</p>
            </div>
          </div>

          <div className="border-t border-gray-200 pt-4 mb-4">
            <p className="text-sm text-gray-500 mb-1">Position</p>

            <div className="flex flex-col gap-1 text-gray-700">
              <p>X: {formatMetric(metrics.position.x, "m")}</p>
              <p>Y: {formatMetric(metrics.position.y, "m")}</p>
              <p>Z: {formatMetric(metrics.position.z, "m")}</p>
            </div>
          </div>

          <div className="border-t border-gray-200 pt-4">
            <p className="text-sm text-gray-500 mb-1">Heart Rate</p>

            <p className="text-gray-700">
              {formatMetric(metrics.heartRate, " bpm")}
            </p>
          </div>
        </section>

        {isCompleted && (
          <section className="border border-gray-300 rounded-xl p-5">
            <h2 className="text-lg font-medium mb-4">Export Data</h2>

            <button
              onClick={handleDownloadResults}
              className="
                w-full
                sm:w-auto
                flex
                items-center
                justify-center
                gap-2
                border
                border-gray-300
                hover:bg-gray-50
                rounded-lg
                px-5
                py-2.5
                transition
              "
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
