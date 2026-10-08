using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Experiment Config
/// Central configuration asset for all experiments.
/// Connects directly to Firebase Cloud Firestore to fetch live module parameters.
/// </summary>
[CreateAssetMenu(fileName = "ExperimentConfig", menuName = "SLUBT Labs/Experiment Config")]
public class ExperimentConfig : ScriptableObject
{
    // ── General ───────────────────────────────────────────────────────────────
    [Header("General")]
    public string studentId;
    public string groupId;
    public string blockId;
    public string experimentId;
    public string vrId;
    public string progressId;
    public float globalInstructionDelay = 1.5f;

    // ── Attentional Blindness (Furniture Counting) ───────────────────────────
    [Header("Attentional Blindness")]
    public float ab_FadeDelaySeconds = 8f;
    public float ab_FadeDurationSeconds = 3f;
    public float ab_PostFadePauseSeconds = 2f;
    public bool ab_UseRandomFadeTarget = true;

    [TextArea(2, 4)]
    public string ab_InstructionText = "<b>Count the furniture</b>\n\nWalk around the scene and count how many\nfurniture items you can see.\n\nPress <b>Start Counting</b> when you are ready.";

    [TextArea(2, 4)]
    public string ab_AwarenessQuestionText = "While counting the furniture,\ndid you notice anything unusual\nhappening in the scene?";

    [TextArea(2, 3)]
    public string ab_NoticedText = "You noticed the furniture item fading —\nyour attention was broadly distributed.";

    [TextArea(2, 3)]
    public string ab_NotNoticedText = "You did not notice the fading item.\nThis is the inattentional blindness effect.";

    // ── Odd Item Detection ───────────────────────────────────────────────────
    [Header("Odd Item Detection")]
    public string oddItem_TargetDisplayName = "the odd item";
    public float oddItem_SearchTimeLimitSeconds = 120f;

    [TextArea(2, 4)]
    public string oddItem_InstructionText = "Find the item that doesn't belong on the shelves.\n\nPoint at it and pull the trigger to confirm.";

    public string oddItem_WrongItemFeedback = "That item belongs here. Keep looking!";
    public float oddItem_RaycastDistance = 10f;

    [Header("Odd Item Detection — Time Ratings")]
    public float oddItem_ExcellentThresholdSeconds = 10f;
    public float oddItem_GoodThresholdSeconds = 20f;
    public string oddItem_RatingExcellent = "Excellent!";
    public string oddItem_RatingGood = "Good";
    public string oddItem_RatingKeepPracticing = "Keep Practicing";

    // ── Depth Perception ──────────────────────────────────────────────────────
    [Header("Depth Perception")]
    public float depth_MaxHeightMetres = 100f;
    public float depth_StepAmount = 1f;

    [TextArea(2, 4)]
    public string depth_InstructionText = "You are standing on top of a building.\nHow high up do you think you are?\n\nUse the +/- buttons or slider to input your estimate.";

    public float depth_ActualHeightMetres = 50f;

    // ── Depth Perception 2 (Horizontal Distance) ─────────────────────────────
    [Header("Depth Perception 2 — Car Subject Distance Bounds")]
    public float depth_MinDistanceMeters = 0f;
    public float depth_MaxDistanceMeters = 115f;
    public float depth_ActualDistanceMeters = 50f;

    [TextArea(2, 4)]
    public string depth_InstructionText2 = "Observe the car ahead and estimate its horizontal distance in meters.";

    // ── Reaction Time ─────────────────────────────────────────────────────────
    [Header("Reaction Time")]
    [Range(5, 40)] public int rt_TotalTrials = 10;
    [Range(0.5f, 2f)] public float rt_MinForeperiodSeconds = 1.0f;
    [Range(2f, 5f)] public float rt_MaxForeperiodSeconds = 3.5f;
    [Range(1f, 5f)] public float rt_StimulusTimeoutSeconds = 3.0f;
    [Range(0.5f, 2f)] public float rt_InterTrialIntervalSeconds = 1.0f;

    [TextArea(2, 4)]
    public string rt_InstructionText = "Press the trigger when you see the green panel.\n\nPress trigger to begin.";

    // ── Shelf Spawner ─────────────────────────────────────────────────────────
    [Header("Shelf Spawner")]
    public int shelf_Columns = 5;
    public int shelf_Rows = 2;
    public float shelf_RandomPositionOffset = 0.04f;
    public float shelf_RandomRotationOffset = 15f;

    // ── Memory Experiment ─────────────────────────────────────────────────────
    [Header("Memory Experiment")]
    public float memory_TimePerRoomSeconds = 60f;
    public float memory_TransitionFadeDuration = 0.5f;
    public float memory_DistractorTaskDuration = 30f;
    public bool memory_UseDistractorTask = true;
    public bool memory_RandomizeQuestions = true;

    [Header("Memory – Remote Targets (from Firestore)")]
    public List<MemoryTargetEntry> memory_Targets = new List<MemoryTargetEntry>();

    [TextArea(2, 4)]
    public string memory_BriefingText =
        "<b>Memory Experiment</b>\n\n" +
        "You will explore several rooms.\n" +
        "Pay close attention to the objects and details in each room.\n\n" +
        "Press <b>Start</b> when you are ready.";

    [TextArea(2, 4)]
    public string memory_RoomInstructionText =
        "Explore this room carefully.\n" +
        "Try to remember as many objects and details as you can.";

    [TextArea(2, 4)]
    public string memory_DistractorInstructionText =
        "Before we continue, please count backwards from 100 by 3s.\n\n" +
        "Say each number <b>aloud</b>.";

    [TextArea(2, 4)]
    public string memory_RecallInstructionText =
        "Now you will be asked questions about the rooms you explored.\n\n" +
        "Answer as accurately as you can.";

    // ── Web App Integration ───────────────────────────────────────────────────
    [Header("Web App Integration")]
    [Tooltip("If enabled, configuration parameters are fetched directly from Firestore at startup.")]
    public bool useRemoteConfig = true;

    // ── Runtime Methods ───────────────────────────────────────────────────────

    public async Task FetchFromFirestoreAsync()
    {
        if (!useRemoteConfig)
        {
            Debug.Log("[ExperimentConfig] Firestore synchronization disabled. Using local inspector values.");
            return;
        }

        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            // 1. Attentional Blindness
            DocumentReference abRef = db.Collection("experimentModule").Document("Attentional_Blindness");
            DocumentSnapshot abSnapshot = await abRef.GetSnapshotAsync();
            if (abSnapshot.Exists) ApplyAttentionalBlindnessValues(abSnapshot);

            // 2. Odd Item Detection
            DocumentReference oddItemRef = db.Collection("experimentModule").Document("Odd_Item_Detection");
            DocumentSnapshot oddItemSnapshot = await oddItemRef.GetSnapshotAsync();
            if (oddItemSnapshot.Exists) ApplyOddItemValues(oddItemSnapshot);

            // 3. Depth Perception
            DocumentReference depthRef = db.Collection("experimentModule").Document("Depth_Perception");
            DocumentSnapshot depthSnapshot = await depthRef.GetSnapshotAsync();
            if (depthSnapshot.Exists) ApplyDepthPerceptionValues(depthSnapshot);

            // 4. Depth Perception 2
            DocumentReference depth2Ref = db.Collection("experimentModule").Document("Depth_Perception2");
            DocumentSnapshot depth2Snapshot = await depth2Ref.GetSnapshotAsync();
            if (depth2Snapshot.Exists) ApplyDepthPerception2Values(depth2Snapshot);

            // 5. Memory
            DocumentReference memoryRef = db.Collection("experimentModule").Document("Memory2");
            DocumentSnapshot memorySnapshot = await memoryRef.GetSnapshotAsync();
            if (memorySnapshot.Exists)
            {
                ApplyMemoryValues(memorySnapshot);
                Debug.Log("[ExperimentConfig] Memory values updated from Firestore.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExperimentConfig] Error fetching parameters from Firestore: {ex.Message}");
        }
    }

    public void ApplyConfigurationMap(Dictionary<string, object> configMap)
    {
        if (configMap == null) return;

        float GetFloat(string key, float fallback) =>
            configMap.TryGetValue(key, out object v) ? Convert.ToSingle(v) : fallback;
        bool GetBool(string key, bool fallback) =>
            configMap.TryGetValue(key, out object v) ? Convert.ToBoolean(v) : fallback;
        string GetString(string key, string fallback) =>
            configMap.TryGetValue(key, out object v) ? v.ToString() : fallback;

        globalInstructionDelay = GetFloat("globalInstructionDelay", globalInstructionDelay);

        // Depth (Height)
        depth_MaxHeightMetres = GetFloat("depth_MaxHeightMetres", depth_MaxHeightMetres);
        depth_ActualHeightMetres = GetFloat("depth_ActualHeightMetres", depth_ActualHeightMetres);
        depth_StepAmount = GetFloat("depth_StepAmount", depth_StepAmount);
        depth_InstructionText = GetString("depth_InstructionText", depth_InstructionText);

        // Depth 2 / Distance Perception
        depth_MinDistanceMeters = Mathf.Clamp(GetFloat("distance_MinDistanceMeters", GetFloat("depth_MinDistanceMeters", depth_MinDistanceMeters)), 0f, 115f);
        depth_MaxDistanceMeters = Mathf.Clamp(GetFloat("distance_MaxDistanceMeters", GetFloat("depth_MaxDistanceMeters", depth_MaxDistanceMeters)), 0f, 115f);
        depth_ActualDistanceMeters = Mathf.Clamp(GetFloat("distance_ActualDistanceMeters", GetFloat("depth_ActualDistanceMeters", depth_ActualDistanceMeters)), depth_MinDistanceMeters, depth_MaxDistanceMeters);
        depth_InstructionText2 = GetString("distance_InstructionText", GetString("depth_InstructionText2", depth_InstructionText2));

        // Attentional Blindness
        ab_FadeDelaySeconds = GetFloat("ab_FadeDelaySeconds", ab_FadeDelaySeconds);
        ab_FadeDurationSeconds = GetFloat("ab_FadeDurationSeconds", ab_FadeDurationSeconds);
        ab_PostFadePauseSeconds = GetFloat("ab_PostFadePauseSeconds", ab_PostFadePauseSeconds);
        ab_UseRandomFadeTarget = GetBool("ab_UseRandomFadeTarget", ab_UseRandomFadeTarget);
        ab_InstructionText = GetString("ab_InstructionText", ab_InstructionText);
        ab_AwarenessQuestionText = GetString("ab_AwarenessQuestionText", ab_AwarenessQuestionText);
        ab_NoticedText = GetString("ab_NoticedText", ab_NoticedText);
        ab_NotNoticedText = GetString("ab_NotNoticedText", ab_NotNoticedText);

        // Odd Item
        oddItem_SearchTimeLimitSeconds = GetFloat("oddItem_SearchTimeLimitSeconds", oddItem_SearchTimeLimitSeconds);
        oddItem_RaycastDistance = GetFloat("oddItem_RaycastDistance", oddItem_RaycastDistance);
        oddItem_InstructionText = GetString("oddItem_InstructionText", oddItem_InstructionText);
        oddItem_TargetDisplayName = GetString("oddItem_TargetDisplayName", oddItem_TargetDisplayName);
        oddItem_WrongItemFeedback = GetString("oddItem_WrongItemFeedback", oddItem_WrongItemFeedback);
        oddItem_ExcellentThresholdSeconds = GetFloat("oddItem_ExcellentThresholdSeconds", oddItem_ExcellentThresholdSeconds);
        oddItem_GoodThresholdSeconds = GetFloat("oddItem_GoodThresholdSeconds", oddItem_GoodThresholdSeconds);
        oddItem_RatingExcellent = GetString("oddItem_RatingExcellent", oddItem_RatingExcellent);
        oddItem_RatingGood = GetString("oddItem_RatingGood", oddItem_RatingGood);
        oddItem_RatingKeepPracticing = GetString("oddItem_RatingKeepPracticing", oddItem_RatingKeepPracticing);

        // Memory
        memory_TimePerRoomSeconds = GetFloat("memory_TimePerRoomSeconds", memory_TimePerRoomSeconds);
        memory_TransitionFadeDuration = GetFloat("memory_TransitionFadeDuration", memory_TransitionFadeDuration);
        memory_DistractorTaskDuration = GetFloat("memory_DistractorTaskDuration", memory_DistractorTaskDuration);
        memory_UseDistractorTask = GetBool("memory_UseDistractorTask", memory_UseDistractorTask);
        memory_RandomizeQuestions = GetBool("memory_RandomizeQuestions", memory_RandomizeQuestions);
        memory_BriefingText = GetString("memory_BriefingText", memory_BriefingText);
        memory_RoomInstructionText = GetString("memory_RoomInstructionText", memory_RoomInstructionText);
        memory_DistractorInstructionText = GetString("memory_DistractorInstructionText", memory_DistractorInstructionText);
        memory_RecallInstructionText = GetString("memory_RecallInstructionText", memory_RecallInstructionText);

        if (configMap.TryGetValue("targets", out object targetsObj))
        {
            ParseMemoryTargets(targetsObj);
        }
    }

    private void ApplyAttentionalBlindnessValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;
        ApplyConfigurationMap(configMap);
    }

    private void ApplyOddItemValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;
        ApplyConfigurationMap(configMap);
    }

    private void ApplyDepthPerceptionValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;
        ApplyConfigurationMap(configMap);
    }

    private void ApplyDepthPerception2Values(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;
        ApplyConfigurationMap(configMap);
    }

    private void ApplyMemoryValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap))
        {
            Debug.LogWarning("[ExperimentConfig] 'defaultConfig' field missing in Memory2 document.");
            return;
        }

        ApplyConfigurationMap(configMap);
    }

    private void ParseMemoryTargets(object targetsObj)
    {
        if (targetsObj is not List<object> targetsList) return;

        memory_Targets.Clear();

        foreach (object targetObj in targetsList)
        {
            if (targetObj is not Dictionary<string, object> targetMap) continue;

            MemoryTargetEntry entry = new MemoryTargetEntry();

            if (targetMap.TryGetValue("gameID", out object id)) entry.gameID = id.ToString();
            if (targetMap.TryGetValue("objectName", out object name)) entry.objectName = name.ToString();
            if (targetMap.TryGetValue("room", out object room)) entry.room = room.ToString();

            if (targetMap.TryGetValue("questions", out object questionsObj) &&
                questionsObj is List<object> questionsList)
            {
                List<QuestionData> qList = new List<QuestionData>();

                foreach (object qObj in questionsList)
                {
                    if (qObj is not Dictionary<string, object> qMap) continue;

                    QuestionData q = new QuestionData();

                    if (qMap.TryGetValue("questionText", out object qt)) q.questionText = qt.ToString();
                    if (qMap.TryGetValue("correctAnswer", out object ca)) q.correctAnswer = ca.ToString();

                    if (qMap.TryGetValue("questionType", out object typeObj) &&
                        Enum.TryParse(typeObj.ToString(), true, out QuestionType parsedType))
                    {
                        q.questionType = parsedType;
                    }

                    if (qMap.TryGetValue("multipleChoiceOptions", out object optsObj) &&
                        optsObj is List<object> optsList)
                    {
                        q.multipleChoiceOptions = optsList.ConvertAll(o => o.ToString()).ToArray();
                    }
                    else
                    {
                        q.multipleChoiceOptions = new string[0];
                    }

                    qList.Add(q);
                }

                entry.questions = qList.ToArray();
            }

            memory_Targets.Add(entry);
        }

        Debug.Log($"[ExperimentConfig] Parsed {memory_Targets.Count} memory targets.");
    }

    public void ApplyFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        Debug.Log("[ExperimentConfig] Config updated from JSON payload.");
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, prettyPrint: true);
    }
}