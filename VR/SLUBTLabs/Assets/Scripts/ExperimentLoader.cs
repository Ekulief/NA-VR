using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// SLUBT Labs — Dynamic Experiment Loader
/// Keep this script attached to your central manager object.
/// Updates vrDevices status in Firebase when loading/unloading experiments.
/// </summary>
public class ExperimentLoader : MonoBehaviour
{
    [Header("Player")]
    public GameObject xrOrigin;

    [Header("Spawn Position Settings")]
    [Tooltip("If checked, forces the player's camera height to fixedSpawnHeight. If unchecked, uses Spawn Point's Y position.")]
    public bool useFixedSpawnHeight = true;
    [Tooltip("Target eye height relative to floor (in meters). Standard average standing height is ~1.6m to 1.7m.")]
    public float fixedSpawnHeight = 1.6f;

    [Header("Main Scene Visibility")]
    public GameObject[] mainSceneObjects;

    [Header("Transition")]
    public CanvasGroup fadeCanvas;

    [Tooltip("Fade duration in seconds.")]
    public float fadeDuration = 0.4f;

    [Header("Firebase")]
    [Tooltip("Assign your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    [Tooltip("The Firestore document ID of this VR headset in the vrDevices collection.")]
    public string vrDeviceDocumentId = "";

    // State variables
    private bool _isLoading = false;
    private Scene _loadedScene;
    private bool _experimentSceneLoaded = false;
    private string _currentSceneName = "";
    private FirebaseFirestore _db;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
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

    // ── Scene loading ─────────────────────────────────────────────────────────

    public void LoadScene(string sceneName)
    {
        if (_isLoading) return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SLUBT Labs] Cannot load scene: The passed scene name is empty!");
            return;
        }

        StartCoroutine(LoadExperimentScene(sceneName));
    }

    private IEnumerator LoadExperimentScene(string sceneName)
    {
        _isLoading = true;
        _currentSceneName = sceneName;

        // 1. Fade out to loading screen
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f));

        // 2. Load the dynamic experiment scene additively 
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);
        _experimentSceneLoaded = true;

        SceneManager.SetActiveScene(_loadedScene);

        SetMainSceneVisible(false);

        // 3. Find the Spawn Point inside the newly loaded scene
        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);

        if (spawnPoint == null)
        {
            Debug.LogError($"[SLUBT Labs] No GameObject named 'Spawn Point' found in '{sceneName}'. " +
                           "Create an empty GameObject called Spawn Point and position it where the player should appear.");
            _isLoading = false;
            yield break;
        }

        // 4. Move XR Origin precisely accounting for room-scale physical offset
        if (xrOrigin != null)
        {
            TeleportPlayerToTransform(spawnPoint.transform);
        }
        else
        {
            Debug.LogWarning("[SLUBT Labs] xrOrigin is not assigned on Experiment Loader. Player won't be repositioned.");
        }

        // 5. Update Firebase device status to In Use
        UpdateDeviceStatus("In Use", sceneName);

        // 6. Fade back in
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    // ── Return to main scene ──────────────────────────────────────────────────

    public void ReturnToHub(Vector3 hubSpawnPosition)
    {
        ReturnToHub();
    }

    public void ReturnToHub()
    {
        if (!_experimentSceneLoaded) return;

        Scene hubScene = SceneManager.GetSceneAt(0);
        SceneManager.SetActiveScene(hubScene);

        GameObject respawnObject = GameObject.Find("Respawn");

        if (respawnObject == null)
            Debug.LogError("[SLUBT Labs] CRITICAL ERROR: Could not locate 'Respawn' in Main VR Scene!");

        StartCoroutine(UnloadExperimentScene(respawnObject));
    }

    private IEnumerator UnloadExperimentScene(GameObject respawnObject)
    {
        _isLoading = true;

        if (fadeCanvas != null)
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

        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    // ── Helper method for precise physical repositioning ─────────────────────

    private void TeleportPlayerToTransform(Transform targetTransform)
    {
        Camera mainCam = Camera.main;

        if (mainCam == null && xrOrigin != null)
            mainCam = xrOrigin.GetComponentInChildren<Camera>();

        if (mainCam != null)
        {
            // Rotate origin to match target rotation
            float currentCamYAngle = mainCam.transform.eulerAngles.y;
            float targetYAngle = targetTransform.eulerAngles.y;
            float rotationDelta = targetYAngle - currentCamYAngle;
            xrOrigin.transform.Rotate(0f, rotationDelta, 0f, Space.World);

            // Calculate room-scale horizontal offset from Camera to Origin
            Vector3 cameraPosition = mainCam.transform.position;
            Vector3 originPosition = xrOrigin.transform.position;

            Vector3 cameraOffsetHorizontal = cameraPosition - originPosition;
            cameraOffsetHorizontal.y = 0f;

            // Apply horizontal offset so the camera aligns with target spot
            Vector3 finalPosition = targetTransform.position - cameraOffsetHorizontal;

            // Height calculation
            if (useFixedSpawnHeight)
            {
                Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
                float localCamY = cameraOffset != null ? cameraOffset.localPosition.y : mainCam.transform.localPosition.y;
                finalPosition.y = targetTransform.position.y + fixedSpawnHeight - localCamY;
            }
            else
            {
                finalPosition.y = targetTransform.position.y;
            }

            xrOrigin.transform.position = finalPosition;
        }
        else
        {
            xrOrigin.transform.position = targetTransform.position;
            xrOrigin.transform.rotation = targetTransform.rotation;
        }
    }

    // ── Firebase ──────────────────────────────────────────────────────────────

    private async void UpdateDeviceStatus(string status, string experimentSceneName)
    {
        if (_db == null || string.IsNullOrEmpty(vrDeviceDocumentId))
        {
            Debug.LogWarning("[ExperimentLoader] Firebase not ready or vrDeviceDocumentId not set — skipping status update.");
            return;
        }

        string participantId = config != null ? config.participantId : "";
        string sessionId = config != null ? config.sessionId : "";

        Dictionary<string, object> update = new()
        {
            { "Status",               status },
            { "currentExperimentId",  experimentSceneName },
            { "currentUserId",        participantId },
            { "currentSessionStart",  FieldValue.ServerTimestamp },
            { "notes",                sessionId }
        };

        var docRef = _db.Collection("vrDevices").Document(vrDeviceDocumentId);

        try
        {
            await docRef.UpdateAsync(update);
            Debug.Log($"[ExperimentLoader] vrDevices status updated to '{status}'.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ExperimentLoader] Non-critical Firebase error: {e.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetMainSceneVisible(bool visible)
    {
        foreach (GameObject obj in mainSceneObjects)
            if (obj != null) obj.SetActive(visible);
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

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null) yield break;

        fadeCanvas.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = to;

        if (to <= 0f)
            fadeCanvas.gameObject.SetActive(false);
    }
}