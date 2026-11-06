using UnityEngine;
//using Unity.XR.Oculus;

public class FrameRateSetup : MonoBehaviour
{
    [SerializeField] private float targetRefreshRate = 72f;

    private void Start()
    {
        Application.targetFrameRate = (int)targetRefreshRate;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Performance.TrySetDisplayRefreshRate(targetRefreshRate))
            Debug.Log($"[FrameRate] Set display to {targetRefreshRate} Hz");
        else
            Debug.LogWarning("[FrameRate] Could not set the display refresh rate.");
#endif
    }
}