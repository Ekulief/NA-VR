using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

/// <summary>
/// One-shot importer for experimentModule/Odd_Item_Detection.
/// Writes the available targets list + randomize flag into defaultConfig.
/// </summary>
public class FirestoreImporter : MonoBehaviour
{
    [Header("Which document to update")]
    public string documentId = "Odd_Item_Detection";

    [Header("Run")]
    public bool runOnStart = true;

    // Exact names from your OddItemManager.oddItemPrefabs list
    private static readonly string[] OddItemTargetNames =
    {
        "RubberDuck",
        "Bowl",
        "Crown",
        "Backpack",
        "Bomb",
        "Diver's Mask",
        "Dynamite",
        "Frying Pan",
        "Hat",
        "Headphones",
        "Idol",
        "Piggybank",
        "Pot",
        "Smartphone",
        "Smiley",
        "Soccer Boot",
        "Steering Wheel",
        "Water Mine",
        "Watering Can",
        "Wheel"
    };

    async void Start()
    {
        if (!runOnStart) return;
        await ImportDefaultConfig();
    }

    [ContextMenu("Import Odd Item Config Now")]
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

            // Keep existing defaultConfig if present
            Dictionary<string, object> defaultConfig = new Dictionary<string, object>();
            if (data.TryGetValue("defaultConfig", out object dcObj) &&
                dcObj is Dictionary<string, object> existing)
            {
                defaultConfig = new Dictionary<string, object>(existing);
            }

            // ——— Odd Item specific fields ———
            // Always overwrite the targets list so it stays in sync with the scene
            defaultConfig["oddItem_AvailableTargets"] = new List<object>(OddItemTargetNames);
            defaultConfig["oddItem_SelectedTarget"] = "";          // empty = no forced selection
            defaultConfig["oddItem_RandomizeTarget"] = true;       // randomize by default

            // Keep / set the other common fields only if missing
            SetIfMissing(defaultConfig, "globalInstructionDelay", 1.5);
            SetIfMissing(defaultConfig, "oddItem_SearchTimeLimitSeconds", 120L);
            SetIfMissing(defaultConfig, "oddItem_RaycastDistance", 10L);
            SetIfMissing(defaultConfig, "oddItem_ExcellentThresholdSeconds", 10L);
            SetIfMissing(defaultConfig, "oddItem_GoodThresholdSeconds", 20L);
            SetIfMissing(defaultConfig, "oddItem_InstructionText",
                "Find the item that doesn't belong on the shelves.\n\nPoint at it and pull the trigger to confirm.");
            SetIfMissing(defaultConfig, "oddItem_WrongItemFeedback",
                "That item belongs here. Keep looking!");
            SetIfMissing(defaultConfig, "oddItem_RatingExcellent", "Excellent!");
            SetIfMissing(defaultConfig, "oddItem_RatingGood", "Good");
            SetIfMissing(defaultConfig, "oddItem_RatingKeepPracticing", "Keep Practicing");
            SetIfMissing(defaultConfig, "oddItem_TargetDisplayName", "the odd item");

            // Module meta
            SetIfMissing(defaultConfig, "moduleName", "Odd Item Detection");
            SetIfMissing(defaultConfig, "sceneId", "Odd Item Scene");
            SetIfMissing(defaultConfig, "description",
                "Participant finds the item that does not belong on the shelves.");

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "defaultConfig", defaultConfig }
            });

            Debug.Log($"<color=green>[FirestoreImporter] Updated experimentModule/{documentId} " +
                      $"with {OddItemTargetNames.Length} available targets + randomize=true</color>");
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
}