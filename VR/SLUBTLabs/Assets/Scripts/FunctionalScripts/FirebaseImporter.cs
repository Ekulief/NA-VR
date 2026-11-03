using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LogCleanupManager : MonoBehaviour
{
    [Header("Firestore Collections")]
    public string logsCollection = "logs";
    public string resultsCollection = "experimentResults";
    public string progressCollection = "experimentProgress";

    [Header("Settings")]
    public bool runOnStart = false;

    [Header("Safety Limit")]
    [Tooltip("Maximum documents to delete in a single execution to prevent performance spikes.")]
    public int batchLimit = 500;

    private async void Start()
    {
        if (runOnStart)
        {
            await Task.Delay(2000); // Slight delay for smooth scene startup
            await DeleteLogsWithinLastMonth();
        }
    }

    [ContextMenu("Delete Logs Within Last Month")]
    public async void DeleteRecentLogsMenu()
    {
        await DeleteLogsWithinLastMonth();
    }

    [ContextMenu("Delete Results Within Last Week")]
    public async void DeleteRecentResultsMenu()
    {
        await DeleteResultsWithinLastWeek();
    }

    [ContextMenu("Delete Progress Within Last Week")]
    public async void DeleteRecentProgressMenu()
    {
        await DeleteProgressWithinLastWeek();
    }

    public async Task DeleteLogsWithinLastMonth()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        DateTime thresholdDate = DateTime.UtcNow.AddMonths(-1);
        string thresholdString = thresholdDate.ToString("o");

        Debug.Log($"[Cleanup] Querying logs created from {thresholdString} onward (within the last month)...");

        try
        {
            Query query = db.Collection(logsCollection)
                            .WhereGreaterThanOrEqualTo("timestamp", thresholdString)
                            .Limit(batchLimit);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                Debug.Log("[Cleanup] No logs found within the last month.");
                return;
            }

            Debug.Log($"[Cleanup] Found {snapshot.Count} recent log(s) to delete. Processing...");

            WriteBatch batch = db.StartBatch();
            int totalDeleted = 0;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                batch.Delete(doc.Reference);
                totalDeleted++;
            }

            await batch.CommitAsync();
            Debug.Log($"[Cleanup] Successfully deleted {totalDeleted} logs from the last month.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Cleanup] Failed to delete recent logs: {ex}");
        }
    }

    public async Task DeleteResultsWithinLastWeek()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        DateTime thresholdDate = DateTime.UtcNow.AddDays(-7);
        string thresholdString = thresholdDate.ToString("o");

        Debug.Log($"[Cleanup] Querying results created from {thresholdString} onward (within the last week)...");

        try
        {
            Query query = db.Collection(resultsCollection)
                            .WhereGreaterThanOrEqualTo("timestamp", thresholdString)
                            .Limit(batchLimit);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                Debug.Log("[Cleanup] No results found within the last week.");
                return;
            }

            Debug.Log($"[Cleanup] Found {snapshot.Count} recent result(s) to delete. Processing...");

            WriteBatch batch = db.StartBatch();
            int totalDeleted = 0;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                batch.Delete(doc.Reference);
                totalDeleted++;
            }

            await batch.CommitAsync();
            Debug.Log($"[Cleanup] Successfully deleted {totalDeleted} results from the last week.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Cleanup] Failed to delete recent results: {ex}");
        }
    }

    public async Task DeleteProgressWithinLastWeek()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        DateTime thresholdDate = DateTime.UtcNow.AddDays(-7);
        string thresholdString = thresholdDate.ToString("o");

        Debug.Log($"[Cleanup] Querying experiment progress from {thresholdString} onward (within the last week)...");

        try
        {
            // Targets the 'completionAt' ISO string field visible in your experimentProgress collection
            Query query = db.Collection(progressCollection)
                            .WhereGreaterThanOrEqualTo("completionAt", thresholdString)
                            .Limit(batchLimit);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                Debug.Log("[Cleanup] No experiment progress records found within the last week.");
                return;
            }

            Debug.Log($"[Cleanup] Found {snapshot.Count} recent progress record(s) to delete. Processing...");

            WriteBatch batch = db.StartBatch();
            int totalDeleted = 0;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                batch.Delete(doc.Reference);
                totalDeleted++;
            }

            await batch.CommitAsync();
            Debug.Log($"[Cleanup] Successfully deleted {totalDeleted} experiment progress records from the last week.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Cleanup] Failed to delete recent experiment progress: {ex}");
        }
    }
}