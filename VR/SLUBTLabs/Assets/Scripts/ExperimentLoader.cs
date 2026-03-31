using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SLUBT Labs — Experiment Loader
///
///   1. Player activates the teleport anchor normally (ray + trigger).
///   2. ExperimentAnchor (on the TeleportationAnchor) calls LoadScene() on this.
///   3. We load the target scene additively (Main scene stays loaded).
///   4. We move the XR Origin to the Spawn Point in the new scene.
///   5. On return, we unload the experiment scene and move the player back.
///
/// </summary>
public class ExperimentLoader : MonoBehaviour
{
    [Header("Scene")]
    public string targetSceneName = "Depth Perception Scene";

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

    public void LoadScene()
    {
        if (_isLoading) return;
        StartCoroutine(LoadExperimentScene());
    }

    private IEnumerator LoadExperimentScene()
    {
        _isLoading = true;

        // 1.  loading screen
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f));

        // 2. Load the experiment scene 
        AsyncOperation load = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        yield return new WaitUntil(() => load.isDone);

        _loadedScene = SceneManager.GetSceneByName(targetSceneName);
        _experimentSceneLoaded = true;

        SceneManager.SetActiveScene(_loadedScene);
        DynamicGI.UpdateEnvironment();

        SetMainSceneVisible(false);

        // 3. Find the SpawnPoint 
        GameObject spawnPoint = FindSpawnPointInScene(_loadedScene);

        if (spawnPoint == null)
        {
            Debug.LogError($"[SLUBT Labs] No GameObject named 'Spawn Point' found in '{targetSceneName}'. " +
                           "Create an empty GameObject called Spawn Point and position it where the player should appear.");
            _isLoading = false;
            yield break;
        }

        // 4. Move XR Origin to the spawn point
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = spawnPoint.transform.position;
            xrOrigin.transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogWarning("[SLUBT Labs] xrOrigin is not assigned on Experiment Loader. Player won't be repositioned.");
        }

        // 5.  fade back in
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f));

        _isLoading = false;
    }

    // ── Return to main scene ──────────────────────────────────────────────────

    public void ReturnToHub(Vector3 hubSpawnPosition)
    {
        if (!_experimentSceneLoaded) return;
        StartCoroutine(UnloadExperimentScene(hubSpawnPosition));
    }

    private IEnumerator UnloadExperimentScene(Vector3 returnPosition)
    {
        _isLoading = true;

        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f));

        // Move player back to hub before unloading so they don't fall into void
        if (xrOrigin != null)
            xrOrigin.transform.position = returnPosition;

        Scene mainScene = SceneManager.GetSceneAt(0);
        SceneManager.SetActiveScene(mainScene);
        DynamicGI.UpdateEnvironment();
        SetMainSceneVisible(true);

        AsyncOperation unload = SceneManager.UnloadSceneAsync(_loadedScene);
        yield return new WaitUntil(() => unload.isDone);

        _experimentSceneLoaded = false;

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

            // Also check children of root objects
            Transform found = root.transform.Find("Spawn Point");
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    /// <summary>Fades the optional CanvasGroup between two alpha values.</summary>
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

        // Hide the canvas when fully transparent so it doesn't block raycasts
        if (to <= 0f)
            fadeCanvas.gameObject.SetActive(false);
    }
}