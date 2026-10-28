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

    [Header("Firebase")]
    public ExperimentConfig config;
    public string vrDeviceDocumentId = "";

    [Header("Live Streaming")]
    public LiveKitVrPublisher livePublisher;

    [Header("Teleport Stabilization")]
    public float controllerReenableDelay = 0.25f;

    private bool _isLoading = false;
    private Scene _loadedScene;
    private bool _experimentSceneLoaded = false;
    private string _currentSceneName = "";

    private void Awake()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 1f;
            fadeCanvas.blocksRaycasts = true;
            fadeCanvas.interactable = false;
        }

        if (logoImage != null)
            logoImage.enabled = true;

        if (loadingText != null)
        {
            loadingText.enabled = true;
            loadingText.text = "Initializing VR & Connecting...";
        }

        // Keep the loading screen stationary in world space.
        PlaceLoadingScreenInFront();
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

        if (loadingText != null)
            loadingText.text = "Connecting to Database...";

        if (!FirebaseManager.IsInitialized)
        {
            yield return new WaitUntil(() => FirebaseManager.IsInitialized || FirebaseManager.InitializationFailed);
        }
        if (FirebaseManager.InitializationFailed)
        {
            Debug.LogError("[ExperimentLoader] Firebase startup or VR registration failed.");
            if (loadingText != null) loadingText.text = "Cannot register VR headset.";
            yield break;
        }
        // No inspector pairing: use the document ID verified during startup.
        vrDeviceDocumentId = FirebaseManager.RegisteredDeviceDocumentId;

        if (loadingText != null)
            loadingText.text = "Loading Experiment Configuration...";

        yield return new WaitUntil(() =>
            ExperimentConfigLoader.IsReady &&
            ExperimentConfigLoader.Current != null);

        config = ExperimentConfigLoader.Current;

        if (loadingText != null)
            loadingText.text = "Ready!";

        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitForEndOfFrame();

        yield return StartCoroutine(Fade(1f, 0f));
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading || string.IsNullOrEmpty(sceneName))
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

    private IEnumerator LoadExperimentScene(string sceneName)
    {
        _isLoading = true;
        _currentSceneName = sceneName;

        if (loadingText != null)
            loadingText.text = "Loading Experiment…";

        yield return StartCoroutine(Fade(0f, 1f));

        AsyncOperation load =
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (load == null)
        {
            Debug.LogError(
                $"[ExperimentLoader] Could not begin loading scene '{sceneName}'."
            );

            _isLoading = false;
            yield return StartCoroutine(Fade(1f, 0f));
            yield break;
        }

        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        yield return new WaitForSecondsRealtime(0.1f);

        load.allowSceneActivation = true;

        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);

        if (!_loadedScene.IsValid() || !_loadedScene.isLoaded)
        {
            Debug.LogError($"[ExperimentLoader] Scene '{sceneName}' did not load correctly.");

            _currentSceneName = "";
            _isLoading = false;

            yield return StartCoroutine(Fade(1f, 0f));
            yield break;
        }

        _experimentSceneLoaded = true;

        SceneManager.SetActiveScene(_loadedScene);
        SetMainSceneVisible(false);

        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);

        if (spawnPoint != null && xrOrigin != null)
        {
            TeleportPlayerToTransform(spawnPoint.transform);
        }

        // Lock continuous movement, but allow teleport until
        // Begin Experiment is pressed.
        LocomotionLocker.Lock(
            LocomotionLocker.LockMode.MoveOnly,
            xrOrigin
        );

        // Update Firestore before starting the video publisher.
        Task statusTask = UpdateDeviceStatus("In Use", sceneName);

        yield return new WaitUntil(() => statusTask.IsCompleted);

        if (statusTask.IsFaulted || statusTask.IsCanceled)
        {
            Debug.LogError("[ExperimentLoader] Firestore status update failed. Live streaming will not start.");

            _isLoading = false;
            yield return StartCoroutine(Fade(1f, 0f));
            yield break;
        }

        // Start the LiveKit publisher only after the status update succeeds.
        if (livePublisher != null)
        {
            livePublisher.StartStreaming();
        }
        else
        {
            Debug.LogWarning("[ExperimentLoader] LiveKit publisher is not assigned. The experiment will load without live streaming.");
        }

        yield return new WaitForSecondsRealtime(0.8f);
        yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    private IEnumerator UnloadExperimentScene(GameObject respawnObject)
    {
        _isLoading = true;

        if (loadingText != null)
            loadingText.text = "Returning…";

        yield return StartCoroutine(Fade(0f, 1f));

        // Stop the LiveKit publisher before unloading the experiment.
        if (livePublisher != null)
        {
            livePublisher.StopStreaming();
        }

        Scene mainScene = SceneManager.GetSceneAt(0);

        SceneManager.SetActiveScene(mainScene);

        DynamicGI.UpdateEnvironment();
        SetMainSceneVisible(true);

        if (_loadedScene.IsValid() && _loadedScene.isLoaded)
        {
            AsyncOperation unload =
                SceneManager.UnloadSceneAsync(_loadedScene);

            if (unload != null)
                yield return new WaitUntil(() => unload.isDone);
        }

        _experimentSceneLoaded = false;
        _currentSceneName = "";

        yield return null;

        if (xrOrigin != null && respawnObject != null)
        {
            TeleportPlayerToTransform(respawnObject.transform);
        }

        // Mark the device as available in Firestore.
        Task statusTask = UpdateDeviceStatus("Available", "");

        yield return new WaitUntil(() => statusTask.IsCompleted);

        if (statusTask.IsFaulted || statusTask.IsCanceled)
        {
            Debug.LogError("[ExperimentLoader] Failed to mark the VR device as Available.");
        }

        // Make sure locomotion is free again in the hub.
        LocomotionLocker.Unlock();

        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    private void TeleportPlayerToTransform(Transform targetTransform)
    {
        if (xrOrigin == null || targetTransform == null)
            return;

        XROrigin origin = xrOrigin.GetComponent<XROrigin>();

        if (origin == null)
            return;

        CharacterController cc =
            xrOrigin.GetComponentInChildren<CharacterController>();

        bool controllerWasEnabled = cc != null && cc.enabled;

        if (cc != null)
            cc.enabled = false;

        Vector3 forward = Vector3.ProjectOnPlane(
            targetTransform.forward,
            Vector3.up
        ).normalized;

        if (forward.sqrMagnitude > 0.001f)
        {
            origin.MatchOriginUpCameraForward(Vector3.up, forward);
        }

        origin.MoveCameraToWorldLocation(targetTransform.position);

        StartCoroutine(
            RestoreCharacterController(cc, controllerWasEnabled)
        );
    }

    private IEnumerator RestoreCharacterController(
        CharacterController cc,
        bool wasEnabled)
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.25f, controllerReenableDelay)
        );

        if (cc != null)
            cc.enabled = wasEnabled;
    }

    private async Task UpdateDeviceStatus(
        string status,
        string experimentSceneName)
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
        fadeCanvas.transform.SetParent(null);

        Canvas canvas = fadeCanvas.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvas.sortingOrder = 999;
        }

        Transform cam = Camera.main != null
            ? Camera.main.transform
            : null;

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

        Vector3 forward = Vector3.ProjectOnPlane(
            cam.forward,
            Vector3.up
        ).normalized;

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        Vector3 pos = cam.position + forward * loadingScreenDistance;
        pos.y = cam.position.y;

        fadeCanvas.transform.position = pos;
        fadeCanvas.transform.rotation =
            Quaternion.LookRotation(pos - cam.position);

        // Adjust the scale here if the loading screen is too large.
        // fadeCanvas.transform.localScale = Vector3.one * 0.01f;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null)
            yield break;

        // Place it in front of the player each time it appears.
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
            elapsed += Time.unscaledDeltaTime;

            fadeCanvas.alpha = Mathf.Lerp(
                from,
                to,
                elapsed / fadeDuration
            );

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
