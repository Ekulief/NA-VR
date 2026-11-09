using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// Starts an experiment when the player's head stays inside a circle around this
/// object for a short time. Works for teleporting onto the anchor, thumbstick
/// walking, and physically walking, because it just checks the headset position.
///
/// Put this on the ExperimentAnchor (or on an empty child at its centre).
/// Leave Scene Name empty to automatically use the scene from the current config.
/// </summary>
public class ExperimentWalkTrigger : MonoBehaviour
{
    [Header("What to load")]
    [Tooltip("Optional. If empty, the ExperimentLoader in the scene is found automatically.")]
    public ExperimentLoader loader;

    [Tooltip("Exact scene name. Leave empty to automatically resolve from the current config.")]
    public string sceneName;

    [Header("Zone")]
    [Tooltip("Optional. Defaults to this object's transform.")]
    public Transform center;
    public float radius = 1.0f;

    [Tooltip("How far above the anchor the head may be (metres). Keeps other floors from triggering.")]
    public float maxHeadHeight = 3.0f;
    public float minHeadHeight = -1.0f;

    [Header("Timing")]
    [Tooltip("Seconds the player must stay inside before the experiment starts.")]
    public float dwellSeconds = 1.0f;

    [Header("Diagnostics")]
    public bool debugLogging = true;

    [Tooltip("Print live distance numbers when the head is within this many metres.")]
    public float debugNearDistance = 4f;

    private float _insideTime;
    private bool _armed = true;
    private bool _loading;
    private Transform _head;
    private float _nextDebug;
    private bool _warnedSetup;
    private bool _loggedActiveConfig;

    private void Update()
    {
        if (!EnsureSetup()) return;
        if (_loading) return;

        // Log the currently active experiment once (when config becomes ready)
        if (!_loggedActiveConfig && ExperimentConfigLoader.IsReady && ExperimentConfigLoader.Current != null)
        {
            _loggedActiveConfig = true;
            var cfg = ExperimentConfigLoader.Current;
            Debug.Log(
                $"[WalkTrigger:{name}] ACTIVE CONFIG → " +
                $"experimentId='{cfg.experimentId}' | " +
                $"moduleId='{cfg.moduleId}' | " +
                $"sceneId='{cfg.sceneId}' | " +
                $"progressId='{cfg.progressId}'",
                this);
        }

        if (loader.IsBusy)
        {
            _armed = false;
            _insideTime = 0f;
            return;
        }

        Vector3 c = center != null ? center.position : transform.position;
        Vector3 d = _head.position - c;
        float flat = new Vector2(d.x, d.z).magnitude;
        bool inside = flat <= radius && d.y >= minHeadHeight && d.y <= maxHeadHeight;

        if (debugLogging && Time.unscaledTime >= _nextDebug &&
            new Vector3(d.x, 0f, d.z).magnitude <= debugNearDistance)
        {
            _nextDebug = Time.unscaledTime + 1f;
            Debug.Log(
                $"[WalkTrigger:{name}] flatDistance={flat:F2} (radius {radius:F2}), " +
                $"heightAboveAnchor={d.y:F2} (allowed {minHeadHeight:F1}..{maxHeadHeight:F1}), " +
                $"inside={inside}, armed={_armed}, dwell={_insideTime:F1}/{dwellSeconds:F1}",
                this);
        }

        if (!inside)
        {
            _armed = true;
            _insideTime = 0f;
            return;
        }

        if (!_armed) return;

        _insideTime += Time.deltaTime;

        if (_insideTime >= dwellSeconds)
        {
            _armed = false;
            _insideTime = 0f;
            StartCoroutine(LoadAfterRefresh());
        }
    }

    /// <summary>
    /// Re-fetches the active session from Firestore so we never load the previous
    /// experiment after End Attempt / finish, then resolves and loads the scene.
    /// </summary>
    private System.Collections.IEnumerator LoadAfterRefresh()
    {
        _loading = true;

        var config = ExperimentConfigLoader.Current;
        if (config == null)
        {
            Debug.LogWarning($"[WalkTrigger:{name}] No ExperimentConfig available.", this);
            _loading = false;
            yield break;
        }

        // Same refresh the teleport anchor does — critical after End Attempt
        Task fetchTask = config.FetchActiveSessionFromFirestore();
        while (fetchTask != null && !fetchTask.IsCompleted)
            yield return null;

        if (fetchTask != null && fetchTask.IsFaulted)
        {
            Debug.LogError($"[WalkTrigger:{name}] FetchActiveSessionFromFirestore failed: {fetchTask.Exception?.GetBaseException().Message}", this);
            _loading = false;
            yield break;
        }

        // Keep SessionController in sync if a progress id came back
        if (SessionController.Instance != null && !string.IsNullOrEmpty(config.progressId))
            SessionController.Instance.SwitchSession(config.progressId);

        // Refuse to load if nothing is assigned (ended attempt, no new session yet)
        if (string.IsNullOrEmpty(config.progressId))
        {
            Debug.LogWarning($"[WalkTrigger:{name}] No active progress. Connect / start a session from the web first.", this);
            _loading = false;
            yield break;
        }

        string finalScene = ResolveSceneName(config);
        if (string.IsNullOrEmpty(finalScene))
        {
            Debug.LogWarning($"[WalkTrigger:{name}] No scene name available after refresh.", this);
            _loading = false;
            yield break;
        }

        if (debugLogging)
        {
            Debug.Log(
                $"[WalkTrigger:{name}] Starting scene '{finalScene}' " +
                $"(source={(string.IsNullOrEmpty(sceneName) ? "config" : "inspector override")}) | " +
                $"experimentId='{config.experimentId}' | moduleId='{config.moduleId}' | " +
                $"sceneId='{config.sceneId}' | progressId='{config.progressId}'",
                this);
        }

        if (loader != null)
            loader.LoadScene(finalScene);
        else
            Debug.LogError($"[WalkTrigger:{name}] ExperimentLoader is null.", this);

        _loading = false;
    }

    /// <summary>
    /// Priority: inspector override → config.sceneId → moduleId map (same as ExperimentAnchor).
    /// </summary>
    private string ResolveSceneName(ExperimentConfig config)
    {
        if (!string.IsNullOrEmpty(sceneName))
            return sceneName;

        if (config == null)
            return null;

        if (!string.IsNullOrEmpty(config.sceneId))
            return config.sceneId;

        switch (config.moduleId)
        {
            case "Depth_Perception": return "Depth Perception Scene";
            case "Distance_Perception": return "Distance Perception Scene";
            case "Attentional_Blindness": return "Attentional Blindness Scene";
            case "Odd_Item_Detection": return "Odd Item Detection Scene";
            case "Memory2": return "Memory Scene 2";
            case "Memory": return "Memory Scene";
            default:
                Debug.LogWarning($"[WalkTrigger:{name}] Unknown moduleId: '{config.moduleId}'", this);
                return null;
        }
    }

    private bool EnsureSetup()
    {
        if (loader == null)
            loader = FindAnyObjectByType<ExperimentLoader>();

        if (_head == null)
        {
            XROrigin origin = FindAnyObjectByType<XROrigin>();
            if (origin != null && origin.Camera != null)
                _head = origin.Camera.transform;
            else if (loader != null && loader.xrCamera != null)
                _head = loader.xrCamera.transform;
            else if (Camera.main != null)
                _head = Camera.main.transform;
        }

        bool ok = loader != null && _head != null;

        if (!ok && !_warnedSetup)
        {
            _warnedSetup = true;
            Debug.LogWarning(
                $"[WalkTrigger:{name}] Not active: " +
                (loader == null ? "no ExperimentLoader found. " : "") +
                (_head == null ? "no player camera found (is there an XR Origin?). " : ""),
                this);
        }

        if (ok) _warnedSetup = false;
        return ok;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 c = center != null ? center.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, radius);
        Gizmos.DrawLine(c, c + Vector3.up * maxHeadHeight);
    }
}