import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { doc, getDoc } from "firebase/firestore";

import { db } from "../../config/firebase-config";

import { ArrowLeft, Box } from "lucide-react";

export default function StudentExperimentRun() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();

  const [experiment, setExperiment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [vrIdentifier, setVrIdentifier] = useState("");
  const [connecting, setConnecting] = useState(false);
  const [connected, setConnected] = useState(false);

  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const [metrics, setMetrics] = useState({
    headRotation: { x: 0, y: 0, z: 0 },
    position: { x: 0, y: 0, z: 0 },
    heartRate: 0,
  });

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

  const resetSession = () => {
    setConnected(false);
    setConnecting(false);

    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }

    setElapsedSeconds(0);

    setMetrics({
      headRotation: { x: 0, y: 0, z: 0 },
      position: { x: 0, y: 0, z: 0 },
      heartRate: 0,
    });
  };

  const handleConnect = () => {
    if (connected) {
      resetSession();
      return;
    }

    if (!vrIdentifier.trim()) {
      alert("Please enter a VR identifier.");
      return;
    }

    setConnecting(true);

    connectTimeoutRef.current = setTimeout(() => {
      setConnecting(false);
      setConnected(true);

      timerRef.current = setInterval(() => {
        setElapsedSeconds((previous) => previous + 1);

        const now = Date.now();

        setMetrics({
          headRotation: {
            x: Math.round(Math.sin(now / 900) * 20),
            y: Math.round(Math.cos(now / 1300) * 30),
            z: Math.round(Math.sin(now / 1600) * 10),
          },
          position: {
            x: Number((Math.sin(now / 1200) * 1.5).toFixed(2)),
            y: Number((1.6 + Math.sin(now / 2000) * 0.05).toFixed(2)),
            z: Number((Math.cos(now / 1400) * 1.2).toFixed(2)),
          },
          heartRate: 68 + Math.round(Math.sin(now / 2500) * 8),
        });
      }, 1000);
    }, 900);
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

  const statusLabel = connected
    ? "In Session"
    : connecting
      ? "Connecting"
      : "Ready";

  const statusStyle = connected
    ? "bg-indigo-100 text-indigo-700"
    : connecting
      ? "bg-yellow-100 text-yellow-700"
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

        <p className="text-gray-500 mb-8">
          {experiment.participantInstructions ||
            experiment.instructions ||
            "Run this experiment in VR."}
        </p>

        <div className="grid lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 flex flex-col gap-6">
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Control</h2>

              <label className="block text-sm text-gray-600 mb-1">
                VR Identifier
              </label>

              <input
                type="text"
                value={vrIdentifier}
                onChange={(e) => setVrIdentifier(e.target.value)}
                disabled={connected || connecting}
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
                  disabled:text-gray-400
                "
              />

              <button
                onClick={handleConnect}
                disabled={connecting}
                className={`
                  w-full
                  rounded-full
                  py-3
                  text-center
                  transition
                  ${
                    connected
                      ? "bg-indigo-800 hover:bg-indigo-700 text-white"
                      : "border border-gray-300 hover:bg-gray-50"
                  }
                  ${connecting ? "opacity-60 cursor-wait" : ""}
                `}
              >
                {connecting
                  ? "Connecting..."
                  : connected
                    ? "Disconnect VR Headset"
                    : "Connect VR Headset"}
              </button>
            </section>

            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">VR Preview</h2>

              <div
                className="
                  bg-gray-200
                  rounded-lg
                  h-64
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
                  {connected ? "Session in progress" : "Awaiting session start"}
                </p>
              </div>
            </section>
          </div>

          <div className="flex flex-col gap-6">
            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Session Timer</h2>

              <p className="text-4xl font-medium text-center tabular-nums">
                {formatTime(elapsedSeconds)}
              </p>
            </section>

            <section className="border border-gray-300 rounded-xl p-5">
              <h2 className="text-lg font-medium mb-4">Live Metrics</h2>

              <div className="mb-4">
                <p className="text-sm text-gray-500 mb-1">Head Rotation</p>

                <div className="flex flex-col gap-1 text-gray-700">
                  <p>X: {metrics.headRotation.x}°</p>
                  <p>Y: {metrics.headRotation.y}°</p>
                  <p>Z: {metrics.headRotation.z}°</p>
                </div>
              </div>

              <div className="border-t border-gray-200 pt-4 mb-4">
                <p className="text-sm text-gray-500 mb-1">Position</p>

                <div className="flex flex-col gap-1 text-gray-700">
                  <p>X: {metrics.position.x.toFixed(2)}m</p>
                  <p>Y: {metrics.position.y.toFixed(2)}m</p>
                  <p>Z: {metrics.position.z.toFixed(2)}m</p>
                </div>
              </div>

              <div className="border-t border-gray-200 pt-4">
                <p className="text-sm text-gray-500 mb-1">Heart Rate</p>

                <p className="text-gray-700">{metrics.heartRate} bpm</p>
              </div>
            </section>
          </div>
        </div>
      </main>
    </div>
  );
}
