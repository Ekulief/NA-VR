using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;

/// <summary>
/// Loads session IDs from experimentProgress and applies experiment.configuration.
/// Prefers device currentProgressId, then sceneId match, then first In Progress.
/// </summary>
public class ExperimentConfigLoader : MonoBehaviour
{
    [Tooltip("Drag your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    [Tooltip("Fallback vrId if device is not found in vrDevices.")]
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
        yield return new WaitUntil(() => FirebaseManager.IsInitialized);

        Debug.Log("[ConfigLoader] Resolving device identity and loading config...");
        var task = FetchProgressAndConfigAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
            Debug.LogError($"[ConfigLoader] Failed to load config: {task.Exception}");

        IsReady = true;
        Debug.Log($"[ConfigLoader] Config ready. Resolved vrId = {ResolvedVrId}");
    }

    private async Task FetchProgressAndConfigAsync()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // ── STEP 1: Hardware ID ───────────────────────────────────────────────
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log($"[ConfigLoader] This device hardware ID = {deviceId}");

        // ── STEP 2: vrDevices → vrId + optional currentProgressId ─────────────
        string vrId = fallbackVrId;
        DocumentSnapshot deviceDoc = null;

        QuerySnapshot deviceSnap = await db.Collection("vrDevices")
            .WhereEqualTo("deviceId", deviceId)
            .Limit(1)
            .GetSnapshotAsync();

        if (deviceSnap.Count > 0)
        {
            foreach (var doc in deviceSnap.Documents)
            {
                deviceDoc = doc;
                break;
            }

            if (deviceDoc != null && deviceDoc.ContainsField("vrId"))
            {
                vrId = deviceDoc.GetValue<string>("vrId") ?? fallbackVrId;
                Debug.Log($"[ConfigLoader] Found matching device → assigned vrId = {vrId}");
            }
            else
            {
                Debug.LogWarning("[ConfigLoader] Device found but has no 'vrId'. Using fallback.");
            }
        }
        else
        {
            Debug.LogWarning($"[ConfigLoader] No vrDevices row for deviceId={deviceId}. Using fallback vrId={fallbackVrId}");
        }

        ResolvedVrId = vrId;

        // ── STEP 3: Choose progress document ──────────────────────────────────
        DocumentSnapshot progressDoc = await ResolveProgressDocAsync(db, deviceDoc, vrId);
        if (progressDoc == null)
        {
            Debug.LogWarning($"[ConfigLoader] No experimentProgress resolved for vrId={vrId}. Using local values.");
            return;
        }

        string progressId = progressDoc.Id;

        // ── STEP 4: Assign standard session IDs ────────────────────────────────
        string studentId = GetFieldString(progressDoc, "studentId");
        string groupId = GetFieldString(progressDoc, "groupId");
        string blockId = GetFieldString(progressDoc, "blockId");
        string experimentId = GetFieldString(progressDoc, "experimentId");
        if (string.IsNullOrEmpty(experimentId))
            experimentId = GetFieldString(progressDoc, "exeprimentId"); // typo fallback

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

        // ── STEP 5: Experiment document ───────────────────────────────────────
        DocumentSnapshot experimentDoc = await db.Collection("experiment")
            .Document(experimentId)
            .GetSnapshotAsync();

        if (!experimentDoc.Exists)
        {
            Debug.LogWarning($"[ConfigLoader] experiment/{experimentId} does not exist.");
            return;
        }

        // ── STEP 6: Apply configuration map ───────────────────────────────────
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
        // 1. Pinned currentProgressId on device
        if (deviceDoc != null && deviceDoc.ContainsField("currentProgressId"))
        {
            string pinnedId = deviceDoc.GetValue<string>("currentProgressId") ?? "";
            if (!string.IsNullOrEmpty(pinnedId))
            {
                DocumentSnapshot pinned = await db.Collection("experimentProgress")
                    .Document(pinnedId)
                    .GetSnapshotAsync();

                if (pinned.Exists)
                {
                    Debug.Log($"[ConfigLoader] Using pinned currentProgressId={pinnedId}");
                    return pinned;
                }

                Debug.LogWarning($"[ConfigLoader] currentProgressId={pinnedId} missing. Falling back.");
            }
        }

        // 2. Query all 'In Progress' for this vrId and match scene
        QuerySnapshot progressSnap = await db.Collection("experimentProgress")
            .WhereEqualTo("vrId", vrId)
            .WhereEqualTo("completionStatus", "In Progress")
            .GetSnapshotAsync();

        if (progressSnap.Count == 0)
            return null;

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[ConfigLoader] Active scene='{currentScene}', In Progress count={progressSnap.Count}");

        foreach (DocumentSnapshot progressDoc in progressSnap.Documents)
        {
            string experimentId = GetFieldString(progressDoc, "experimentId");
            if (string.IsNullOrEmpty(experimentId))
                experimentId = GetFieldString(progressDoc, "exeprimentId");

            if (string.IsNullOrEmpty(experimentId))
                continue;

            DocumentSnapshot experimentDoc = await db.Collection("experiment")
                .Document(experimentId)
                .GetSnapshotAsync();

            if (!experimentDoc.Exists)
                continue;

            string sceneId = GetFieldString(experimentDoc, "sceneId");
            string moduleId = GetFieldString(experimentDoc, "moduleId");

            if (SceneMatches(currentScene, sceneId, moduleId))
            {
                Debug.Log($"[ConfigLoader] Scene match → progress={progressDoc.Id}, experiment={experimentId}, sceneId={sceneId}");
                return progressDoc;
            }
        }

        // 3. Fallback: return the first active session
        foreach (DocumentSnapshot progressDoc in progressSnap.Documents)
        {
            Debug.LogWarning($"[ConfigLoader] No scene match for '{currentScene}'. Using first In Progress: {progressDoc.Id}");
            return progressDoc;
        }

        return null;
    }

    private static bool SceneMatches(string currentScene, string sceneId, string moduleId = null)
    {
        if (string.IsNullOrEmpty(currentScene))
            return false;

        string Clean(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("Scene", "")
                .Replace("scene", "")
                .Trim();
        }

        string cleanCurrent = Clean(currentScene);

        if (!string.IsNullOrEmpty(sceneId))
        {
            string cleanTarget = Clean(sceneId);
            if (string.Equals(cleanCurrent, cleanTarget, StringComparison.OrdinalIgnoreCase) ||
                cleanCurrent.IndexOf(cleanTarget, StringComparison.OrdinalIgnoreCase) >= 0 ||
                cleanTarget.IndexOf(cleanCurrent, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        if (!string.IsNullOrEmpty(moduleId))
        {
            string cleanModule = Clean(moduleId);
            if (string.Equals(cleanCurrent, cleanModule, StringComparison.OrdinalIgnoreCase) ||
                cleanCurrent.IndexOf(cleanModule, StringComparison.OrdinalIgnoreCase) >= 0 ||
                cleanModule.IndexOf(cleanCurrent, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
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