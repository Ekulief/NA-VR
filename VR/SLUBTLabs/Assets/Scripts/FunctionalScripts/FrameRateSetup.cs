using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FrameRateSetup : MonoBehaviour
{
    IEnumerator Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 72;
        Time.fixedDeltaTime = 1f / 72f;

        // The XR display may not be running on the very first frame, so wait briefly.
        for (int i = 0; i < 60; i++)
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            bool applied = false;
            foreach (var d in displays)
            {
                if (d.running)
                {
                    d.foveatedRenderingLevel = 0.5f;
                    applied = true;
                }
            }
            if (applied) yield break;
            yield return null;
        }
    }
}