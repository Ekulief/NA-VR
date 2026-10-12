using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreImporter : MonoBehaviour
{
    [Header("Firestore")]
    public string collectionName = "logs";

    [Header("Deletion Settings")]
    public bool runOnStart = false;

    // Keep false for the first test.
    public bool actuallyDelete = false;

    [Tooltip("Uses the device's local date.")]
    public bool useLocalDate = true;

    private async void Start()
    {
        if (runOnStart)
            await DeleteLogsFromYesterdayAndToday();
    }

    [ContextMenu("Delete Logs From Yesterday And Today")]
    public async void DeleteLogsMenu()
    {
        await DeleteLogsFromYesterdayAndToday();
    }

    public async Task DeleteLogsFromYesterdayAndToday()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // Past 7 days, including the current time.
        DateTime endUtc = DateTime.UtcNow;
        DateTime startUtc = endUtc.AddDays(-7);

        Timestamp startTimestamp = Timestamp.FromDateTime(startUtc);
        Timestamp endTimestamp = Timestamp.FromDateTime(endUtc);

        Debug.Log(
            $"[LogCleaner] Searching logs from {startUtc:yyyy-MM-dd HH:mm} UTC " +
            $"to {endUtc:yyyy-MM-dd HH:mm} UTC.");

        try
        {
            QuerySnapshot snapshot = await db.Collection(collectionName)
                .WhereGreaterThanOrEqualTo("timestamp", startTimestamp)
                .WhereLessThanOrEqualTo("timestamp", endTimestamp)
                .GetSnapshotAsync();

            List<DocumentReference> documentsToDelete =
                new List<DocumentReference>();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                documentsToDelete.Add(document.Reference);
            }

            Debug.Log(
                $"[LogCleaner] Found {documentsToDelete.Count} matching documents.");

            if (!actuallyDelete)
            {
                Debug.LogWarning(
                    "[LogCleaner] DRY RUN: nothing deleted. " +
                    "Set actuallyDelete = true to proceed.");
                return;
            }

            int deleted = 0;

            for (int i = 0; i < documentsToDelete.Count; i += 500)
            {
                WriteBatch batch = db.StartBatch();
                int batchEnd = Math.Min(i + 500, documentsToDelete.Count);

                for (int j = i; j < batchEnd; j++)
                {
                    batch.Delete(documentsToDelete[j]);
                }

                await batch.CommitAsync();
                deleted += batchEnd - i;
            }

            Debug.Log(
                $"[LogCleaner] Successfully deleted {deleted} log documents.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogCleaner] Failed: {ex}");
        }
    }
}