using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Session Data Manager
///
/// SETUP:
///   a) Put this script on a GameObject in your always-loaded "Main VR Scene"
///      (same scene ExperimentLoader lives in, since it's found the same way).
///   b) Leave "File Name" as-is unless you want a different results file.
///   c) That's it — no other wiring needed. Any experiment script can call
///      SessionDataManager.Instance.RecordDepthEstimate(value) once this
///      object exists in the loaded scenes.
///
/// WHERE THE FILE ENDS UP (Windows):
///   %userprofile%\AppData\LocalLow\<CompanyName>\<ProductName>\slubt_results.json
///   (This is what Application.persistentDataPath resolves to on Windows.)
///
/// USER ID:
///   A fresh GUID is generated every time the app launches, i.e. one ID per
///   participant session. If a participant does multiple experiments in one
///   sitting, all their results share that same ID. If you close and reopen
///   the app between participants, each gets a new ID automatically.
/// </summary>
public class SessionDataManager : MonoBehaviour
{
    public static SessionDataManager Instance { get; private set; }

    [Tooltip("File name for the local results JSON. Saved under Application.persistentDataPath.")]
    public string fileName = "slubt_results.json";

    /// <summary>Unique ID for this participant/session. Generated fresh on app launch.</summary>
    public string UserId { get; private set; }

    private string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Awake()
    {
        // Standard singleton guard — in case this scene gets loaded additively more than once
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UserId = Guid.NewGuid().ToString();
        Debug.Log($"[SLUBT Labs] Session started. UserId = {UserId}");
        Debug.Log($"[SLUBT Labs] Results file path: {FilePath}");
    }

    // ── Public API — call these from experiment scripts ─────────────────────

    public void RecordDepthEstimate(float heightMetres)
    {
        var result = new ExperimentResult
        {
            userId = UserId,
            experimentName = "DepthPerception",
            heightEstimateMetres = heightMetres,
            timestampUtc = DateTime.UtcNow.ToString("o")
        };

        SaveResult(result);
    }

    // Add more Record___ methods here as other experiments come online, e.g.:
    // public void RecordReactionTime(float seconds) { ... }

    // ── Internal save/load ────────────────────────────────────────────────

    private void SaveResult(ExperimentResult result)
    {
        ResultCollection collection = LoadExisting();
        collection.results.Add(result);

        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[SLUBT Labs] Saved result for {result.userId} to {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SLUBT Labs] Failed to save result: {e}");
        }
    }

    private ResultCollection LoadExisting()
    {
        if (!File.Exists(FilePath))
            return new ResultCollection();

        try
        {
            string existingJson = File.ReadAllText(FilePath);
            ResultCollection loaded = JsonUtility.FromJson<ResultCollection>(existingJson);
            return loaded ?? new ResultCollection();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SLUBT Labs] Could not read existing results file, starting fresh: {e}");
            return new ResultCollection();
        }
    }
}

// ── Data types ────────────────────────────────────────────────────────────
// JsonUtility can't serialize a top-level List<T>, so results are wrapped
// in ResultCollection. This also makes it trivial to add fields per-experiment
// later without breaking the file format for old entries.

[Serializable]
public class ExperimentResult
{
    public string userId;
    public string experimentName;
    public float heightEstimateMetres;
    public string timestampUtc;
}

[Serializable]
public class ResultCollection
{
    public List<ExperimentResult> results = new List<ExperimentResult>();
}
