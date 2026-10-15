using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AttentionalBlindnessConfigImporter : MonoBehaviour
{
    [Header("Firestore")]
    public string collection = "experimentModule";
    public string documentId = "Attentional_Blindness";

    [Header("Furniture (exact Hierarchy names)")]
    public List<string> furnitureList = new List<string>
    {
        "Kitchen Exhaust",
        "Sofa at Front",
        "Right Stool",
        "Back Drawer",
        "Back Table",
        "Stop Sign",
        "Left Sink",
        "Right Drawer"
    };

    public string specificFadeTarget = "Sofa at Front";
    public bool useRandomFadeTarget = true;

    [Header("Settings")]
    public bool runOnStart = true;   // ← set to false if you only want manual runs later

    private async void Start()
    {
        if (runOnStart)
            await WriteToFirebase();
    }

    [ContextMenu("Write Furniture List to Firebase")]
    public async void WriteToFirebaseMenu()
    {
        await WriteToFirebase();
    }

    public async Task WriteToFirebase()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection(collection).Document(documentId);

        // Write inside the nested "defaultConfig" map
        var defaultConfig = new Dictionary<string, object>
        {
            { "ab_FurnitureList", furnitureList },
            { "ab_SpecificFadeTarget", specificFadeTarget },
            { "ab_UseRandomFadeTarget", useRandomFadeTarget }
        };

        var data = new Dictionary<string, object>
        {
            { "defaultConfig", defaultConfig }
        };

        try
        {
            await docRef.SetAsync(data, SetOptions.MergeAll);
            Debug.Log($"[Importer] Wrote {furnitureList.Count} furniture items into defaultConfig. Target = '{specificFadeTarget}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Importer] Failed: {ex}");
        }
    }
}