using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Cardboard6DoF : MonoBehaviour
{
    [Header("Height Adjustment")]
    [Tooltip("Default height of the camera locally inside your Player layout wrapper (in meters).")]
    public float defaultSpawnHeight = 1.65f;

    private List<XRNodeState> nodeStates = new List<XRNodeState>();
    private Vector3 initialTrackingOffset = Vector3.zero;
    private bool offsetInitialized = false;
    private bool isWarmingUp = true;

    void Start()
    {
        // Enforce baseline position and height instantly on startup
        transform.localPosition = new Vector3(0f, defaultSpawnHeight, 0f);

        // Start a timed warm-up delay to let the tracking subsystem settle down cleanly
        StartCoroutine(StabilizeTrackingRoutine());
    }

    private IEnumerator StabilizeTrackingRoutine()
    {
        isWarmingUp = true;

        // Wait 0.2 seconds to give the Editor simulation or mobile hardware tracking buffer time to clear junk data
        yield return new WaitForSeconds(0.2f);

        // Explicitly fetch and lock down our anchor center-point right now 
        // to prevent any race-conditions or frame-timing offsets in Update()
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.CenterEye && nodeState.tracked)
            {
                if (nodeState.TryGetPosition(out Vector3 stablePos))
                {
                    initialTrackingOffset = stablePos;
                    offsetInitialized = true;
                    break;
                }
            }
        }

        isWarmingUp = false;
    }

    public void Recalibrate()
    {
        offsetInitialized = false;
        StartCoroutine(StabilizeTrackingRoutine());
    }

    void Update()
    {
        // Skip updating positions completely while tracking stabilizes
        if (isWarmingUp) return;

        InputTracking.GetNodeStates(nodeStates);

        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.CenterEye)
            {
                if (nodeState.tracked)
                {
                    if (nodeState.TryGetPosition(out Vector3 arCalculatedPosition))
                    {
                        // Backup safety check: if the startup coroutine missed it, capture it safely here
                        if (!offsetInitialized)
                        {
                            initialTrackingOffset = arCalculatedPosition;
                            offsetInitialized = true;
                        }

                        // Calculate positional change relative to our exact starting position anchor
                        Vector3 dynamicMovement = arCalculatedPosition - initialTrackingOffset;

                        // Apply the relative offset locally to our object
                        transform.localPosition = new Vector3(
                            dynamicMovement.x,
                            dynamicMovement.y + defaultSpawnHeight,
                            dynamicMovement.z
                        );
                    }
                }
                break; // Exit loop early once CenterEye is handled
            }
        }
    }
}