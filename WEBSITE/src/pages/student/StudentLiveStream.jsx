import { useEffect, useRef, useState } from "react";
import { getFunctions, httpsCallable } from "firebase/functions";
import { LiveKitRoom, VideoTrack, useTracks } from "@livekit/components-react";
import { Track } from "livekit-client";
import { Maximize2, X } from "lucide-react";
import "@livekit/components-styles";

// Only display the publisher for the VR device returned by the trusted token Function.
function HeadsetVideo({ vrDeviceId, onVideoChange }) {
  const tracks = useTracks([Track.Source.Camera], { onlySubscribed: true });
  const track = tracks.find((item) => item.participant?.identity === `vr-${vrDeviceId}`);

  useEffect(() => {
    onVideoChange(Boolean(track));
  }, [track, onVideoChange]);

  if (!track) {
    return (
      <div className="flex h-full w-full items-center justify-center p-5 text-center text-sm text-gray-300">
        Connected. Waiting for the VR headset to start streaming this experiment...
      </div>
    );
  }

  return (
    <div className="h-full w-full [&_video]:h-full [&_video]:w-full [&_video]:object-contain">
      <VideoTrack trackRef={track} />
    </div>
  );
}

export default function StudentLiveStream({ blockId, experimentId, progressId, experimentName }) {
  const [tokenInfo, setTokenInfo] = useState(null);
  const [error, setError] = useState("");
  const [phase, setPhase] = useState("Requesting viewer token...");
  const [hasVideo, setHasVideo] = useState(false);
  const [retry, setRetry] = useState(0);
  const [expanded, setExpanded] = useState(false);
  const enlargeButtonRef = useRef(null);
  const closeButtonRef = useRef(null);
  const wasExpanded = useRef(false);

  useEffect(() => {
    let canceled = false;
    Promise.resolve().then(() => {
      if (canceled) return;
      setTokenInfo(null);
      setError("");
      setHasVideo(false);
      setPhase("Requesting viewer token...");
    });

    (async () => {
      try {
        // The server verifies the student owns this exact active attempt,
        // resolves its assigned VR device and grants subscribe-only access.
        const requestToken = httpsCallable(getFunctions(), "createLiveStreamToken");
        const response = await requestToken({ blockId, experimentId, progressId, role: "viewer" });
        if (!canceled) {
          setTokenInfo(response.data);
          setPhase("Connecting to LiveKit...");
        }
      } catch (err) {
        console.error("[Student Live VR] Viewer token failed:", err);
        if (!canceled) setError(err?.message || "Could not connect to the headset livestream.");
      }
    })();

    return () => { canceled = true; };
  }, [blockId, experimentId, progressId, retry]);

  // Restore focus after closing the enlarged view.
  useEffect(() => {
    if (expanded) closeButtonRef.current?.focus();
    else if (wasExpanded.current) enlargeButtonRef.current?.focus();
    wasExpanded.current = expanded;
  }, [expanded]);

  // Like the instructor's live viewer, enlarging only changes CSS positioning.
  // The existing LiveKitRoom stays mounted and does not rejoin the stream.
  useEffect(() => {
    if (!expanded) return undefined;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event) => {
      if (event.key === "Escape") {
        setExpanded(false);
      }
      if (event.key !== "Tab") return;
      const dialog = document.querySelector("[data-student-stream-dialog]");
      const focusable = dialog?.querySelectorAll('button:not([disabled]), [href], [tabindex]:not([tabindex="-1"])');
      if (!focusable?.length) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [expanded]);

  return (
    <>
      {expanded && (
        <button
          type="button"
          aria-label="Close enlarged livestream"
          onClick={() => setExpanded(false)}
          className="fixed inset-0 z-40 cursor-default bg-gray-950/75 backdrop-blur-[2px]"
        />
      )}
      <div
        role={expanded ? "dialog" : undefined}
        aria-modal={expanded ? true : undefined}
        aria-label={expanded ? `Live VR preview — ${experimentName}` : undefined}
        data-student-stream-dialog={expanded ? "" : undefined}
        className={expanded
          ? "fixed left-1/2 top-1/2 z-50 flex max-h-[calc(100dvh-2rem)] w-[min(96vw,75rem)] -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl"
          : "absolute inset-0 flex flex-col overflow-hidden rounded-lg bg-gray-950"}
      >
        {expanded && (
          <div className="flex items-center justify-between gap-3 border-b border-gray-200 px-5 py-3">
            <span className="min-w-0 truncate font-medium text-gray-900">{experimentName} — Live VR Preview</span>
            <button
              ref={closeButtonRef}
              type="button"
              aria-label="Close expanded VR view"
              onClick={() => setExpanded(false)}
              className="rounded-full border border-gray-200 p-2 text-gray-700 hover:bg-gray-100 focus-visible:outline-2 focus-visible:outline-indigo-600"
            >
              <X size={20} />
            </button>
          </div>
        )}
        <div className={expanded
          ? "relative flex min-h-0 flex-1 items-center justify-center bg-gray-950 text-white aspect-video max-h-[calc(100dvh-12rem)]"
          : "relative min-h-0 flex-1 bg-gray-950 text-white"}
        >
          {error ? (
            <div className="flex h-full w-full flex-col items-center justify-center gap-3 p-5 text-center text-sm">
              <p>{error}</p>
              <button
                type="button"
                onClick={() => setRetry((value) => value + 1)}
                className="relative z-20 rounded bg-white px-3 py-2 text-gray-900 hover:bg-gray-100"
              >
                Retry viewer connection
              </button>
            </div>
          ) : tokenInfo ? (
            <LiveKitRoom
              key={`${progressId}-${retry}`}
              serverUrl={tokenInfo.serverUrl}
              token={tokenInfo.token}
              connect
              audio={false}
              video={false}
              onConnected={() => setPhase("Connected. Waiting for headset video...")}
              onDisconnected={() => { setHasVideo(false); setPhase("LiveKit disconnected"); }}
              onError={(err) => { setHasVideo(false); setError(err?.message || "LiveKit connection failed"); }}
            >
              <HeadsetVideo vrDeviceId={tokenInfo.vrDeviceId} onVideoChange={setHasVideo} />
            </LiveKitRoom>
          ) : (
            <div className="flex h-full w-full items-center justify-center p-5 text-center text-sm text-gray-300">{phase}</div>
          )}
          {hasVideo && (
            <span className="pointer-events-none absolute left-3 top-3 rounded bg-red-600 px-2.5 py-1 text-xs font-semibold text-white">
              ● LIVE
            </span>
          )}
          {!expanded && !error && (
            <button
              ref={enlargeButtonRef}
              type="button"
              onClick={() => setExpanded(true)}
              aria-label="Enlarge this experiment's livestream"
              className="absolute inset-0 z-10 cursor-zoom-in focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-indigo-500"
            >
              <Maximize2 size={18} aria-hidden="true" className="absolute bottom-3 right-3 text-white/80" />
            </button>
          )}
        </div>
        {expanded && (
          <div className="flex items-center justify-between gap-3 border-t border-gray-200 px-5 py-3 text-sm text-gray-600">
            <span>{hasVideo ? "Live spectator view" : "Headset preview"}</span>
            <span>Press Esc or X to close</span>
          </div>
        )}
      </div>
    </>
  );
}
