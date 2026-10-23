using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Firestore;
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

    private void Awake()
    {
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
            loadingText.text = "Initializing VR & Connecting...";
        }
    }

    private IEnumerator Start()
    {
        // 1. Wait for XR Initialization
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

        // 2. Wait for Firebase Initialization
        if (loadingText != null) loadingText.text = "Connecting to Database...";
        if (!FirebaseManager.IsInitialized)
        {
            yield return new WaitUntil(() => FirebaseManager.IsInitialized);
        }
        _db = FirebaseFirestore.DefaultInstance;

        // 3. Wait for the Experiment Configuration to be Ready
        if (loadingText != null) loadingText.text = "Loading Experiment Configuration...";

        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady && ExperimentConfigLoader.Current != null);

        // Grab the loaded config reference
        config = ExperimentConfigLoader.Current;

        if (loadingText != null) loadingText.text = "Ready!";
        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitForEndOfFrame();

        // 4. Fade out the loading screen, letting the player see the main room & anchors
        yield return StartCoroutine(Fade(1f, 0f));
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(LoadExperimentScene(sceneName));
    }

    public void ReturnToHub()
    {
        if (!_experimentSceneLoaded) return;
        GameObject respawnObject = GameObject.Find("Respawn");
        StartCoroutine(UnloadExperimentScene(respawnObject));
    }

    public void ReturnToHub(Vector3 unused) => ReturnToHub();

    private IEnumerator LoadExperimentScene(string sceneName)
    {
        _isLoading = true;
        _currentSceneName = sceneName;

        if (loadingText != null) loadingText.text = "Loading Experiment…";

        yield return StartCoroutine(Fade(0f, 1f));

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        yield return new WaitForSecondsRealtime(0.1f);

        load.allowSceneActivation = true;
        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);
        _experimentSceneLoaded = true;

        SceneManager.SetActiveScene(_loadedScene);
        SetMainSceneVisible(false);

        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);
        if (spawnPoint != null && xrOrigin != null)
        {
            TeleportPlayerToTransform(spawnPoint.transform);
        }

        UpdateDeviceStatus("In Use", sceneName);

        yield return new WaitForSecondsRealtime(0.8f);
        yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

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
        }

        UpdateDeviceStatus("Available", "");

        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    private void TeleportPlayerToTransform(Transform targetTransform)
    {
        if (xrOrigin == null || targetTransform == null) return;

        XROrigin origin = xrOrigin.GetComponent<XROrigin>();
        if (origin == null) return;

        CharacterController cc = xrOrigin.GetComponentInChildren<CharacterController>();
        bool controllerWasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        Vector3 forward = Vector3.ProjectOnPlane(targetTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude > 0.001f)
            origin.MatchOriginUpCameraForward(Vector3.up, forward);

        origin.MoveCameraToWorldLocation(targetTransform.position);

        StartCoroutine(RestoreCharacterController(cc, controllerWasEnabled));
    }

    private IEnumerator RestoreCharacterController(CharacterController cc, bool wasEnabled)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, controllerReenableDelay));
        if (cc != null) cc.enabled = wasEnabled;
    }

    private async void UpdateDeviceStatus(string status, string experimentSceneName)
    {
        if (_db == null || string.IsNullOrEmpty(vrDeviceDocumentId)) return;

        string studentId = config != null ? config.studentId : "";
        string groupId = config != null ? config.groupId : "";

        // --- Added Debug checks for null or empty values ---
        if (string.IsNullOrEmpty(groupId))
        {
            Debug.LogWarning("[ExperimentLoader] Warning: groupId is null or empty when updating device status!");
        }
        else
        {
            Debug.Log($"[ExperimentLoader] groupId is valid: '{groupId}'");
        }

        if (string.IsNullOrEmpty(studentId))
        {
            Debug.LogWarning("[ExperimentLoader] Warning: studentId is null or empty when updating device status!");
        }
        // ---------------------------------------------------

        var update = new Dictionary<string, object>
        {
            { "status", status },
            { "currentExperimentId", experimentSceneName },
            { "currentUserId", studentId },
            { "currentGroupId", groupId },
            { "currentSessionStart", FieldValue.ServerTimestamp }
        };

        try
        {
            await _db.Collection("vrDevices").Document(vrDeviceDocumentId).UpdateAsync(update);
            Debug.Log("[ExperimentLoader] Successfully updated device status in Firebase.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ExperimentLoader] Failed to update device status: {ex.Message}");
        }
    }

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

        if (logoImage != null) logoImage.enabled = true;
        if (loadingText != null) loadingText.text = loadingText.text;

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