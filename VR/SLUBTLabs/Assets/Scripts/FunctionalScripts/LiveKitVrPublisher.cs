
using System.Collections;
using System.Collections.Generic;
using Firebase.Functions;
using LiveKit;
using LiveKit.Proto;
using UnityEngine;

public class LiveKitVrPublisher : MonoBehaviour
{
    [Header("Assign the XR headset camera")]
    [SerializeField] private Camera xrCamera;

    private Room _room;
    private ScreenVideoSource _videoSource;
    private LocalVideoTrack _videoTrack;

    private bool _isStarting;
    private bool _isStreaming;

    public void StartStreaming()
    {
        if (_isStarting || _isStreaming)
            return;

        StartCoroutine(StartStreamingRoutine());
    }

    private IEnumerator StartStreamingRoutine()
    {
        _isStarting = true;

        // 1. Validate the active experiment configuration.
        ExperimentConfig config = ExperimentConfigLoader.Current;

        if (config == null)
        {
            Debug.LogError("[LiveKit] Experiment config is not ready.");
            _isStarting = false;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(config.blockId))
        {
            Debug.LogError("[LiveKit] Missing active blockId.");
            _isStarting = false;
            yield break;
        }

        // 2. Find the XR headset camera.
        if (xrCamera == null)
            xrCamera = Camera.main;

        if (xrCamera == null)
        {
            Debug.LogError("[LiveKit] Assign the XR headset camera.");
            _isStarting = false;
            yield break;
        }

        // 3. Validate the VR device document ID.
        ExperimentLoader loader =
            FindAnyObjectByType<ExperimentLoader>();

        if (loader == null ||
            string.IsNullOrWhiteSpace(loader.vrDeviceDocumentId))
        {
            Debug.LogError(
                "[LiveKit] ExperimentLoader or VR device document ID is missing."
            );

            _isStarting = false;
            yield break;
        }

        // The session may have loaded before auth finished; never request an unauthenticated token.
        if (!FirebaseManager.IsInitialized || !FirebaseManager.IsAuthenticated)
        {
            Debug.LogError("[LiveKit] Firebase anonymous sign-in has not completed.");
            _isStarting = false;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(config.experimentId))
        {
            Debug.LogError("[LiveKit] Missing Firestore experiment document ID.");
            _isStarting = false;
            yield break;
        }

        // 4. Request a publisher token from Firebase.
        // Never put your LiveKit API secret in the Unity project.
        var callable = FirebaseFunctions.DefaultInstance
            .GetHttpsCallable("createLiveStreamToken");

        var tokenTask = callable.CallAsync(
            new Dictionary<string, object>
            {
                { "blockId", config.blockId },
                { "vrDeviceId", loader.vrDeviceDocumentId },
                { "experimentId", config.experimentId },
                { "role", "publisher" }
            }
        );

        yield return new WaitUntil(() => tokenTask.IsCompleted);

        if (tokenTask.IsFaulted || tokenTask.IsCanceled)
        {
            string reason = tokenTask.IsCanceled
                ? "The request was canceled."
                : tokenTask.Exception?.GetBaseException().Message
                  ?? "Unknown Firebase error.";

            Debug.LogError(
                "[LiveKit] Token request failed: " + reason
            );

            _isStarting = false;
            yield break;
        }

        // 5. Read the server URL and publisher token.
        var response =
            tokenTask.Result.Data as IDictionary<string, object>;

        if (response == null ||
            !response.ContainsKey("serverUrl") ||
            !response.ContainsKey("token"))
        {
            Debug.LogError(
                "[LiveKit] Invalid token response. Expected serverUrl and token."
            );

            _isStarting = false;
            yield break;
        }

        string serverUrl = response["serverUrl"]?.ToString();
        string token = response["token"]?.ToString();

        if (string.IsNullOrWhiteSpace(serverUrl) ||
            string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError(
                "[LiveKit] Server URL or token is empty."
            );

            _isStarting = false;
            yield break;
        }

        if (response.TryGetValue("roomName", out var roomObj))
            Debug.Log("[LiveKit] Assigned room = " + roomObj);

        // 6. Connect to the LiveKit room.
        _room = new Room();

        var connect = _room.Connect(
            serverUrl,
            token,
            new LiveKit.RoomOptions()
        );

        yield return connect;

        if (connect.IsError)
        {
            Debug.LogError(
                "[LiveKit] Room connection failed. Check the LiveKit SDK logs."
            );

            _room.Disconnect();
            _room = null;

            _isStarting = false;
            yield break;
        }

        // 7. Create a video source from the XR camera.
        _videoSource = new ScreenVideoSource();

        _videoTrack = LocalVideoTrack.CreateVideoTrack(
            "vr-first-person",
            _videoSource,
            _room
        );

        // 8. Configure the video publishing options.
        var encoding = new VideoEncoding
        {
            MaxBitrate = 600000,
            MaxFramerate = 15
        };

        var options = new TrackPublishOptions
        {
            VideoCodec = VideoCodec.Vp8,
            VideoEncoding = encoding,
            Simulcast = true,
            Source = TrackSource.SourceCamera
        };

        // 9. Publish the XR camera track.
        var publish = _room.LocalParticipant.PublishTrack(
            _videoTrack,
            options
        );

        yield return publish;

        if (publish.IsError)
        {
            Debug.LogError(
                "[LiveKit] Video publishing failed. Check the LiveKit SDK logs."
            );

            StopStreaming();
            yield break;
        }

        // 10. Start sending camera frames.
        _videoSource.Start();
        StartCoroutine(_videoSource.Update());

        _isStreaming = true;
        _isStarting = false;

        Debug.Log(
            "[LiveKit] First-person VR video is now publishing."
        );
    }

    public void StopStreaming()
    {
        // Stop capturing frames from the camera.
        if (_videoSource != null)
        {
            _videoSource.Stop();
            _videoSource = null;
        }

        // Disconnect this publisher from the room.
        if (_room != null)
        {
            _room.Disconnect();
            _room = null;
        }

        _videoTrack = null;

        _isStreaming = false;
        _isStarting = false;

        Debug.Log("[LiveKit] VR video publisher stopped.");
    }

    private void OnDestroy()
    {
        StopStreaming();
    }
}
