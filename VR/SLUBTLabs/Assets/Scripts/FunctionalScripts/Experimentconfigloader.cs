using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

/// <summary>
/// Loads session IDs from experimentProgress and applies experiment.configuration.
/// Uses only the currentProgressId pinned to this automatically registered headset.
/// Never chooses a different device's session or the first active session.
/// </summary>
public class ExperimentConfigLoader : MonoBehaviour
{
    [Tooltip("Drag your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    [Tooltip("Legacy local config fallback. Remote mode resolves VR from automatic Firebase registration.")]
    public string fallbackVrId = "VR-01";

    public static ExperimentConfig Current { get; private set; }
    public static bool IsReady { get; private set; } = false;

    public string ResolvedVrId { get; private set; } = "";

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[ConfigLoader] No ExperimentConfig assigned!");
            return;
        }

        Current = config;
        IsReady = false;
        StartCoroutine(LoadConfig());
    }

    private IEnumerator LoadConfig()
    {
        if (!config.useRemoteConfig)
        {
            Debug.Log("[ConfigLoader] Using local config values.");
            IsReady = true;
            yield break;
        }

        Debug.Log("[ConfigLoader] Waiting for Firebase initialization...");
        yield return new WaitUntil(() => FirebaseManager.IsInitialized || FirebaseManager.InitializationFailed);
        if (FirebaseManager.InitializationFailed)
        {
            Debug.LogError("[ConfigLoader] Firebase authentication or automatic device registration failed.");
            yield break;
        }

        Debug.Log("[ConfigLoader] Resolving device identity and loading config...");
        var task = FetchProgressAndConfigAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError($"[ConfigLoader] Failed to load config: {task.Exception}");
            yield break;
        }

        IsReady = true;
        Debug.Log($"[ConfigLoader] Config ready. Resolved vrId = {ResolvedVrId}");
    }

    private async Task FetchProgressAndConfigAsync()
    {
        // ScriptableObject values survive play-mode reloads in some editor setups.
        // Do not carry over another experiment's session when no session is pinned.
        config.studentId = "";
        config.groupId = "";
        config.blockId = "";
        config.experimentId = "";
        config.progressId = "";
        config.moduleId = "";
        config.sceneId = "";
        config.vrId = "";

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        string registeredDocumentId = FirebaseManager.RegisteredDeviceDocumentId;
        if (string.IsNullOrEmpty(registeredDocumentId))
            throw new System.InvalidOperationException("The Firebase VR publisher has not registered.");

        DocumentSnapshot deviceDoc = await db.Collection("vrDevices")
            .Document(registeredDocumentId).GetSnapshotAsync();
        if (!deviceDoc.Exists ||
            GetFieldString(deviceDoc, "deviceId") != SystemInfo.deviceUniqueIdentifier ||
            GetFieldString(deviceDoc, "publisherAuthUid") != FirebaseManager.AuthUid)
            throw new System.InvalidOperationException("The registered VR device identity no longer matches this Unity session.");

        string vrId = GetFieldString(deviceDoc, "vrId");
        if (string.IsNullOrEmpty(vrId))
            throw new System.InvalidOperationException("Registered VR document has no vrId.");

        var loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader == null)
            throw new System.InvalidOperationException("ExperimentLoader not found.");
        loader.vrDeviceDocumentId = deviceDoc.Id; // Filled automatically, no inspector pairing.
        ResolvedVrId = vrId;

        DocumentSnapshot progressDoc = await ResolveProgressDocAsync(db, deviceDoc, vrId);
        if (progressDoc == null)
        {
            Debug.LogWarning($"[ConfigLoader] No experimentProgress resolved for vrId={vrId}. Using local values.");
            return;
        }

        string progressId = progressDoc.Id;

        string studentId = GetFieldString(progressDoc, "studentId");
        string groupId = GetFieldString(progressDoc, "groupId");
        string blockId = GetFieldString(progressDoc, "blockId");
        string experimentId = GetFieldString(progressDoc, "experimentId");
        if (string.IsNullOrEmpty(experimentId))
            experimentId = GetFieldString(progressDoc, "exeprimentId");

        config.studentId = studentId;
        config.groupId = groupId;
        config.blockId = blockId;
        config.experimentId = experimentId;
        config.progressId = progressId;
        config.vrId = vrId;

        Debug.Log($"[ConfigLoader] Progress loaded → studentId={studentId}, groupId={groupId}, experimentId={experimentId}, progressId={progressId}");

        if (SessionController.Instance != null)
            SessionController.Instance.StartListening(progressId);
        else
            Debug.LogWarning("[ConfigLoader] SessionController not found.");

        if (string.IsNullOrEmpty(experimentId))
        {
            Debug.LogWarning("[ConfigLoader] experimentId is empty. Cannot load configuration.");
            return;
        }

        DocumentSnapshot experimentDoc = await db.Collection("experiment")
            .Document(experimentId)
            .GetSnapshotAsync();

        if (!experimentDoc.Exists)
        {
            Debug.LogWarning($"[ConfigLoader] experiment/{experimentId} does not exist.");
            return;
        }

        config.moduleId = GetFieldString(experimentDoc, "moduleId");
        config.sceneId = GetFieldString(experimentDoc, "sceneId");
        Debug.Log($"[ConfigLoader] Experiment document loaded → moduleId='{config.moduleId}', sceneId='{config.sceneId}'");

        Dictionary<string, object> configMap = null;
        if (experimentDoc.TryGetValue("configuration", out Dictionary<string, object> c1))
            configMap = c1;
        else if (experimentDoc.TryGetValue("defaultConfig", out Dictionary<string, object> c2))
            configMap = c2;

        if (configMap != null)
        {
            config.ApplyConfigurationMap(configMap);
            Debug.Log("[ConfigLoader] Configuration applied from experiment document.");
        }
        else
        {
            Debug.LogWarning("[ConfigLoader] No 'configuration' or 'defaultConfig' on experiment document.");
        }
    }

    private async Task<DocumentSnapshot> ResolveProgressDocAsync(
       FirebaseFirestore db,
       DocumentSnapshot deviceDoc,
       string vrId)
    {
        if (deviceDoc != null && deviceDoc.ContainsField("currentProgressId"))
        {
            string pinnedId = deviceDoc.GetValue<string>("currentProgressId") ?? "";
            if (!string.IsNullOrEmpty(pinnedId))
            {
                DocumentSnapshot pinned = await db.Collection("experimentProgress")
                    .Document(pinnedId)
                    .GetSnapshotAsync();

                if (pinned.Exists && GetFieldString(pinned, "vrId") == vrId &&
                    (GetFieldString(pinned, "experimentId") != "" ||
                     GetFieldString(pinned, "exeprimentId") != ""))
                {
                    Debug.Log($"[ConfigLoader] Using pinned currentProgressId={pinnedId}");
                    return pinned;
                }

                Debug.LogWarning($"[ConfigLoader] currentProgressId={pinnedId} is missing, incomplete, or assigned to another VR.");
            }
        }

        Debug.LogWarning("[ConfigLoader] Device has no currentProgressId. Assign an experiment session from the web first; refusing to select another session.");
        return null;
    }

    private static string GetFieldString(DocumentSnapshot snap, string field)
    {
        if (snap == null || !snap.ContainsField(field))
            return "";
        try
        {
            return snap.GetValue<string>(field) ?? "";
        }
        catch
        {
            return snap.GetValue<object>(field)?.ToString() ?? "";
        }
    }
}