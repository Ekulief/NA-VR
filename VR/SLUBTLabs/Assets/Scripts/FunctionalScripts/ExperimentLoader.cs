using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Extensions;

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
        if (xrOrigin == null || targetTransform == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null)
            mainCam = xrOrigin.GetComponentInChildren<Camera>();

        // Stop CharacterController / Rigidbody from fighting the teleport (causes the "fall")
        CharacterController cc = xrOrigin.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = xrOrigin.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (mainCam != null)
        {
            float currentCamYAngle = mainCam.transform.eulerAngles.y;
            float targetYAngle = targetTransform.eulerAngles.y;
            xrOrigin.transform.Rotate(0f, targetYAngle - currentCamYAngle, 0f, Space.World);

            Vector3 cameraPosition = mainCam.transform.position;
            Vector3 originPosition = xrOrigin.transform.position;
            Vector3 cameraOffsetHorizontal = cameraPosition - originPosition;
            cameraOffsetHorizontal.y = 0f;

            Vector3 finalPosition = targetTransform.position - cameraOffsetHorizontal;

            if (useFixedSpawnHeight)
            {
                // LEGACY: lifts origin so camera sits at fixedSpawnHeight above spawn.
                // This is what caused mid-air spawn + fall with room-scale XR. Keep OFF.
                Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
                float localCamY = cameraOffset != null
                    ? cameraOffset.localPosition.y
                    : mainCam.transform.localPosition.y;
                finalPosition.y = targetTransform.position.y + fixedSpawnHeight - localCamY;
                Debug.LogWarning("[ExperimentLoader] useFixedSpawnHeight is ON — origin raised by ~" +
                                 fixedSpawnHeight + "m. Prefer OFF + Spawn Point on the floor.");
            }
            else
            {
                // Room-scale: origin on the floor; headset tracking provides eye height.
                float floorY = targetTransform.position.y;

                if (snapToFloor)
                {
                    Vector3 rayOrigin = targetTransform.position + Vector3.up * 0.5f;
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, floorSnapDistance + 0.5f))
                    {
                        floorY = hit.point.y;
                        Debug.Log($"[ExperimentLoader] Floor snap hit '{hit.collider.name}' at Y={floorY:F3}");
                    }
                }

                finalPosition.y = floorY;
            }

            xrOrigin.transform.position = finalPosition;
            Debug.Log($"[ExperimentLoader] Teleported XR Origin to {finalPosition} " +
                      $"(spawn='{targetTransform.name}', fixedHeight={useFixedSpawnHeight})");
        }
        else
        {
            xrOrigin.transform.position = targetTransform.position;
            xrOrigin.transform.rotation = targetTransform.rotation;
        }

        // Re-enable CC next frame so it doesn't instantly apply gravity while mid-air
        StartCoroutine(ReenableLocomotion(cc, rb));
    }

    private IEnumerator ReenableLocomotion(CharacterController cc, Rigidbody rb)
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        if (cc != null) cc.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;
            // XR locomotion usually drives motion; leave gravity off to avoid a second drop
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
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