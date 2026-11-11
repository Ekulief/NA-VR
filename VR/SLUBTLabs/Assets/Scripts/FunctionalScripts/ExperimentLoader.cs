using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Functions;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Management;

public class ExperimentLoader : MonoBehaviour
{
    [Header("Player")]
    public GameObject xrOrigin;

    [Header("Spawn Position Settings")]
    public bool useFixedSpawnHeight = false;
    public float fixedSpawnHeight = 1.6f;
    public bool snapToFloor = true;
    public float floorSnapDistance = 3f;

    [Header("Main Scene Visibility")]
    public GameObject[] mainSceneObjects;

    [Header("Transition / Loading Screen")]
    public CanvasGroup fadeCanvas;
    public Image logoImage;
    public TMPro.TextMeshProUGUI loadingText;
    public float fadeDuration = 0.5f;
    public float loadingScreenDistance = 1.5f;

    [Header("Loading Void")]
    [Tooltip("The XR Origin's Main Camera.")]
    public Camera xrCamera;
    [Tooltip("Layer the loading canvas (and all its children) must be on.")]
    public string loadingLayerName = "LoadingUI";
    [Tooltip("Max seconds to wait for the experiment scene to signal it is ready.")]
    public float readyTimeout = 5f;
    [Tooltip("Max seconds to wait for Firestore/Functions status updates (In Use / Available). Prevents hanging offline.")]
    public float statusUpdateTimeout = 10f;

    [Header("Startup Disclaimers")]
    [Tooltip("Optional. Shows consent / disclaimer pages during the first startup.")]
    public StartupDisclaimers disclaimers;

    [Header("Slow Screen Fade")]
    [Tooltip("Seconds for the whole view to fade to/from black.")]
    public float screenFadeDuration = 1.5f;
    [Tooltip("Optional. Transparent unlit material (e.g. URP/Unlit, Surface = Transparent). Leave empty to use Sprites/Default.")]
    public Material fadeMaterial;

    [Header("Firebase")]
    public ExperimentConfig config;
    public string vrDeviceDocumentId = "";

    [Header("Live Streaming")]
    public LiveKitVrPublisher livePublisher;

    [Header("Teleport Stabilization")]
    public float controllerReenableDelay = 0.25f;

    [Header("Results Summary (Main Scene)")]
    public GameObject summaryPanel;
    public TMPro.TextMeshProUGUI summaryTitleText;
    public TMPro.TextMeshProUGUI summaryBodyText;
    public UnityEngine.UI.Button summaryDismissButton;

    private string _pendingSummaryTitle;
    private string _pendingSummaryBody;
    private bool _hasPendingSummary;
    private bool _isLoading = false;
    private Scene _loadedScene;
    private bool _experimentSceneLoaded = false;
    private string _currentSceneName = "";

    // Loading void state
    private int _savedMask;
    private CameraClearFlags _savedClear;
    private Color _savedBg;
    private bool _inVoid;

    // Player freeze state (Memory Scene 2 only)
    private bool _playerFrozen;
    private CharacterController _frozenCC;
    private bool _frozenCCWasEnabled;
    private MonoBehaviour _frozenGravity;
    private bool _frozenGravityWasEnabled;

    // Screen fade state
    private Renderer _fadeQuad;
    private Material _fadeMat;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 1f;
            fadeCanvas.blocksRaycasts = true;
            fadeCanvas.interactable = false;
        }
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (summaryDismissButton != null)
            summaryDismissButton.onClick.AddListener(HideSummary);
        if (logoImage != null)
            logoImage.enabled = true;

        if (loadingText != null)
        {
            loadingText.enabled = true;
            loadingText.text = "Initializing VR & Connecting...";
        }

        // Keep the loading screen stationary in world space.
        PlaceLoadingScreenInFront();

        // First boot also starts in the black void.
        EnterLoadingVoid();
    }

    private IEnumerator Start()
    {
        PlaceLoadingScreenInFront();

#if UNITY_EDITOR
        float xrWaitTime = 0f;

        while (xrWaitTime < 3f &&
               (XRGeneralSettings.Instance == null ||
                XRGeneralSettings.Instance.Manager == null ||
                !XRGeneralSettings.Instance.Manager.isInitializationComplete))
        {
            xrWaitTime += Time.unscaledDeltaTime;
            yield return null;
        }
#else
        yield return new WaitUntil(() =>
            XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.isInitializationComplete);
#endif

        PlaceLoadingScreenInFront();   // headset is tracking now, so place the logo correctly

        // Show consent / disclaimer pages while the app connects in the background.
        bool disclaimersDone = true;
        if (disclaimers != null)
        {
            disclaimersDone = false;
            StartCoroutine(RunDisclaimers(() => disclaimersDone = true));
        }

        if (loadingText != null)
            loadingText.text = "Connecting to Database...";

        if (!FirebaseManager.IsInitialized)
        {
            yield return new WaitUntil(() => FirebaseManager.IsInitialized || FirebaseManager.InitializationFailed);
        }
        if (FirebaseManager.InitializationFailed)
        {
            Debug.LogError("[ExperimentLoader] Firebase startup or VR registration failed.");
            if (loadingText != null)
                loadingText.text = "Cannot register VR headset.\nCheck Wi-Fi, then restart the app.";
            yield break;
        }
        // No inspector pairing: use the document ID verified during startup.
        vrDeviceDocumentId = FirebaseManager.RegisteredDeviceDocumentId;

        // Clear any stale "In Use" left by a previous power cut / forced shutdown.
        if (!string.IsNullOrWhiteSpace(vrDeviceDocumentId))
        {
            if (loadingText != null)
                loadingText.text = "Clearing previous session status...";

            Task clearTask = UpdateDeviceStatus("Available", "");
            yield return StartCoroutine(WaitForTaskWithTimeout(
                clearTask, statusUpdateTimeout, "Clearing previous session…"));

            if (clearTask.IsFaulted || clearTask.IsCanceled || !clearTask.IsCompleted)
            {
                Debug.LogWarning("[ExperimentLoader] Could not reset device status to Available at startup (offline or timeout). Continuing.");
            }
        }

        if (loadingText != null)
            loadingText.text = "Loading Experiment Configuration...";

        yield return new WaitUntil(() =>
            ExperimentConfigLoader.IsReady &&
            ExperimentConfigLoader.Current != null);

        config = ExperimentConfigLoader.Current;

        // Don't reveal the hub until every disclaimer page has finished.
        yield return new WaitUntil(() => disclaimersDone);

        if (loadingText != null)
            loadingText.text = "Ready!";

        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitForEndOfFrame();

        yield return StartCoroutine(RevealWorld());
    }

    private IEnumerator RunDisclaimers(System.Action onDone)
    {
        if (disclaimers != null && disclaimers.WillShow)
        {
            // Hide the logo and status text so they don't overlap the pages.
            if (logoImage != null) logoImage.enabled = false;
            if (loadingText != null) loadingText.enabled = false;

            yield return StartCoroutine(disclaimers.Play());

            if (logoImage != null) logoImage.enabled = true;
            if (loadingText != null) loadingText.enabled = true;
        }

        onDone?.Invoke();
    }

    // ------------------------------------------------------------------
    // Loading void (black screen + loading UI only)
    // ------------------------------------------------------------------

    private void EnterLoadingVoid()
    {
        if (xrCamera == null || _inVoid) return;

        int layer = LayerMask.NameToLayer(loadingLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[ExperimentLoader] Layer '{loadingLayerName}' does not exist.");
            return;
        }

        _savedMask = xrCamera.cullingMask;
        _savedClear = xrCamera.clearFlags;
        _savedBg = xrCamera.backgroundColor;

        xrCamera.cullingMask = 1 << layer;
        xrCamera.clearFlags = CameraClearFlags.SolidColor;
        xrCamera.backgroundColor = Color.black;
        _inVoid = true;
    }

    private void ExitLoadingVoid()
    {
        if (xrCamera == null || !_inVoid) return;

        int layer = LayerMask.NameToLayer(loadingLayerName);
        xrCamera.cullingMask = _savedMask | (1 << layer);
        xrCamera.clearFlags = _savedClear;
        xrCamera.backgroundColor = _savedBg;
        _inVoid = false;
    }

    // Fades the world to black, switches to the void, then fades the loading UI in.
    private IEnumerator EnterLoadingScreen()
    {
        yield return StartCoroutine(FadeScreen(0f, 1f, screenFadeDuration));
        EnterLoadingVoid();
        SetScreenAlpha(0f);                       // void is already black
        yield return StartCoroutine(Fade(0f, 1f));
    }

    // Fades the loading UI out, then slowly fades the world in from black.
    private IEnumerator RevealWorld()
    {
        if (_inVoid)
        {
            yield return StartCoroutine(Fade(1f, 0f));
            SetScreenAlpha(1f);
            ExitLoadingVoid();
            yield return StartCoroutine(WaitForSmoothFrames());
            yield return StartCoroutine(FadeScreen(1f, 0f, screenFadeDuration));
        }
        else
        {
            yield return StartCoroutine(Fade(1f, 0f));
        }
    }

    // Waits until the frame rate has settled (shader/GPU warm-up after the scene appears).
    private IEnumerator WaitForSmoothFrames(float timeout = 1.5f)
    {
        int smooth = 0;
        float waited = 0f;

        while (smooth < 5 && waited < timeout)
        {
            float dt = Time.unscaledDeltaTime;
            waited += dt;
            smooth = dt < 0.04f ? smooth + 1 : 0;
            yield return null;
        }
    }

    // ------------------------------------------------------------------
    // Camera-locked black quad used for the slow fade
    // ------------------------------------------------------------------

    private void EnsureFadeQuad()
    {
        if (_fadeQuad != null || xrCamera == null) return;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "ScreenFadeQuad";
        Destroy(go.GetComponent<Collider>());

        go.transform.SetParent(xrCamera.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        go.transform.localScale = new Vector3(3f, 3f, 1f);

        int layer = LayerMask.NameToLayer(loadingLayerName);
        if (layer >= 0) go.layer = layer;

        _fadeMat = fadeMaterial != null
            ? new Material(fadeMaterial)
            : new Material(Shader.Find("Sprites/Default"));
        _fadeMat.renderQueue = 4000;

        _fadeQuad = go.GetComponent<Renderer>();
        _fadeQuad.sharedMaterial = _fadeMat;
        _fadeQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _fadeQuad.receiveShadows = false;
        _fadeQuad.enabled = false;
    }

    private void SetScreenAlpha(float a)
    {
        EnsureFadeQuad();
        if (_fadeMat == null) return;

        SetMatColor(a);
        _fadeQuad.enabled = a > 0.001f;
    }

    private void SetMatColor(float a)
    {
        Color c = new Color(0f, 0f, 0f, a);
        if (_fadeMat.HasProperty("_BaseColor")) _fadeMat.SetColor("_BaseColor", c);
        if (_fadeMat.HasProperty("_Color")) _fadeMat.SetColor("_Color", c);
    }

    private IEnumerator FadeScreen(float from, float to, float duration)
    {
        EnsureFadeQuad();
        if (_fadeMat == null || duration <= 0f)
        {
            SetScreenAlpha(to);
            yield break;
        }

        _fadeQuad.enabled = true;
        float t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            SetMatColor(Mathf.SmoothStep(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        SetScreenAlpha(to);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>True while an experiment is loading, running, or returning to the hub.</summary>
    public bool IsBusy => _isLoading || _experimentSceneLoaded;

    public void LoadScene(string sceneName)
    {
        if (_isLoading || _experimentSceneLoaded || string.IsNullOrEmpty(sceneName))
            return;

        StartCoroutine(LoadExperimentScene(sceneName));
    }

    public void ReturnToHub()
    {
        if (!_experimentSceneLoaded || _isLoading)
            return;

        GameObject respawnObject = GameObject.Find("Respawn");

        StartCoroutine(UnloadExperimentScene(respawnObject));
    }

    public void ReturnToHub(Vector3 unused) => ReturnToHub();

    /// <summary>
    /// Call from any experiment manager when the trial is finished.
    /// The summary will be shown in the Main VR Scene after ReturnToHub completes.
    /// </summary>
    public void QueueExperimentSummary(string title, string body)
    {
        _pendingSummaryTitle = title ?? "Experiment Complete";
        _pendingSummaryBody = body ?? "";
        _hasPendingSummary = true;
    }

    public void HideSummary()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
        _hasPendingSummary = false;
    }

    private void ShowPendingSummary()
    {
        if (!_hasPendingSummary || summaryPanel == null)
            return;

        if (summaryTitleText != null)
            summaryTitleText.text = _pendingSummaryTitle;

        if (summaryBodyText != null)
            summaryBodyText.text = _pendingSummaryBody;

        summaryPanel.SetActive(true);

        StartCoroutine(AutoHideSummary(20f));
    }

    private IEnumerator AutoHideSummary(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (_hasPendingSummary && summaryPanel != null && summaryPanel.activeSelf)
            HideSummary();
    }

    // ------------------------------------------------------------------
    // Load / unload
    // ------------------------------------------------------------------

    private IEnumerator LoadExperimentScene(string sceneName)
    {
        _isLoading = true;
        _currentSceneName = sceneName;
        SceneReadySignal.Reset();

        // Only freeze if loading Memory Scene 2
        FreezePlayer();

        if (loadingText != null) loadingText.text = "Loading Experiment…";
        yield return StartCoroutine(EnterLoadingScreen());

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError($"[ExperimentLoader] Could not begin loading scene '{sceneName}'.");
            _isLoading = false;
            yield return StartCoroutine(RevealWorld());
            UnfreezePlayer();
            yield break;
        }

        load.allowSceneActivation = false;
        while (load.progress < 0.9f)
        {
            if (loadingText != null)
                loadingText.text = $"Loading Experiment… {Mathf.RoundToInt(load.progress / 0.9f * 100f)}%";
            yield return null;
        }

        load.allowSceneActivation = true;
        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!_loadedScene.IsValid() || !_loadedScene.isLoaded)
        {
            Debug.LogError($"[ExperimentLoader] Scene '{sceneName}' did not load correctly.");
            _currentSceneName = "";
            _isLoading = false;
            yield return StartCoroutine(RevealWorld());
            UnfreezePlayer();
            yield break;
        }

        _experimentSceneLoaded = true;
        SceneManager.SetActiveScene(_loadedScene);
        DynamicGI.UpdateEnvironment();
        SetMainSceneVisible(false);

        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);
        if (spawnPoint != null && xrOrigin != null)
            TeleportPlayerToTransform(spawnPoint.transform);

        PlaceLoadingScreenInFront();

        // Lock continuous movement, but allow teleport until
        // Begin Experiment is pressed.
        LocomotionLocker.Lock(LocomotionLocker.LockMode.MoveOnly, xrOrigin);

        // Update Firestore before starting the video publisher (with timeout so offline does not hang).
        if (loadingText != null)
            loadingText.text = "Updating session status…";

        Task statusTask = UpdateDeviceStatus("In Use", sceneName);
        yield return StartCoroutine(WaitForTaskWithTimeout(
            statusTask, statusUpdateTimeout, "Updating session status…"));

        bool statusOk = statusTask.IsCompleted && !statusTask.IsFaulted && !statusTask.IsCanceled;
        if (!statusOk)
        {
            Debug.LogWarning("[ExperimentLoader] Firestore status update failed or timed out. Continuing without live streaming.");
            if (loadingText != null)
                loadingText.text = "No connection — continuing offline…";
            yield return new WaitForSecondsRealtime(1f);
        }
        else
        {
            // Start the LiveKit publisher only after the status update succeeds.
            if (livePublisher != null)
                livePublisher.StartStreaming();
            else
                Debug.LogWarning("[ExperimentLoader] LiveKit publisher is not assigned. The experiment will load without live streaming.");
        }

        // Wait until the experiment scene says it is ready (with a safety timeout).
        float waited = 0f;
        while (!SceneReadySignal.IsReady && waited < readyTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(1f);
        yield return StartCoroutine(RevealWorld());
        UnfreezePlayer();
        _isLoading = false;
    }

    private IEnumerator UnloadExperimentScene(GameObject respawnObject)
    {
        _isLoading = true;

        // Only freeze if returning from Memory Scene 2
        FreezePlayer();

        if (loadingText != null)
            loadingText.text = "Returning…";

        yield return StartCoroutine(EnterLoadingScreen());

        // Stop the LiveKit publisher before unloading the experiment.
        if (livePublisher != null)
            livePublisher.StopStreaming();

        Scene mainScene = SceneManager.GetSceneAt(0);
        SceneManager.SetActiveScene(mainScene);

        DynamicGI.UpdateEnvironment();
        SetMainSceneVisible(true);

        if (_loadedScene.IsValid() && _loadedScene.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(_loadedScene);

            if (unload != null)
                yield return new WaitUntil(() => unload.isDone);
        }

        _experimentSceneLoaded = false;
        _currentSceneName = "";

        yield return null;

        if (xrOrigin != null && respawnObject != null)
        {
            TeleportPlayerToTransform(respawnObject.transform);
            PlaceLoadingScreenInFront();          // keep the canvas with the player
        }

        // Mark the device as available in Firestore (timeout so offline return to hub does not hang).
        if (loadingText != null)
            loadingText.text = "Updating session status…";

        Task statusTask = UpdateDeviceStatus("Available", "");
        yield return StartCoroutine(WaitForTaskWithTimeout(
            statusTask, statusUpdateTimeout, "Updating session status…"));

        if (statusTask.IsFaulted || statusTask.IsCanceled || !statusTask.IsCompleted)
            Debug.LogWarning("[ExperimentLoader] Failed or timed out marking the VR device as Available.");

        // Make sure locomotion is free again in the hub.
        LocomotionLocker.Unlock();

        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(RevealWorld());
        UnfreezePlayer();

        _isLoading = false;

        if (_hasPendingSummary)
            ShowPendingSummary();
    }

    // ------------------------------------------------------------------
    // Player freeze (Memory Scene 2 only)
    // ------------------------------------------------------------------

    private void FreezePlayer()
    {
        // Only apply freeze behavior if the target/current scene is "Memory Scene 2"
        if (_currentSceneName != "Memory Scene 2") return;

        if (_playerFrozen || xrOrigin == null) return;

        _frozenCC = xrOrigin.GetComponentInChildren<CharacterController>();
        _frozenCCWasEnabled = _frozenCC != null && _frozenCC.enabled;

        // Found by name so this doesn't depend on the XRI namespace.
        _frozenGravity = null;
        foreach (MonoBehaviour mb in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "GravityProvider")
            {
                _frozenGravity = mb;
                break;
            }
        }
        _frozenGravityWasEnabled = _frozenGravity != null && _frozenGravity.enabled;

        if (_frozenGravity != null) _frozenGravity.enabled = false;
        if (_frozenCC != null) _frozenCC.enabled = false;

        _playerFrozen = true;
    }

    private void UnfreezePlayer()
    {
        if (_currentSceneName != "Memory Scene 2") return;
        if (!_playerFrozen) return;

        _playerFrozen = false;

        if (_frozenCC != null) _frozenCC.enabled = _frozenCCWasEnabled;
        if (_frozenGravity != null) _frozenGravity.enabled = _frozenGravityWasEnabled;
    }

    // ------------------------------------------------------------------
    // Player teleport
    // ------------------------------------------------------------------

    private void TeleportPlayerToTransform(Transform targetTransform)
    {
        if (xrOrigin == null || targetTransform == null)
            return;

        XROrigin origin = xrOrigin.GetComponent<XROrigin>();

        if (origin == null)
            return;

        CharacterController cc = xrOrigin.GetComponentInChildren<CharacterController>();

        bool controllerWasEnabled = cc != null && cc.enabled;

        if (cc != null)
            cc.enabled = false;

        Vector3 forward = Vector3.ProjectOnPlane(
            targetTransform.forward,
            Vector3.up
        ).normalized;

        if (forward.sqrMagnitude > 0.001f)
            origin.MatchOriginUpCameraForward(Vector3.up, forward);

        origin.MoveCameraToWorldLocation(targetTransform.position);

        StartCoroutine(RestoreCharacterController(cc, controllerWasEnabled));
    }

    private IEnumerator RestoreCharacterController(CharacterController cc, bool wasEnabled)
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.25f, controllerReenableDelay)
        );

        // While frozen, UnfreezePlayer() owns the CharacterController.
        if (_playerFrozen) yield break;

        if (cc != null)
            cc.enabled = wasEnabled;
    }

    // ------------------------------------------------------------------
    // Firebase
    // ------------------------------------------------------------------

    private async Task UpdateDeviceStatus(string status, string experimentSceneName)
    {
        if (!FirebaseManager.IsInitialized || string.IsNullOrWhiteSpace(vrDeviceDocumentId))
            throw new System.InvalidOperationException("[ExperimentLoader] VR publisher is not registered.");

        bool inUse = status == "In Use";
        if (inUse && (config == null || string.IsNullOrWhiteSpace(config.experimentId) ||
                      string.IsNullOrWhiteSpace(config.progressId) ||
                      string.IsNullOrWhiteSpace(config.blockId)))
            throw new System.InvalidOperationException("[ExperimentLoader] Missing active session IDs.");

        // Server-side callable validates the anonymous publisher UID, the pinned
        // student session, and the course. No anonymous Firestore writes are needed.
        var payload = new Dictionary<string, object>
        {
            { "vrDeviceId", vrDeviceDocumentId },
            { "status", status },
            { "experimentId", inUse ? config.experimentId : "" },
            { "progressId", inUse ? config.progressId : "" },
            { "blockId", inUse ? config.blockId : "" },
            { "sceneName", inUse ? experimentSceneName : "" },
        };

        try
        {
            var callable = FirebaseFunctions.DefaultInstance
                .GetHttpsCallable("setVrExperimentSceneStatus");
            await callable.CallAsync(payload);
            Debug.Log($"[ExperimentLoader] Server updated VR status to '{status}'.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ExperimentLoader] Failed to update VR status: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Waits for a Task up to timeoutSeconds. Updates loading text while waiting.
    /// Returns when the task completes or the timeout elapses (does not cancel the task).
    /// </summary>
    private IEnumerator WaitForTaskWithTimeout(Task task, float timeoutSeconds, string statusMessage)
    {
        if (task == null)
            yield break;

        float waited = 0f;
        float timeout = Mathf.Max(0.5f, timeoutSeconds);

        while (!task.IsCompleted && waited < timeout)
        {
            if (loadingText != null && !string.IsNullOrEmpty(statusMessage))
                loadingText.text = $"{statusMessage} ({Mathf.CeilToInt(timeout - waited)}s)";
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!task.IsCompleted)
            Debug.LogWarning($"[ExperimentLoader] Task timed out after {timeout:F0}s ({statusMessage}).");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void SetMainSceneVisible(bool visible)
    {
        if (mainSceneObjects == null)
            return;

        foreach (GameObject obj in mainSceneObjects)
        {
            if (obj != null)
                obj.SetActive(visible);
        }
    }

    private GameObject FindSpawnPointInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Spawn Point")
                return root;

            Transform found = root.transform.Find("Spawn Point");

            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Places the loading screen in world space and keeps it stationary.
    /// </summary>
    private void PlaceLoadingScreenInFront()
    {
        if (fadeCanvas == null)
            return;

        // Force unparent so it never follows the head.
        if (fadeCanvas.transform.parent != null)
            fadeCanvas.transform.SetParent(null);

        Canvas canvas = fadeCanvas.GetComponent<Canvas>();

        if (canvas != null)
        {
            // Only touch these when they differ; setting them every time forces canvas rebuilds.
            if (canvas.renderMode != RenderMode.WorldSpace) canvas.renderMode = RenderMode.WorldSpace;
            if (canvas.worldCamera != null) canvas.worldCamera = null;
            if (canvas.sortingOrder != 999) canvas.sortingOrder = 999;
        }

        Transform cam = xrCamera != null
            ? xrCamera.transform
            : (Camera.main != null ? Camera.main.transform : null);

        if (cam == null)
        {
            GameObject found =
                GameObject.Find("Main Camera") ??
                GameObject.Find("CenterEyeAnchor");

            if (found != null)
                cam = found.transform;
        }

        if (cam == null)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        Vector3 pos = cam.position + forward * loadingScreenDistance;
        pos.y = cam.position.y;

        fadeCanvas.transform.position = pos;
        fadeCanvas.transform.rotation = Quaternion.LookRotation(pos - cam.position);

        // Adjust the scale here if the loading screen is too large.
        // fadeCanvas.transform.localScale = Vector3.one * 0.01f;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null)
            yield break;

        // Place it only when it appears. Re-placing on fade-out made the logo
        // jump to wherever the player was now looking.
        if (from <= 0.01f)
            PlaceLoadingScreenInFront();

        fadeCanvas.gameObject.SetActive(true);
        fadeCanvas.blocksRaycasts = true;

        if (logoImage != null)
            logoImage.enabled = true;

        if (loadingText != null)
            loadingText.enabled = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);

            yield return null;
        }

        fadeCanvas.alpha = to;

        if (to <= 0.01f)
        {
            fadeCanvas.blocksRaycasts = false;
            fadeCanvas.gameObject.SetActive(false);

            if (logoImage != null)
                logoImage.enabled = false;

            if (loadingText != null)
                loadingText.enabled = false;
        }
    }
}