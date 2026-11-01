using UnityEngine;

/// <summary>
/// Starts an experiment when the player's head stays inside a circle around this
/// object for a short time. Works for teleporting onto the anchor, thumbstick
/// walking, and physically walking, because it just checks the headset position.
///
/// Put this on the ExperimentAnchor (or on an empty child at its centre).
/// </summary>
public class ExperimentWalkTrigger : MonoBehaviour
{
    [Header("What to load")]
    public ExperimentLoader loader;
    [Tooltip("Exact scene name, e.g. the same one you use with ExperimentLoader.LoadScene.")]
    public string sceneName;

    [Header("Zone")]
    [Tooltip("Optional. Defaults to this object's transform.")]
    public Transform center;
    public float radius = 0.75f;
    [Tooltip("How far above the anchor the head may be (metres). Keeps other floors from triggering.")]
    public float maxHeadHeight = 2.5f;
    public float minHeadHeight = -0.5f;

    [Header("Timing")]
    [Tooltip("Seconds the player must stay inside before the experiment starts.")]
    public float dwellSeconds = 1.0f;

    private float _insideTime;
    private bool _armed = true;
    private Transform _head;

    private void Update()
    {
        if (loader == null || string.IsNullOrEmpty(sceneName)) return;

        // While an experiment is loading/running, disarm. After returning to the hub
        // the player must step out of the zone once before it can trigger again.
        if (loader.IsBusy)
        {
            _armed = false;
            _insideTime = 0f;
            return;
        }

        if (_head == null)
        {
            if (Camera.main != null) _head = Camera.main.transform;
            if (_head == null) return;
        }

        Vector3 c = center != null ? center.position : transform.position;
        Vector3 d = _head.position - c;
        float flat = new Vector2(d.x, d.z).magnitude;

        bool inside = flat <= radius && d.y >= minHeadHeight && d.y <= maxHeadHeight;

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
            loader.LoadScene(sceneName);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 c = center != null ? center.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, radius);
        Gizmos.DrawLine(c, c + Vector3.up * maxHeadHeight);
    }
}