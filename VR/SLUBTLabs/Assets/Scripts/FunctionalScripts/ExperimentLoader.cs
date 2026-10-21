using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Firestore;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Management;

/// <summary>
/// SLUBT Labs — Dynamic Experiment Loader
/// - Proper loading screen with logo (no blue/green flash)
/// - Additive scene loading
/// - Teleports XR Origin to "Spawn Point"
/// - Updates Firebase vrDevices status
/// </summary>
public class ExperimentLoader : MonoBehaviour
{
    [Header("Player")]
    public GameObject xrOrigin;

    [Header("Spawn Position Settings")]
    [Tooltip("Leave OFF for room-scale (Spawn Point = feet). ON was causing mid-air spawn.")]
    public bool useFixedSpawnHeight = false;
    public float fixedSpawnHeight = 1.6f;
    public bool snapToFloor = true;
    public float floorSnapDistance = 3f;

    [Header("Main Scene Visibility")]
    public GameObject[] mainSceneObjects;

    [Header("Transition / Loading Screen")]
    public CanvasGroup fadeCanvas;
    [Tooltip("Optional logo that appears while the screen is black")]
    public Image logoImage;
    [Tooltip("Optional loading text")]
    public TMPro.TextMeshProUGUI loadingText;
    public float fadeDuration = 0.5f;

    [Header("Firebase")]
    public ExperimentConfig config;
    public string vrDeviceDocumentId = "";

    [Header("Teleport Stabilization")]
    public float controllerReenableDelay = 0.25f;

    private bool _isLoading = false;
    private Scene _loadedScene;
    private bool _experimentSceneLoaded = false;
    private string _currentSceneName = "";
    private FirebaseFirestore _db;

    // ────────────────────────────────────────────────
    //  INITIAL BOOT – kill the blue/green flash
    // ────────────────────────────────────────────────
    private void Awake()
    {
        // Force the loading screen ON from the very first frame
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 1f;
            fadeCanvas.blocksRaycasts = true;
            fadeCanvas.interactable = false;
        }

        if (logoImage != null) logoImage.enabled = true;
        if (loadingText != null)
        {
            loadingText.enabled = true;
            loadingText.text = "Loading…";
        }
    }

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        float xrWaitTime = 0f;
        while (xrWaitTime < 3f && (XRGeneralSettings.Instance == null ||
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

        // Keep the screen black while everything initializes
        yield return new WaitForSecondsRealtime(0.5f);

        // CRITICAL: Wait for end of frame so Unity finishes its initial rendering pass
        yield return new WaitForEndOfFrame();

        // Now fade out smoothly
        yield return StartCoroutine(Fade(1f, 0f));

        // Firebase
        if (FirebaseManager.IsInitialized)
            _db = FirebaseFirestore.DefaultInstance;
        else
            StartCoroutine(WaitForFirebaseInit());
    }

    private IEnumerator WaitForFirebaseInit()
    {
        yield return new WaitUntil(() => FirebaseManager.IsInitialized);
        _db = FirebaseFirestore.DefaultInstance;
        Debug.Log("[ExperimentLoader] Firebase ready.");
    }

    // ────────────────────────────────────────────────
    //  PUBLIC API
    // ────────────────────────────────────────────────
    public void LoadScene(string sceneName)
    {
        if (_isLoading) return;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SLUBT Labs] Scene name is empty!");
            return;
        }
        StartCoroutine(LoadExperimentScene(sceneName));
    }

    public void ReturnToHub()
    {
        if (!_experimentSceneLoaded) return;

        GameObject respawnObject = GameObject.Find("Respawn");
        if (respawnObject == null)
            Debug.LogError("[SLUBT Labs] CRITICAL: 'Respawn' not found in Main VR Scene!");

        StartCoroutine(UnloadExperimentScene(respawnObject));
    }

    // Keep the old signature so existing buttons still work
    public void ReturnToHub(Vector3 unused) => ReturnToHub();

    // ────────────────────────────────────────────────
    //  LOAD EXPERIMENT
    // ────────────────────────────────────────────────
    private IEnumerator LoadExperimentScene(string sceneName)
    {
        _isLoading = true;
        _currentSceneName = sceneName;

        // 1. Fade to black + show logo
        if (loadingText != null) loadingText.text = "Loading Experiment…";
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. Load additively
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        // Extra safety for XR
        yield return new WaitForSecondsRealtime(0.1f);
        load.allowSceneActivation = true;

        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);
        _experimentSceneLoaded = true;
        SceneManager.SetActiveScene(_loadedScene);

        SetMainSceneVisible(false);

        // 3. Find Spawn Point & teleport
        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);
        if (spawnPoint == null)
        {
            Debug.LogError($"[SLUBT Labs] No 'Spawn Point' found in '{sceneName}'. " +
                           "Create an empty GameObject named exactly 'Spawn Point' on the floor.");
            _isLoading = false;
            yield break;
        }

        if (xrOrigin != null)
            TeleportPlayerToTransform(spawnPoint.transform);
        else
            Debug.LogWarning("[SLUBT Labs] xrOrigin not assigned.");

        UpdateDeviceStatus("In Use", sceneName);

        // After the scene is activated and player is teleported:
        yield return null;
        yield return null;

        // Add this extra wait:
        yield return new WaitForSecondsRealtime(0.8f);  

        // Then fade out
        yield return StartCoroutine(Fade(1f, 0f));
    }

    // ────────────────────────────────────────────────
    //  RETURN TO HUB
    // ────────────────────────────────────────────────
    private IEnumerator UnloadExperimentScene(GameObject respawnObject)
    {
        _isLoading = true;

        if (loadingText != null) loadingText.text = "Returning…";
        yield return StartCoroutine(Fade(0f, 1f));

        Scene mainScene = SceneManager.GetSceneAt(0);
        SceneManager.SetActiveScene(mainScene);
        DynamicGI.UpdateEnvironment();
        SetMainSceneVisible(true);

        AsyncOperation unload = SceneManager.UnloadSceneAsync(_loadedScene);
        yield return new WaitUntil(() => unload.isDone);

        _experimentSceneLoaded = false;
        _currentSceneName = "";

        yield return null;

        if (xrOrigin != null && respawnObject != null)
        {
            TeleportPlayerToTransform(respawnObject.transform);
            Debug.Log($"[SLUBT Labs] Returned to hub at {respawnObject.transform.position}");
        }

        UpdateDeviceStatus("Available", "");

        yield return StartCoroutine(Fade(1f, 0f));
        _isLoading = false;
    }

    // ────────────────────────────────────────────────
    //  TELEPORT
    // ────────────────────────────────────────────────
    private void TeleportPlayerToTransform(Transform targetTransform)
    {
        if (xrOrigin == null || targetTransform == null)
        {
            Debug.LogError("[ExperimentLoader] XR Origin or target missing.");
            return;
        }

        XROrigin origin = xrOrigin.GetComponent<XROrigin>();
        if (origin == null)
        {
            Debug.LogError("[ExperimentLoader] XROrigin component not found.");
            return;
        }

        Camera vrCam = origin.Camera;
        if (vrCam == null)
            vrCam = xrOrigin.GetComponentInChildren<Camera>();

        if (vrCam == null)
        {
            Debug.LogError("[ExperimentLoader] VR camera not found.");
            return;
        }

        CharacterController cc = xrOrigin.GetComponentInChildren<CharacterController>();
        bool controllerWasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        // Face the same direction as the Spawn Point
        Vector3 forward = Vector3.ProjectOnPlane(targetTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude > 0.001f)
            origin.MatchOriginUpCameraForward(Vector3.up, forward);

        // Move camera exactly to the Spawn Point
        origin.MoveCameraToWorldLocation(targetTransform.position);

        StartCoroutine(RestoreCharacterController(cc, controllerWasEnabled));

        Debug.Log($"[ExperimentLoader] Teleported. Target={targetTransform.position}, Camera={vrCam.transform.position}");
    }

    private IEnumerator RestoreCharacterController(CharacterController cc, bool wasEnabled)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, controllerReenableDelay));
        if (cc != null) cc.enabled = wasEnabled;
    }

    // ────────────────────────────────────────────────
    //  FIREBASE
    // ────────────────────────────────────────────────
    private async void UpdateDeviceStatus(string status, string experimentSceneName)
    {
        if (_db == null || string.IsNullOrEmpty(vrDeviceDocumentId))
        {
            Debug.LogWarning("[ExperimentLoader] Firebase not ready or document ID missing.");
            return;
        }

        string studentId = config != null ? config.studentId : "";
        string groupId = config != null ? config.groupId : "";

        var update = new Dictionary<string, object>
        {
            { "Status", status },
            { "currentExperimentId", experimentSceneName },
            { "currentUserId", studentId },
            { "currentGroupId", groupId },
            { "currentSessionStart", FieldValue.ServerTimestamp }
        };

        try
        {
            await _db.Collection("vrDevices").Document(vrDeviceDocumentId).UpdateAsync(update);
            Debug.Log($"[ExperimentLoader] Status → {status}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ExperimentLoader] Firebase error (non-critical): {e.Message}");
        }
    }

    // ────────────────────────────────────────────────
    //  HELPERS
    // ────────────────────────────────────────────────
    private void SetMainSceneVisible(bool visible)
    {
        if (mainSceneObjects == null) return;
        foreach (GameObject obj in mainSceneObjects)
            if (obj != null) obj.SetActive(visible);
    }

    private GameObject FindSpawnPointInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Spawn Point") return root;
            Transform found = root.transform.Find("Spawn Point");
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null) yield break;

        fadeCanvas.gameObject.SetActive(true);
        fadeCanvas.blocksRaycasts = true;

        // Show logo while black
        if (logoImage != null) logoImage.enabled = true;
        if (loadingText != null) loadingText.enabled = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = to;

        if (to <= 0.01f)
        {
            fadeCanvas.blocksRaycasts = false;
            fadeCanvas.gameObject.SetActive(false);
            if (logoImage != null) logoImage.enabled = false;
            if (loadingText != null) loadingText.enabled = false;
        }
    }
}