using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreImporter : MonoBehaviour
{
    [Header("Firestore")]
    public string collectionName = "logs";

    [Header("Deletion Settings")]
    public bool runOnStart = false;

    private async void Start()
    {
        if (runOnStart)
            await DeleteLogsFromLastWeekUntilToday();
    }

    [ContextMenu("Delete Logs From Last Week Until Today")]
    public async void DeleteLogsMenu()
    {
        await DeleteLogsFromLastWeekUntilToday();
    }

    public async Task DeleteLogsFromLastWeekUntilToday()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // Past 7 days up to now (UTC)
        DateTime endUtc = DateTime.UtcNow;
        DateTime startUtc = endUtc.AddDays(-7);

        // Your logs store timestamp as ISO strings, not Firestore Timestamp
        string startIso = startUtc.ToString("o"); // e.g. 2026-10-06T...
        string endIso = endUtc.ToString("o");

        Debug.Log(
            $"[LogCleaner] Searching logs from {startUtc:yyyy-MM-dd HH:mm} UTC " +
            $"to {endUtc:yyyy-MM-dd HH:mm} UTC.");

        try
        {
            QuerySnapshot snapshot = await db.Collection(collectionName)
                .WhereGreaterThanOrEqualTo("timestamp", startIso)
                .WhereLessThanOrEqualTo("timestamp", endIso)
                .GetSnapshotAsync();

            List<DocumentReference> documentsToDelete = new List<DocumentReference>();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                documentsToDelete.Add(document.Reference);
            }

            Debug.Log($"[LogCleaner] Found {documentsToDelete.Count} matching documents. Deleting...");

            int deleted = 0;

            // Firestore batch limit is 500
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

            Debug.Log($"[LogCleaner] Successfully deleted {deleted} log documents.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogCleaner] Failed: {ex}");
        }
    }
}