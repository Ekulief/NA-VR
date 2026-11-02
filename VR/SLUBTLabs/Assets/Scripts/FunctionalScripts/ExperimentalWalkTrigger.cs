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

    [Tooltip("Exact scene name. Leave empty to automatically use ExperimentConfig.sceneId.")]
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
    private Transform _head;
    private float _nextDebug;
    private bool _warnedSetup;
    private bool _loggedActiveConfig;

    private void Update()
    {
        if (!EnsureSetup()) return;

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

            string finalScene = ResolveSceneName();

            if (string.IsNullOrEmpty(finalScene))
            {
                Debug.LogWarning($"[WalkTrigger:{name}] No scene name available.", this);
                return;
            }

            // Extra log right before loading so we see exactly what is being used
            if (debugLogging)
            {
                var cfg = ExperimentConfigLoader.Current;
                Debug.Log(
                    $"[WalkTrigger:{name}] Starting scene '{finalScene}' " +
                    $"(source={(string.IsNullOrEmpty(sceneName) ? "config.sceneId" : "inspector override")}) | " +
                    $"config.experimentId='{cfg?.experimentId}' | config.moduleId='{cfg?.moduleId}'",
                    this);
            }

            loader.LoadScene(finalScene);
        }
    }

    /// <summary>
    /// Returns the scene name to load.
    /// Priority: inspector field → current ExperimentConfig.sceneId
    /// </summary>
    private string ResolveSceneName()
    {
        if (!string.IsNullOrEmpty(sceneName))
            return sceneName;

        // Automatic: use whatever experiment is currently assigned to this headset
        if (ExperimentConfigLoader.Current != null &&
            !string.IsNullOrEmpty(ExperimentConfigLoader.Current.sceneId))
        {
            return ExperimentConfigLoader.Current.sceneId;
        }

        return null;
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