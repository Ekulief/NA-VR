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
        // Wait one frame so XRI finishes its internal logic
        yield return null;

        GameObject destination = GameObject.Find(destinationName);
        if (destination == null)
        {
            Debug.LogError($"[SLUBT Labs] InSceneTeleporter: Could not find '{destinationName}' in scene!");
            _hasTriggered = false;
            yield break;
        }

        // Locate XR Origin
        GameObject xrOriginObj = null;

        ExperimentLoader loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader != null && loader.xrOrigin != null)
        {
            xrOriginObj = loader.xrOrigin;
        }
        else
        {
            XROrigin originComponent = FindAnyObjectByType<XROrigin>();
            if (originComponent != null)
                xrOriginObj = originComponent.gameObject;
        }

        if (xrOriginObj == null)
        {
            Debug.LogError("[SLUBT Labs] InSceneTeleporter: Could not find XR Origin!");
            _hasTriggered = false;
            yield break;
        }

        // Get actual VR head/eye camera
        Camera vrCamera = Camera.main;
        if (vrCamera == null && xrOriginObj != null)
        {
            vrCamera = xrOriginObj.GetComponentInChildren<Camera>();
        }

        // 1. Calculate player's current physical eye height above the floor/origin
        float currentEyeHeightAboveOrigin = 0f;
        if (vrCamera != null)
        {
            currentEyeHeightAboveOrigin = vrCamera.transform.position.y - xrOriginObj.transform.position.y;
        }

        // 2. Determine target position on floor
        Vector3 targetPos = destination.transform.position;

        // 3. Set origin position taking into account the headset's physical height offset
        xrOriginObj.transform.position = targetPos;
        xrOriginObj.transform.rotation = destination.transform.rotation;

        // If the eye level was calculated, ensure target eye position stays consistent
        if (vrCamera != null && currentEyeHeightAboveOrigin > 0.1f)
        {
            Vector3 adjustedPos = xrOriginObj.transform.position;
            float newCameraY = vrCamera.transform.position.y;
            float intendedEyeY = destination.transform.position.y + currentEyeHeightAboveOrigin;

            // Adjust origin Y so Camera.main Y matches target floor + physical eye height
            adjustedPos.y += (intendedEyeY - newCameraY);
            xrOriginObj.transform.position = adjustedPos;
        }

        Debug.Log($"[SLUBT Labs] InSceneTeleporter: Teleported to '{destinationName}' keeping player eye level intact.");
    }
}