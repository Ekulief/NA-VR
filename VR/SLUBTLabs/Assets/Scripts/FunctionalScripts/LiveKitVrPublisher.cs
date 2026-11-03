using System.Collections;
using System.Collections.Generic;
using Firebase.Functions;
using Firebase.Firestore;
using LiveKit;
using LiveKit.Proto;
using UnityEngine;

public class LiveKitVrPublisher : MonoBehaviour
{
    [Header("Assign the XR headset camera")]
    [SerializeField] private Camera xrCamera;

    [Header("Instructor spectator livestream (does not change the VR headset view)")]
    [Tooltip("Render a separate, wider camera for the instructor. Turn off to restore the original headset screen capture.")]
    [SerializeField] private bool useWideSpectatorView = true;
    [Tooltip("Vertical field of view for the 16:9 spectator camera. The XR camera remains unchanged.")]
    [SerializeField, Range(80f, 120f)] private float spectatorFieldOfView = 100f;
    [SerializeField] private int spectatorWidth = 960;
    [SerializeField] private int spectatorHeight = 540;

    private Room _room;
    private RtcVideoSource _videoSource;
    private Coroutine _videoUpdateCoroutine;
    private Camera _spectatorCamera;
    private RenderTexture _spectatorTexture;
    private LocalVideoTrack _videoTrack;

    private bool _isStarting;
    private bool _isStreaming;

    // A web-created attempt, not a loaded Unity scene, controls publishing.
    private ListenerRegistration _deviceListener;
    private ListenerRegistration _progressListener;
    private string _watchedProgressId = "";
    private string _activeBlockId = "";
    private string _activeExperimentId = "";
    private string _deviceDocumentId = "";
    private bool _attemptActive;
    private int _streamGeneration;
    private float _nextRetryAt;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            FirebaseManager.IsInitialized || FirebaseManager.InitializationFailed);
        if (FirebaseManager.InitializationFailed)
        {
            Debug.LogError("[LiveKit] Firebase registration failed; cannot watch web attempts.");
            yield break;
        }

        _deviceDocumentId = FirebaseManager.RegisteredDeviceDocumentId;
        if (string.IsNullOrWhiteSpace(_deviceDocumentId))
        {
            Debug.LogError("[LiveKit] Registered VR device ID is missing.");
            yield break;
        }

        _deviceListener = FirebaseFirestore.DefaultInstance.Collection("vrDevices")
            .Document(_deviceDocumentId).Listen(OnDeviceChanged);
        Debug.Log($"[LiveKit] Watching web attempts on vrDevices/{_deviceDocumentId}.");
    }

    private static string ReadString(DocumentSnapshot doc, string key)
    {
        if (doc == null || !doc.Exists || !doc.ContainsField(key)) return "";
        try { return doc.GetValue<string>(key) ?? ""; }
        catch { return ""; } // Nullable/legacy Firestore fields.
    }

    private void OnDeviceChanged(DocumentSnapshot device)
    {
        // The web pins the complete attempt in one transaction. Never follow
        // a device that is not owned by this anonymous Unity publisher.
        string progressId = ReadString(device, "currentProgressId");
        string blockId = ReadString(device, "currentBlockId");
        string experimentId = ReadString(device, "currentExperimentId");
        bool valid = !string.IsNullOrWhiteSpace(progressId) &&
            !string.IsNullOrWhiteSpace(blockId) &&
            !string.IsNullOrWhiteSpace(experimentId) &&
            ReadString(device, "publisherAuthUid") == FirebaseManager.AuthUid &&
            ReadString(device, "status") == "In Use" &&
            ReadString(device, "streamRoomName") == $"slubt-{blockId}-{_deviceDocumentId}";

        if (!valid)
        {
            ClearAttempt();
            return;
        }

        if (progressId == _watchedProgressId && blockId == _activeBlockId &&
            experimentId == _activeExperimentId)
            return; // Scene/status updates do not restart an existing stream.

        ClearAttempt();
        _watchedProgressId = progressId;
        _activeBlockId = blockId;
        _activeExperimentId = experimentId;
        _progressListener = FirebaseFirestore.DefaultInstance
            .Collection("experimentProgress").Document(progressId)
            .Listen(snapshot => OnProgressChanged(progressId, snapshot));
        Debug.Log($"[LiveKit] Waiting for attempt {progressId} state.");
    }

    private void OnProgressChanged(string progressId, DocumentSnapshot progress)
    {
        if (progressId != _watchedProgressId) return; // Old listener callback.

        string control = ReadString(progress, "sessionControl").Trim().ToLowerInvariant();
        bool active = progress != null && progress.Exists &&
            ReadString(progress, "blockId") == _activeBlockId &&
            ReadString(progress, "experimentId") == _activeExperimentId &&
            ReadString(progress, "vrId") == FirebaseManager.RegisteredVrId &&
            ReadString(progress, "completionStatus") != "Completed" &&
            (control == "idle" || control == "running" || control == "paused");

        if (!active)
        {
            if (_attemptActive) Debug.Log($"[LiveKit] Attempt {progressId} ended; stopping publisher.");
            _attemptActive = false;
            if (_isStarting || _isStreaming) StopStreaming();
            return;
        }

        if (!_attemptActive)
        {
            _attemptActive = true;
            _nextRetryAt = 0f;
            Debug.Log($"[LiveKit] Web attempt {progressId} is active ({control}); publishing now.");
            StartStreaming();
        }
        // Pausing does not end the attempt; keep the spectator stream active.
    }

    private void ClearAttempt()
    {
        _progressListener?.Stop();
        _progressListener = null;
        _watchedProgressId = "";
        _activeBlockId = "";
        _activeExperimentId = "";
        _attemptActive = false;
        if (_isStarting || _isStreaming) StopStreaming();
    }

    private void Update()
    {
        // A temporary Functions/LiveKit error should not require the student
        // to end and recreate the attempt before the headset retries.
        if (_attemptActive && !_isStarting && !_isStreaming &&
            Time.realtimeSinceStartup >= _nextRetryAt)
            StartStreaming();
    }

    private bool IsStillCurrent(int generation, string progressId) =>
        _attemptActive && generation == _streamGeneration &&
        progressId == _watchedProgressId;

    private void RetryLater()
    {
        _isStarting = false;
        _nextRetryAt = Time.realtimeSinceStartup + 5f;
    }

    public void StartStreaming()
    {
        if (!_attemptActive || _isStarting || _isStreaming)
            return;

        StartCoroutine(StartStreamingRoutine());
    }

    private IEnumerator StartStreamingRoutine()
    {
        _isStarting = true;
        int generation = _streamGeneration;
        string progressId = _watchedProgressId;
        string blockId = _activeBlockId;
        string experimentId = _activeExperimentId;

        if (xrCamera == null)
            xrCamera = Camera.main;
        if (xrCamera == null)
        {
            Debug.LogError("[LiveKit] Assign the persistent XR headset camera.");
            RetryLater();
            yield break;
        }

        // Use the pinned web attempt, even while the headset is still in the
        // main VR hub. The experiment scene/config need not be loaded.
        var callable = FirebaseFunctions.DefaultInstance
            .GetHttpsCallable("createLiveStreamToken");
        var tokenTask = callable.CallAsync(new Dictionary<string, object>
        {
            { "blockId", blockId },
            { "vrDeviceId", _deviceDocumentId },
            { "experimentId", experimentId },
            { "role", "publisher" }
        });

        yield return new WaitUntil(() => tokenTask.IsCompleted);
        if (!IsStillCurrent(generation, progressId)) yield break;

        if (tokenTask.IsFaulted || tokenTask.IsCanceled)
        {
            Debug.LogError("[LiveKit] Token request failed: " +
                (tokenTask.Exception?.GetBaseException().Message ?? "Canceled"));
            RetryLater();
            yield break;
        }

        var response = tokenTask.Result.Data as IDictionary<string, object>;
        if (response == null || !response.ContainsKey("serverUrl") ||
            !response.ContainsKey("token"))
        {
            Debug.LogError("[LiveKit] Invalid publisher token response.");
            RetryLater();
            yield break;
        }

        string serverUrl = response["serverUrl"]?.ToString();
        string token = response["token"]?.ToString();
        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError("[LiveKit] Publisher URL or token is empty.");
            RetryLater();
            yield break;
        }

        if (response.TryGetValue("roomName", out var roomObj))
            Debug.Log("[LiveKit] Assigned room = " + roomObj);

        Room room = new Room();
        _room = room;
        var connect = room.Connect(serverUrl, token, new LiveKit.RoomOptions());
        yield return connect;

        // Disconnect again after a pending connect resolves: the SDK's
        // Disconnect() can be a no-op while Connect() is still pending.
        if (!IsStillCurrent(generation, progressId))
        {
            room.Disconnect();
            yield break;
        }
        if (connect.IsError)
        {
            Debug.LogError("[LiveKit] Room connection failed. Check the LiveKit SDK logs.");
            room.Disconnect();
            _room = null;
            RetryLater();
            yield break;
        }

        if (useWideSpectatorView)
        {
            try
            {
                CreateSpectatorCamera();
                _videoSource = new TextureVideoSource(_spectatorTexture);
                Debug.Log($"[LiveKit] Wide spectator camera enabled: {spectatorFieldOfView:0} degrees.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[LiveKit] Could not create the spectator camera; using headset screen capture. " + e.Message);
                DestroySpectatorCamera();
                _videoSource = new ScreenVideoSource();
            }
        }
        else
        {
            _videoSource = new ScreenVideoSource();
        }

        _videoTrack = LocalVideoTrack.CreateVideoTrack(
            "vr-first-person", _videoSource, room);
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
        var publish = room.LocalParticipant.PublishTrack(_videoTrack, options);
        yield return publish;

        if (!IsStillCurrent(generation, progressId))
        {
            room.Disconnect();
            yield break;
        }
        if (publish.IsError)
        {
            Debug.LogError("[LiveKit] Video publishing failed. Check the LiveKit SDK logs.");
            StopStreaming();
            _nextRetryAt = Time.realtimeSinceStartup + 5f;
            yield break;
        }

        _videoSource.Start();
        _videoUpdateCoroutine = StartCoroutine(_videoSource.Update());
        _isStreaming = true;
        _isStarting = false;
        Debug.Log("[LiveKit] VR video is now publishing for the active web attempt.");
    }

    private void CreateSpectatorCamera()
    {
        // Keep the camera in the persistent hub scene; experiment scenes load
        // additively, so the exact same camera can follow the XR head in each.
        GameObject spectator = new GameObject("LiveKit Instructor Spectator Camera");
        spectator.transform.SetParent(transform, false);
        _spectatorCamera = spectator.AddComponent<Camera>();
        _spectatorCamera.enabled = false;
        _spectatorCamera.CopyFrom(xrCamera);
        _spectatorCamera.enabled = false;
        _spectatorCamera.stereoTargetEye = StereoTargetEyeMask.None;
        _spectatorCamera.usePhysicalProperties = false;
        _spectatorCamera.fieldOfView = Mathf.Clamp(spectatorFieldOfView, 80f, 120f);
        _spectatorCamera.aspect = 16f / 9f;
        _spectatorCamera.ResetProjectionMatrix();
        _spectatorCamera.allowHDR = false;
        _spectatorCamera.allowMSAA = false;

        // Target only this texture, never the player's VR eye displays.
        _spectatorTexture = new RenderTexture(
            Mathf.Clamp(spectatorWidth, 320, 1920),
            Mathf.Clamp(spectatorHeight, 180, 1080),
            24, RenderTextureFormat.ARGB32);
        _spectatorTexture.name = "LiveKit Instructor Spectator Stream";
        if (!_spectatorTexture.Create())
            throw new System.InvalidOperationException("Could not allocate the spectator video texture.");
        _spectatorCamera.targetTexture = _spectatorTexture;
        FollowXrHead();
        _spectatorCamera.enabled = true;
    }

    private void LateUpdate()
    {
        if (_spectatorCamera != null && xrCamera != null)
            FollowXrHead();
    }

    private void FollowXrHead()
    {
        _spectatorCamera.transform.SetPositionAndRotation(
            xrCamera.transform.position, xrCamera.transform.rotation);
    }

    private void DestroySpectatorCamera()
    {
        if (_spectatorCamera != null)
        {
            _spectatorCamera.enabled = false;
            _spectatorCamera.targetTexture = null;
            Destroy(_spectatorCamera.gameObject);
            _spectatorCamera = null;
        }
        if (_spectatorTexture != null)
        {
            _spectatorTexture.Release();
            Destroy(_spectatorTexture);
            _spectatorTexture = null;
        }
    }

    public void StopStreaming()
    {
        // Invalidate any in-flight token/connect/publish routine.
        _streamGeneration++;
        // Stop the capture coroutine before releasing its backing texture.
        if (_videoUpdateCoroutine != null)
        {
            StopCoroutine(_videoUpdateCoroutine);
            _videoUpdateCoroutine = null;
        }
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
        DestroySpectatorCamera();

        _isStreaming = false;
        _isStarting = false;

        Debug.Log("[LiveKit] VR video publisher stopped.");
    }

    private void OnDestroy()
    {
        _deviceListener?.Stop();
        _progressListener?.Stop();
        StopStreaming();
    }
}
