using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using System.Threading.Tasks;

public class FirestoreImporter : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("Authenticating...");

        try
        {
            Debug.Log("Signed in successfully! Processing existing targets...");
            await AddEnabledFieldToAllTargets();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Authentication failed: {ex.Message}");
        }
    }

    private async Task AddEnabledFieldToAllTargets()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("experimentModule").Document("Memory2");

        try
        {
            // 1. Fetch document from Firestore
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("Document Memory2 does not exist!");
                return;
            }

            Dictionary<string, object> data = snapshot.ToDictionary();
            List<object> targets = new List<object>();

            // 2. Extract defaultConfig.targets
            if (data.TryGetValue("defaultConfig", out object defaultConfigObj) && defaultConfigObj is Dictionary<string, object> defaultConfig)
            {
                if (defaultConfig.TryGetValue("targets", out object targetsObj) && targetsObj is List<object> existingList)
                {
                    targets = existingList;
                }
            }

            if (targets.Count == 0)
            {
                Debug.LogWarning("No targets found in defaultConfig.targets.");
                return;
            }

            // 3. Loop through all existing targets and set enabled = true on every question
            foreach (var targetObj in targets)
            {
                if (targetObj is Dictionary<string, object> target)
                {
                    if (target.TryGetValue("questions", out object questionsObj) && questionsObj is List<object> questions)
                    {
                        foreach (var questionObj in questions)
                        {
                            if (questionObj is Dictionary<string, object> question)
                            {
                                question["enabled"] = true;
                            }
                        }
                    }
                }
            }

            // 4. Overwrite defaultConfig.targets with updated data
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "defaultConfig.targets", targets }
            };

            await docRef.UpdateAsync(updates);
            Debug.Log("<color=green>Successfully added 'enabled: true' to all questions in Firestore!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update Firestore: {ex.Message}");
        }
    }
}