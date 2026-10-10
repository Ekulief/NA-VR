using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

/// <summary>
/// Central logger for all experiment events.
/// Writes to the top-level "logs" collection with full session IDs.
/// </summary>
public static class ExperimentLogger
{
    /// <summary>
    /// Full log method with optional data and source.
    /// </summary>
    public static async void Log(
        string eventType,
        string message,
        Dictionary<string, object> data = null,
        string source = "VR")
    {
        try
        {
            var cfg = ExperimentConfigLoader.Current;
            if (cfg == null)
            {
                Debug.LogWarning("[ExperimentLogger] No ExperimentConfig available — log skipped.");
                return;
            }

            var doc = new Dictionary<string, object>
            {
                { "progressId",   cfg.progressId   ?? "" },
                { "experimentId", cfg.experimentId ?? "" },
                { "studentId",    cfg.studentId    ?? "" },
                { "groupId",      cfg.groupId      ?? "" },
                { "blockId",      cfg.blockId      ?? "" },
                { "vrId",         cfg.vrId         ?? "" },
                { "moduleName",   "Odd_Item_Detection" }, // update per manager if needed
                { "eventType",    eventType },
                { "message",      message },
                { "source",       source },
                { "timestamp",    DateTime.UtcNow.ToString("o") }
            };

            if (data != null && data.Count > 0)
                doc["data"] = data;

            await FirebaseFirestore.DefaultInstance
                .Collection("logs")
                .AddAsync(doc);

            Debug.Log($"[ExperimentLogger] {eventType}: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExperimentLogger] Failed to write log: {ex.Message}");
        }
    }

    /// <summary>
    /// Simple overload – just eventType + message.
    /// </summary>
    public static void Log(string eventType, string message)
    {
        Log(eventType, message, null, "VR");
    }
}