using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SLUBT Labs — Dynamic Experiment Loader
/// Keep this script attached to your central manager object.
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

    // State variables
    private bool _isLoading = false;
    private Scene _loadedScene;
    private bool _experimentSceneLoaded = false;

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

        // 1. Fade out to loading screen
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f));

        // 2. Load the dynamic experiment scene additively 
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(sceneName);
        _experimentSceneLoaded = true;

        SceneManager.SetActiveScene(_loadedScene);
        DynamicGI.UpdateEnvironment();

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
            // Get Camera Offset height so player stands at correct eye level
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

        // 5. Fade back in
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

        bool foundRespawnObject = false;
        GameObject respawnObject = null;

        // Automatically scan your main Hub scene roots for an object named exactly "Respawn"
        // Search all scenes including Main for Respawn
        Scene hubScene = SceneManager.GetSceneAt(0);
        SceneManager.SetActiveScene(hubScene); // make sure we search the right scene

        respawnObject = GameObject.Find("Respawn");
        foundRespawnObject = respawnObject != null;

        if (!foundRespawnObject)
        {
            Debug.LogError("[SLUBT Labs] CRITICAL ERROR: Could not locate a GameObject named exactly 'Respawn' inside your Main VR Scene hierarchy!");
        }

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
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
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