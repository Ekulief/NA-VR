using System.Collections;
using UnityEngine;

public class CarTargetController : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    private void Start()
    {
        StartCoroutine(PositionWhenReady());
    }

    private IEnumerator PositionWhenReady()
    {
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        if (config == null)
            config = ExperimentConfigLoader.Current;

        PositionCarSubject();
    }

    public void PositionCarSubject()
    {
        if (config == null)
            config = ExperimentConfigLoader.Current;

        float distanceMeters = config != null
            ? config.distance_ActualDistanceMeters
            : 50f;

        float minDist = config != null ? config.distance_MinDistanceMeters : 10f;
        float maxDist = config != null ? config.distance_MaxDistanceMeters : 115f;

        if (minDist <= 0f) minDist = 10f;
        if (maxDist <= 0f) maxDist = 115f;

        distanceMeters = Mathf.Clamp(distanceMeters, minDist, maxDist);


        float targetLocalZ = 20f - distanceMeters;

        Vector3 localPos = transform.localPosition;
        localPos.z = targetLocalZ;
        transform.localPosition = localPos;

        Debug.Log($"[CarTargetController] distance={distanceMeters}m → local Z={targetLocalZ}");
        Debug.Log($"[CarTargetController] localPos={transform.localPosition}  worldPos={transform.position}");
    }
}