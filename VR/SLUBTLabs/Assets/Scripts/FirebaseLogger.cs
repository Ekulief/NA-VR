using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal logger that matches the interface used by MemoryExperimentManager.
/// Later you can replace the body with real Firebase Firestore calls.
/// </summary>
public class FirebaseLogger : MonoBehaviour
{
    public void LogTrial(string sessionId, Dictionary<string, object> data)
    {
        // For now just print – replace with real Firebase later
        string log = $"[Memory] Session {sessionId} | ";
        foreach (var kvp in data)
            log += $"{kvp.Key}={kvp.Value} | ";
        Debug.Log(log);
    }

    public void FinalizeSession(string sessionId)
    {
        Debug.Log($"[Memory] Session {sessionId} finished and ready for export");
    }
}