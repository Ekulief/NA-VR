using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(TeleportationAnchor))]
public class InSceneTeleporter : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Type the EXACT name of the GameObject to teleport to in this scene.")]
    public string destinationName = "Reference Point";

    [Header("Height Control")]
    [Tooltip("If enabled, forces camera Y eye level to a fixed height above target floor.")]
    public bool overrideEyeHeight = false;
    [Tooltip("Target eye level height in meters relative to destination floor.")]
    public float targetEyeHeight = 1.6f;

    private bool _hasTriggered = false;

    private void OnEnable()
    {
        _hasTriggered = false;
        GetComponent<TeleportationAnchor>().selectExited.AddListener(OnAnchorSelected);
    }

    private void OnDisable()
    {
        GetComponent<TeleportationAnchor>().selectExited.RemoveListener(OnAnchorSelected);
    }

    private void OnAnchorSelected(SelectExitEventArgs args)
    {
        if (_hasTriggered) return;
        _hasTriggered = true;
        StartCoroutine(TeleportAfterFrame());
    }

    private IEnumerator TeleportAfterFrame()
    {
        yield return null; // Wait 1 frame for XRI logic

        GameObject destination = GameObject.Find(destinationName);
        if (destination == null)
        {
            Debug.LogError($"[SLUBT Labs] InSceneTeleporter: Could not find '{destinationName}' in scene!");
            _hasTriggered = false;
            yield break;
        }

        GameObject xrOriginObj = GetXROriginObject();
        if (xrOriginObj == null)
        {
            Debug.LogError("[SLUBT Labs] InSceneTeleporter: Could not find XR Origin!");
            _hasTriggered = false;
            yield break;
        }

        Camera vrCamera = Camera.main ?? xrOriginObj.GetComponentInChildren<Camera>();
        if (vrCamera == null)
        {
            xrOriginObj.transform.position = destination.transform.position;
            xrOriginObj.transform.rotation = destination.transform.rotation;
            yield break;
        }

        // 1. First align rotation matching target
        float currentCamYAngle = vrCamera.transform.eulerAngles.y;
        float targetYAngle = destination.transform.eulerAngles.y;
        float rotationDelta = targetYAngle - currentCamYAngle;
        xrOriginObj.transform.Rotate(0f, rotationDelta, 0f, Space.World);

        // 2. Calculate horizontal physical offset (X/Z)
        Vector3 cameraOffset = vrCamera.transform.position - xrOriginObj.transform.position;
        cameraOffset.y = 0f;

        // 3. Compute final position based on height preferences
        Vector3 finalPos = destination.transform.position - cameraOffset;

        if (overrideEyeHeight)
        {
            Transform cameraOffsetTransform = xrOriginObj.transform.Find("Camera Offset");
            float localCamY = cameraOffsetTransform != null ? cameraOffsetTransform.localPosition.y : vrCamera.transform.localPosition.y;
            finalPos.y = destination.transform.position.y + targetEyeHeight - localCamY;
        }
        else
        {
            // Retain natural physical head height above the new floor level
            float localHeadHeight = vrCamera.transform.position.y - xrOriginObj.transform.position.y;
            finalPos.y = destination.transform.position.y;
        }

        xrOriginObj.transform.position = finalPos;
        Debug.Log($"[SLUBT Labs] InSceneTeleporter: Teleported to '{destinationName}' cleanly.");
    }

    private GameObject GetXROriginObject()
    {
        ExperimentLoader loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader != null && loader.xrOrigin != null) return loader.xrOrigin;

        XROrigin originComponent = FindAnyObjectByType<XROrigin>();
        return originComponent != null ? originComponent.gameObject : null;
    }
}