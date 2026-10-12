using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Unity.XR.CoreUtils;

/// <summary>
/// SLUBT Labs — Dynamic Experiment Loader
/// Loads experiment scenes additively, teleports XR Origin to "Spawn Point",
/// updates vrDevices status in Firebase.
/// </summary>
public class ExperimentLoader : MonoBehaviour
{
    [Header("Player")]
    public GameObject xrOrigin;

    [Header("Spawn Position Settings")]
    [Tooltip("If true, raises the XR Origin so the camera sits at fixedSpawnHeight above the Spawn Point. " +
             "Leave OFF for room-scale XR (Spawn Point = feet/floor). ON is what caused the mid-air spawn + fall.")]
    public bool useFixedSpawnHeight = false;

    [Tooltip("Only used when useFixedSpawnHeight is true. Eye height in meters (~1.6).")]
    public float fixedSpawnHeight = 1.6f;

    [Tooltip("If true, raycasts down from the spawn to find the floor collider and places the origin there.")]
    public bool snapToFloor = true;

    [Tooltip("How far downward to search for a floor when snapToFloor is enabled.")]
    public float floorSnapDistance = 3f;

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

    [Header("Teleport Stabilization")]
    [Tooltip("Time to wait before re-enabling the CharacterController.")]
    public float controllerReenableDelay = 0.25f;
    private bool _isLoading = false;
    private Scene _loadedScene;
    private bool _experimentSceneLoaded = false;
    private string _currentSceneName = "";
    private FirebaseFirestore _db;

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

        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f));

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);
        _experimentSceneLoaded = true;
        SceneManager.SetActiveScene(_loadedScene);
        SetMainSceneVisible(false);

        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);
        if (spawnPoint == null)
        {
            Debug.LogError($"[SLUBT Labs] No GameObject named 'Spawn Point' found in '{sceneName}'. " +
                           "Create an empty GameObject called 'Spawn Point' on the FLOOR where the player should appear.");
            _isLoading = false;
            yield break;
        }

        if (xrOrigin != null)
            TeleportPlayerToTransform(spawnPoint.transform);
        else
            Debug.LogWarning("[SLUBT Labs] xrOrigin is not assigned on Experiment Loader. Player won't be repositioned.");

        UpdateDeviceStatus("In Use", sceneName);

        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

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

    /// <summary>
    /// Places XR Origin so the player stands at the target.
    /// Default (useFixedSpawnHeight = false): origin Y = spawn Y (spawn must be on the floor).
    /// Optional snapToFloor raycasts down to the collider under the spawn.
    /// </summary>
    private void TeleportPlayerToTransform(Transform targetTransform)
    {
        if (xrOrigin == null || targetTransform == null)
        {
            Debug.LogError("[ExperimentLoader] XR Origin or target is missing.");
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

        // Temporarily disable the CharacterController to avoid collision
        // interference while repositioning the XR Origin.
        CharacterController cc =
            xrOrigin.GetComponentInChildren<CharacterController>();

        bool controllerWasEnabled = cc != null && cc.enabled;

        if (cc != null)
            cc.enabled = false;

        // Match the spawn point's horizontal facing direction.
        Vector3 forward = Vector3.ProjectOnPlane(
            targetTransform.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude > 0.001f)
            origin.MatchOriginUpCameraForward(Vector3.up, forward);

        // Move the camera to the exact target world position,
        // regardless of its previous position or height.
        origin.MoveCameraToWorldLocation(targetTransform.position);

        // Restore the controller after the teleport has settled.
        StartCoroutine(RestoreCharacterController(
            cc, controllerWasEnabled));

        Debug.Log(
            $"[ExperimentLoader] Camera teleported to target. " +
            $"Target={targetTransform.position}, " +
            $"Camera={vrCam.transform.position}");
    }

    private IEnumerator RestoreCharacterController(
        CharacterController cc, bool wasEnabled)
    {
        // Wait briefly for XR tracking and transforms to settle.
        yield return new WaitForSecondsRealtime(0.3f);

        if (cc != null)
            cc.enabled = wasEnabled;
    }

    private IEnumerator RestorePlayerPhysics(
        CharacterController cc,
        Rigidbody rb,
        bool wasKinematic,
        bool usedGravity)
    {
        // Let the XR rig and tracking settle before restoring collision.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.1f, controllerReenableDelay));

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = usedGravity;
            rb.isKinematic = wasKinematic;
        }

        if (cc != null)
        {
            cc.enabled = true;
        }

        Debug.Log("[ExperimentLoader] Player physics restored after teleport.");
    }

    private async void UpdateDeviceStatus(string status, string experimentSceneName)
    {
        if (_db == null || string.IsNullOrEmpty(vrDeviceDocumentId))
        {
            Debug.LogWarning("[ExperimentLoader] Firebase not ready or vrDeviceDocumentId not set — skipping status update.");
            return;
        }

        string studentId = config != null ? config.studentId : "";
        string groupId = config != null ? config.groupId : "";

        Dictionary<string, object> update = new()
        {
            { "Status", status },
            { "currentExperimentId", experimentSceneName },
            { "currentUserId", studentId },
            { "currentGroupId", groupId },
            { "currentSessionStart", FieldValue.ServerTimestamp },
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