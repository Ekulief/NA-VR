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

        // 7. Use a dedicated mono spectator camera with a wider field of view.
        // ScreenVideoSource captured the headset's narrow eye view and could not
        // reveal scenery outside that crop. The student's XR camera is untouched.
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

        // 9. Publish the spectator (or fallback screen) track.
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
        _videoUpdateCoroutine = StartCoroutine(_videoSource.Update());

        _isStreaming = true;
        _isStarting = false;

        Debug.Log(
            "[LiveKit] VR video is now publishing to the instructor."
        );
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
        StopStreaming();
    }
}
