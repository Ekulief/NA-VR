using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ExperimentLocomotionController : MonoBehaviour
{
    private CharacterController _characterController;
    private bool _locomotionLocked = false;
    private Vector3 _lockedPosition;
    private readonly List<Behaviour> _disabledLocomotion = new();
    private GameObject _cachedPlayer;

    private GameObject FindVRPlayer()
    {
        if (_cachedPlayer != null) return _cachedPlayer;

        _cachedPlayer = GameObject.Find("VR Player");

        if (_cachedPlayer == null)
        {
            var xr = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xr != null) _cachedPlayer = xr.gameObject;
        }

        return _cachedPlayer;
    }

    public void LockLocomotion()
    {
        if (_locomotionLocked) return;

        GameObject origin = FindVRPlayer();
        if (origin == null) return;

        Debug.Log($"[ExperimentLocomotion] Locking position for player '{origin.name}' (Turning remains fully active)...");

        // Only disable movement and teleportation. Turning providers are intentionally excluded so physical head turning/snapping is never locked.
        DisableIfFound<ContinuousMoveProvider>(origin, "Continuous Move");
        DisableIfFound<TeleportationProvider>(origin, "Teleportation Provider");

        // Safely disable CharacterControllerDriver components via type name inspection
        DisableCharacterControllerDrivers(origin);

        _characterController = origin.GetComponent<CharacterController>();
        if (_characterController != null && _characterController.enabled)
        {
            _characterController.enabled = false;
        }

        _lockedPosition = origin.transform.position;
        _locomotionLocked = true;
        Debug.Log("[ExperimentLocomotion] Position locked. Physical turning and UI rays remain active.");
    }

    public void UnlockLocomotion()
    {
        if (!_locomotionLocked) return;

        foreach (var c in _disabledLocomotion)
        {
            if (c != null) c.enabled = true;
        }
        _disabledLocomotion.Clear();

        if (_characterController != null)
            _characterController.enabled = true;

        _locomotionLocked = false;
        Debug.Log("[ExperimentLocomotion] Locomotion unlocked.");
    }

    private void DisableIfFound<T>(GameObject root, string label) where T : Behaviour
    {
        var comps = root.GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            if (c == null || !c.enabled) continue;
            if (c.GetType().Name.Contains("RayInteractor") || c.GetType().Name.Contains("Canvas")) continue;

            c.enabled = false;
            _disabledLocomotion.Add(c);
        }
    }

    private void DisableCharacterControllerDrivers(GameObject root)
    {
        var comps = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in comps)
        {
            if (c == null || !c.enabled) continue;
            string typeName = c.GetType().Name;

            if (typeName.Contains("CharacterControllerDriver"))
            {
                c.enabled = false;
                _disabledLocomotion.Add(c);
            }
        }
    }

    private void LateUpdate()
    {
        if (!_locomotionLocked) return;

        GameObject origin = FindVRPlayer();
        if (origin == null) return;

        // Explicitly lock ONLY the position vector, leaving transform.rotation completely unconstrained
        origin.transform.position = _lockedPosition;
    }
}