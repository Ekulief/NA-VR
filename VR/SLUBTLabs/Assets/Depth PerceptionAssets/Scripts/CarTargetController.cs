using UnityEngine;

public class CarTargetController : MonoBehaviour
{
    [Header("Observer Reference")]
    [Tooltip("The camera or player origin point (e.g., Z = -8)")]
    public Transform observerOrigin;

    [Header("Config")]
    public ExperimentConfig config;

    private void Start()
    {
        PositionCarSubject();
    }

    public void PositionCarSubject()
    {
        if (config == null) config = ExperimentConfigLoader.Current;

        // Get assigned distance clamped between 0m and 115m
        float distanceMeters = config != null ? config.depth_ActualDistanceMeters : 50f;
        distanceMeters = Mathf.Clamp(distanceMeters, 0f, 115f);

        // Calculate Z position based on +15 starting origin
        float playerOriginZ = 15f;
        float targetZ = playerOriginZ - distanceMeters;

        Vector3 newPos = transform.position;
        newPos.z = targetZ;
        transform.position = newPos;

        Debug.Log($"[CarTargetController] Placed car at distance {distanceMeters}m (World Z: {transform.position.z}).");
    }
}