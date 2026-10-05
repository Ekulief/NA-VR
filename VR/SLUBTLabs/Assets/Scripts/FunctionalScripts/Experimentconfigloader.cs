using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using System.Threading.Tasks;

/// <summary>
/// SLUBT Labs — Experiment Config Loader
/// 
/// Flow:
/// 1. Wait for Firebase
/// 2. Get this headset's unique device ID
/// 3. Look it up in the vrDevices collection → get the friendly vrId (e.g. "VR-01")
/// 4. Find the matching experimentProgress document
/// 5. Read studentId, groupId, experimentId
/// 6. Fetch the experiment document and apply its "configuration"
/// 7. Set IsReady = true
/// </summary>
public class ExperimentConfigLoader : MonoBehaviour
{
    [Tooltip("Drag your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    [Tooltip("Fallback vrId used only if the device is not found in the vrDevices collection.")]
    public string fallbackVrId = "VR-01";

    public static ExperimentConfig Current { get; private set; }
    public static bool IsReady { get; private set; } = false;

    // The friendly vrId that was resolved for this headset
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
        {
            Debug.LogError($"[ConfigLoader] Failed to load config: {task.Exception}");
        }

        IsReady = true;
        Debug.Log($"[ConfigLoader] Config ready. Resolved vrId = {ResolvedVrId}");
    }

    private async Task FetchProgressAndConfigAsync()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // ─────────────────────────────────────────────
        // STEP 1: Get this headset's unique hardware ID
        // ─────────────────────────────────────────────
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log($"[ConfigLoader] This device hardware ID = {deviceId}");

        // ─────────────────────────────────────────────
        // STEP 2: Look up the device in vrDevices collection
        // ─────────────────────────────────────────────
        string vrId = fallbackVrId; // default

        Query deviceQuery = db.Collection("vrDevices")
                              .WhereEqualTo("deviceId", deviceId)
                              .Limit(1);

        QuerySnapshot deviceSnap = await deviceQuery.GetSnapshotAsync();

        if (deviceSnap.Count > 0)
        {
            DocumentSnapshot deviceDoc = null;
            foreach (var doc in deviceSnap.Documents)
            {
                deviceDoc = doc;
                break;
            }

            if (deviceDoc != null && deviceDoc.ContainsField("vrId"))
            {
                vrId = deviceDoc.GetValue<string>("vrId");
                Debug.Log($"[ConfigLoader] Found matching device → assigned vrId = {vrId}");
            }
            else
            {
                Debug.LogWarning("[ConfigLoader] Device found but has no 'vrId' field. Using fallback.");
            }
        }
        else
        {
            Debug.LogWarning($"[ConfigLoader] No matching device found in vrDevices for deviceId={deviceId}. " +
                             $"Using fallback vrId = {fallbackVrId}");
        }

        ResolvedVrId = vrId;

        // ─────────────────────────────────────────────
        // STEP 3: Find the experimentProgress document
        // ─────────────────────────────────────────────
        Query progressQuery = db.Collection("experimentProgress")
                                .WhereEqualTo("vrId", vrId)
                                .Limit(1);

        QuerySnapshot progressSnap = await progressQuery.GetSnapshotAsync();

        if (progressSnap.Count == 0)
        {
            Debug.LogWarning($"[ConfigLoader] No experimentProgress found for vrId={vrId}. Using local values.");
            return;
        }

        DocumentSnapshot progressDoc = null;
        foreach (var doc in progressSnap.Documents)
        {
            progressDoc = doc;
            break;
        }

        if (progressDoc == null)
        {
            Debug.LogWarning("[ConfigLoader] No progress document found.");
            return;
        }

        // ─────────────────────────────────────────────
        // STEP 4: Extract studentId, groupId, experimentId
        // ─────────────────────────────────────────────
        string studentId = progressDoc.ContainsField("studentId") ? progressDoc.GetValue<string>("studentId") : "";
        string groupId = progressDoc.ContainsField("groupId") ? progressDoc.GetValue<string>("groupId") : "";
        string experimentId = progressDoc.ContainsField("experimentId") ? progressDoc.GetValue<string>("experimentId") : "";

        config.studentId = studentId;
        config.groupId = groupId;

        Debug.Log($"[ConfigLoader] Progress loaded → studentId={studentId}, groupId={groupId}, experimentId={experimentId}");

        if (string.IsNullOrEmpty(experimentId))
        {
            Debug.LogWarning("[ConfigLoader] experimentId is empty. Cannot load configuration.");
            return;
        }

        // ─────────────────────────────────────────────
        // STEP 5: Fetch the experiment document
        // ─────────────────────────────────────────────
        DocumentSnapshot experimentDoc = await db.Collection("experiment").Document(experimentId).GetSnapshotAsync();

        if (!experimentDoc.Exists)
        {
            Debug.LogWarning($"[ConfigLoader] experiment/{experimentId} does not exist.");
            return;
        }

        // ─────────────────────────────────────────────
        // STEP 6: Apply the nested "configuration" map
        // ─────────────────────────────────────────────
        if (experimentDoc.TryGetValue("configuration", out Dictionary<string, object> configMap))
        {
            ApplyConfigurationMap(configMap);
            Debug.Log("[ConfigLoader] Configuration applied from experiment document.");
        }
        else
        {
            Debug.LogWarning("[ConfigLoader] No 'configuration' field found in experiment document.");
        }
    }

    private void ApplyConfigurationMap(Dictionary<string, object> map)
    {
        float GetFloat(string key, float fallback) =>
            map.TryGetValue(key, out object v) ? System.Convert.ToSingle(v) : fallback;

        bool GetBool(string key, bool fallback) =>
            map.TryGetValue(key, out object v) ? System.Convert.ToBoolean(v) : fallback;

        string GetString(string key, string fallback) =>
            map.TryGetValue(key, out object v) ? v.ToString() : fallback;

        // General
        config.globalInstructionDelay = GetFloat("globalInstructionDelay", config.globalInstructionDelay);

        // Depth Perception (Height)
        config.depth_MaxHeightMetres = GetFloat("depth_MaxHeightMetres", config.depth_MaxHeightMetres);
        config.depth_ActualHeightMetres = GetFloat("depth_ActualHeightMetres", config.depth_ActualHeightMetres);
        config.depth_StepAmount = GetFloat("depth_StepAmount", config.depth_StepAmount);
        config.depth_InstructionText = GetString("depth_InstructionText", config.depth_InstructionText);

        // Depth Perception 2 (Distance)
        config.depth_MinDistanceMeters = GetFloat("depth_MinDistanceMeters", config.depth_MinDistanceMeters);
        config.depth_MaxDistanceMeters = GetFloat("depth_MaxDistanceMeters", config.depth_MaxDistanceMeters);
        config.depth_ActualDistanceMeters = GetFloat("depth_ActualDistanceMeters", config.depth_ActualDistanceMeters);
        config.depth_InstructionText2 = GetString("depth_InstructionText", config.depth_InstructionText2);

        // Attentional Blindness
        config.ab_FadeDelaySeconds = GetFloat("ab_FadeDelaySeconds", config.ab_FadeDelaySeconds);
        config.ab_FadeDurationSeconds = GetFloat("ab_FadeDurationSeconds", config.ab_FadeDurationSeconds);
        config.ab_UseRandomFadeTarget = GetBool("ab_UseRandomFadeTarget", config.ab_UseRandomFadeTarget);
        config.ab_InstructionText = GetString("ab_InstructionText", config.ab_InstructionText);
        config.ab_AwarenessQuestionText = GetString("ab_AwarenessQuestionText", config.ab_AwarenessQuestionText);

        // Odd Item
        config.oddItem_SearchTimeLimitSeconds = GetFloat("oddItem_SearchTimeLimitSeconds", config.oddItem_SearchTimeLimitSeconds);
        config.oddItem_RaycastDistance = GetFloat("oddItem_RaycastDistance", config.oddItem_RaycastDistance);
        config.oddItem_InstructionText = GetString("oddItem_InstructionText", config.oddItem_InstructionText);

        // Memory
        config.memory_TimePerRoomSeconds = GetFloat("memory_TimePerRoomSeconds", config.memory_TimePerRoomSeconds);
        config.memory_DistractorTaskDuration = GetFloat("memory_DistractorTaskDuration", config.memory_DistractorTaskDuration);
        config.memory_UseDistractorTask = GetBool("memory_UseDistractorTask", config.memory_UseDistractorTask);
        config.memory_BriefingText = GetString("memory_BriefingText", config.memory_BriefingText);
        config.memory_RoomInstructionText = GetString("memory_RoomInstructionText", config.memory_RoomInstructionText);
        config.memory_DistractorInstructionText = GetString("memory_DistractorInstructionText", config.memory_DistractorInstructionText);
        config.memory_RecallInstructionText = GetString("memory_RecallInstructionText", config.memory_RecallInstructionText);
    }
}