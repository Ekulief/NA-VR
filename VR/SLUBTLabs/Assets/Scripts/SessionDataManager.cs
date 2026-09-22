using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// SLUBT Labs — Session Data Manager
///
/// Handles session lifecycle, local JSON logging, and cloud synchronization
/// with Firebase Firestore.
/// </summary>
public class SessionDataManager : MonoBehaviour
{
    public static SessionDataManager Instance { get; private set; }

    [Header("Local File Settings")]
    [Tooltip("File name for the local results JSON. Saved under Application.persistentDataPath.")]
    public string fileName = "slubt_results.json";

    [Header("Firestore Settings")]
    [Tooltip("Target Firestore collection name for storing experiment results.")]
    public string collectionName = "ExperimentReports";

    /// <summary>Unique ID for this participant/session. Generated fresh on app launch.</summary>
    public string UserId { get; private set; }

    private string FilePath => Path.Combine(Application.persistentDataPath, fileName);
    private FirebaseFirestore _db;
    private bool _isFirebaseReady = false;

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

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _db = FirebaseFirestore.DefaultInstance;
                _isFirebaseReady = true;
                Debug.Log("[SLUBT Labs] Firebase Firestore initialized successfully.");
            }
            else
            {
                Debug.LogError($"[SLUBT Labs] Could not resolve Firebase dependencies: {task.Result}");
            }
        });
    }

    // ── Public API — call these from experiment scripts ─────────────────────

    /// <summary>
    /// Records depth estimation trial data to both local JSON and Firebase Firestore.
    /// Matches the signature called by DepthPerceptionUI.
    /// </summary>
    public void RecordDepthEstimate(float estimatedHeight, float actualHeight, float error)
    {
        ExperimentResult result = new ExperimentResult
        {
            userId = UserId,
            experimentName = "DepthPerception",
            inputHeight = estimatedHeight,
            actualHeight = actualHeight,
            difference = error,
            timestampUtc = DateTime.UtcNow.ToString("o")
        };

        // 1. Save locally as backup
        SaveResultLocally(result);

        // 2. Upload to Firestore
        _ = SaveResultToFirestoreAsync(result);
    }

    // ── Internal Save Logic ───────────────────────────────────────────────

    private async Task SaveResultToFirestoreAsync(ExperimentResult result)
    {
        if (!_isFirebaseReady || _db == null)
        {
            Debug.LogWarning("[SLUBT Labs] Firestore is not initialized yet. Skipping cloud save.");
            return;
        }

        try
        {
            CollectionReference reportsRef = _db.Collection(collectionName);
            await reportsRef.AddAsync(result);
            Debug.Log($"[SLUBT Labs] Successfully uploaded result to Firestore for User: {result.userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SLUBT Labs] Failed to upload result to Firestore: {e.Message}");
        }
    }

    private void SaveResultLocally(ExperimentResult result)
    {
        ResultCollection collection = LoadExistingLocal();
        collection.results.Add(result);

        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[SLUBT Labs] Saved result locally for {result.userId} to {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SLUBT Labs] Failed to save result locally: {e.Message}");
        }
    }

    private ResultCollection LoadExistingLocal()
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
            Debug.LogWarning($"[SLUBT Labs] Could not read existing results file, starting fresh: {e.Message}");
            return new ResultCollection();
        }
    }
}

// ── Data types ────────────────────────────────────────────────────────────

[Serializable]
[FirestoreData]
public class ExperimentResult
{
    [FirestoreProperty]
    public string userId { get; set; }

    [FirestoreProperty]
    public string experimentName { get; set; }

    [FirestoreProperty]
    public float inputHeight { get; set; }

    [FirestoreProperty]
    public float actualHeight { get; set; }

    [FirestoreProperty]
    public float difference { get; set; }

    [FirestoreProperty]
    public string timestampUtc { get; set; }
}

[Serializable]
public class ResultCollection
{
    public List<ExperimentResult> results = new List<ExperimentResult>();
}