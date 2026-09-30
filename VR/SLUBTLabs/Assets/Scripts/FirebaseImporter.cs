using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using System.Threading.Tasks;

public class FirestoreImporter : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("Starting Firestore cleanup...");
        await ProcessQuestionsAndTargets();
    }

    private async Task ProcessQuestionsAndTargets()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("experimentModule").Document("Memory2");

        try
        {
            // 1. Fetch current document from Firestore
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

            // 3. Loop through all targets and process questions
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
                                // Remove enabled fields
                                question.Remove("enabled");
                                question.Remove("enabledQuestion");

                                // Check and update questionType
                                if (question.TryGetValue("questionType", out object typeObj) && typeObj is string currentType)
                                {
                                    string typeLower = currentType.ToLower().Trim();

                                    // Convert Color, Detail, Symbol, Count, and Identification to MultipleChoice; keep YesNo
                                    if (typeLower == "color" ||
                                        typeLower == "detail" ||
                                        typeLower == "symbol" ||
                                        typeLower == "count" ||
                                        typeLower == "identification")
                                    {
                                        question["questionType"] = "MultipleChoice";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 4. Overwrite defaultConfig.targets with the updated targets array
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "defaultConfig.targets", targets }
            };

            await docRef.UpdateAsync(updates);
            Debug.Log("<color=green>Successfully updated question types (including Identification) to MultipleChoice in Firestore!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update Firestore: {ex.Message}");
        }
    }
}