using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;

public static class LocomotionLocker
{
    public enum LockMode
    {
        None,
        MoveOnly,  
        Full        
    }

    private static readonly List<Behaviour> _disabled = new();
    private static CharacterController _cc;
    private static LockMode _currentMode = LockMode.None;
    private static GameObject _origin;

    public static LockMode CurrentMode => _currentMode;
    public static bool IsLocked => _currentMode != LockMode.None;

    public static void Lock(LockMode mode = LockMode.MoveOnly, GameObject xrOriginOverride = null)
    {
        Unlock();

        if (mode == LockMode.None) return;

        _origin = xrOriginOverride;
        if (_origin == null)
        {
            var xr = Object.FindAnyObjectByType<XROrigin>();
            if (xr != null) _origin = xr.gameObject;
        }

        if (_origin == null)
        {
            Debug.LogWarning("[LocomotionLocker] XROrigin not found.");
            return;
        }

        DisableIfFound<ContinuousMoveProvider>(_origin);
        DisableIfFound<ContinuousTurnProvider>(_origin);
        DisableIfFound<SnapTurnProvider>(_origin);

        if (mode == LockMode.Full)
        {
            DisableIfFound<TeleportationProvider>(_origin);
        }

        _cc = _origin.GetComponent<CharacterController>();
        if (_cc != null && _cc.enabled)
            _cc.enabled = false;

        _currentMode = mode;
        Debug.Log($"[LocomotionLocker] Locked with mode: {mode}");
    }

    public static void Unlock()
    {
        if (_currentMode == LockMode.None) return;

        foreach (var c in _disabled)
            if (c != null) c.enabled = true;
        _disabled.Clear();

        if (_cc != null)
            _cc.enabled = true;

        _currentMode = LockMode.None;
        Debug.Log("[LocomotionLocker] Unlocked.");
    }

    private static void DisableIfFound<T>(GameObject root) where T : Behaviour
    {
        foreach (var c in root.GetComponentsInChildren<T>(true))
        {
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            _disabled.Add(c);
        }
    }
}