using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

/// <summary>
/// One-shot: fills experimentModule/Memory (or Memory2) defaultConfig
/// with editable Memory Scene values. Does not wipe existing targets.
/// </summary>
public class FirestoreImporter : MonoBehaviour
{
    [Header("Which document to update")]
    public string documentId = "Memory"; // change to "Memory2" if needed

    [Header("Run")]
    public bool runOnStart = true;
    public bool alsoNormalizeQuestionTypes = true;

    async void Start()
    {
        if (!runOnStart) return;
        await ImportDefaultConfig();
    }

    [ContextMenu("Import Default Config Now")]
    public async void ImportDefaultConfigMenu()
    {
        await ImportDefaultConfig();
    }

    public async Task ImportDefaultConfig()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("experimentModule").Document(documentId);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError($"[FirestoreImporter] Document '{documentId}' does not exist.");
                return;
            }

            Dictionary<string, object> data = snapshot.ToDictionary();

            // Existing defaultConfig (or new)
            Dictionary<string, object> defaultConfig = new Dictionary<string, object>();
            if (data.TryGetValue("defaultConfig", out object dcObj) &&
                dcObj is Dictionary<string, object> existing)
            {
                defaultConfig = new Dictionary<string, object>(existing);
            }

            // ——— Editable Memory Scene defaults ———
            // Only set if missing, so you don't wipe values you already tuned.
            SetIfMissing(defaultConfig, "memory_TimePerRoomSeconds", 60L);
            SetIfMissing(defaultConfig, "memory_TransitionFadeDuration", 0.5);
            SetIfMissing(defaultConfig, "memory_DistractorTaskDuration", 30L);
            SetIfMissing(defaultConfig, "memory_UseDistractorTask", true);
            SetIfMissing(defaultConfig, "memory_RandomizeQuestions", true);

            SetIfMissing(defaultConfig, "memory_BriefingText",
                "You will explore several rooms. Pay close attention to the objects and details in each room. Press Begin Experiment when you are ready.");

            SetIfMissing(defaultConfig, "memory_RoomInstructionText",
                "Explore this room carefully. Remember what you see.");

            SetIfMissing(defaultConfig, "memory_DistractorInstructionText",
                "Count backwards from 100 by 3s.\n\nSay each number aloud.");

            SetIfMissing(defaultConfig, "memory_RecallInstructionText",
                "You will now answer questions about what you saw. Press Begin Questioning when you are ready.");

            // Module meta (from your console)
            SetIfMissing(defaultConfig, "moduleName", "Memory");
            SetIfMissing(defaultConfig, "sceneId", "Memory Scene");
            SetIfMissing(defaultConfig, "globalInstructionDelay", 1.5);
            SetIfMissing(defaultConfig, "description", "");

            // Optional: clean junk field "memory_" empty string if present
            if (defaultConfig.ContainsKey("memory_"))
                defaultConfig.Remove("memory_");

            // Optional: normalize question types inside targets (same as your old script)
            if (alsoNormalizeQuestionTypes &&
                defaultConfig.TryGetValue("targets", out object targetsObj) &&
                targetsObj is List<object> targets)
            {
                NormalizeTargets(targets);
                defaultConfig["targets"] = targets;
            }

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "defaultConfig", defaultConfig }
            });

            Debug.Log($"<color=green>[FirestoreImporter] defaultConfig updated on experimentModule/{documentId}</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FirestoreImporter] Failed: {ex.Message}");
        }
    }

    private static void SetIfMissing(Dictionary<string, object> map, string key, object value)
    {
        if (!map.ContainsKey(key) || map[key] == null || IsEmptyString(map[key]))
            map[key] = value;
    }

    private static bool IsEmptyString(object o)
    {
        return o is string s && string.IsNullOrWhiteSpace(s);
    }

    private static void NormalizeTargets(List<object> targets)
    {
        foreach (var targetObj in targets)
        {
            if (targetObj is not Dictionary<string, object> target) continue;
            if (!target.TryGetValue("questions", out object qObj) || qObj is not List<object> questions)
                continue;

            foreach (var questionObj in questions)
            {
                if (questionObj is not Dictionary<string, object> question) continue;

                question.Remove("enabled");
                question.Remove("enabledQuestion");

                if (question.TryGetValue("questionType", out object typeObj) && typeObj is string currentType)
                {
                    string t = currentType.ToLower().Trim();
                    if (t == "color" || t == "detail" || t == "symbol" ||
                        t == "count" || t == "identification")
                    {
                        question["questionType"] = "MultipleChoice";
                    }
                }
            }
        }
    }
}