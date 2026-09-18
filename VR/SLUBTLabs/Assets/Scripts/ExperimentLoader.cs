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

    /// <summary>
    /// Dynamically loads any scene name passed to it from an individual teleport anchor pad.
    /// </summary>
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
        //DynamicGI.UpdateEnvironment();

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

        // 4. Move XR Origin to the dynamic spawn point
        if (xrOrigin != null)
        {
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            float camOffsetY = cameraOffset != null ? cameraOffset.localPosition.y : 0f;

            Vector3 targetPos = spawnPoint.transform.position;
            targetPos.y -= camOffsetY;

            xrOrigin.transform.position = targetPos;
            xrOrigin.transform.rotation = spawnPoint.transform.rotation;
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

    /// <summary>
    /// Legacy fallback method signature wrapper to handle any calling script still passing a Vector3.
    /// </summary>
    public void ReturnToHub(Vector3 hubSpawnPosition)
    {
        ReturnToHub();
    }

    /// <summary>
    /// Clean parameterless method that locates the "Respawn" tracking target directly in the Main VR Scene.
    /// </summary>
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

        // Wait one frame for XR tracking to settle after scene unload
        yield return null;

        // Apply tracking offset compensation so camera lands exactly on Respawn
        if (xrOrigin != null && respawnObject != null)
        {
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            float camOffsetY = cameraOffset != null ? cameraOffset.localPosition.y : 0f;

            Vector3 targetPos = respawnObject.transform.position;
            targetPos.y -= camOffsetY;

            xrOrigin.transform.position = targetPos;
            xrOrigin.transform.rotation = respawnObject.transform.rotation;

            Debug.Log($"[SLUBT Labs] Returned to hub at {targetPos}");
        }

        // Update Firebase device status back to Available
        UpdateDeviceStatus("Available", "");

        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    // ── Firebase ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the vrDevices document in Firestore with current headset status.
    /// Called when loading an experiment (In Use) and returning to hub (Available).
    /// </summary>
    private void UpdateDeviceStatus(string status, string experimentSceneName)
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

        _db.Collection("vrDevices")
           .Document(vrDeviceDocumentId)
           .UpdateAsync(update)
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsFaulted)
                   Debug.LogError($"[ExperimentLoader] Failed to update vrDevices: {task.Exception}");
               else
                   Debug.Log($"[ExperimentLoader] vrDevices status updated to '{status}'.");
           });
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