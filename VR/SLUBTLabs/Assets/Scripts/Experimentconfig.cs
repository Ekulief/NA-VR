using Firebase.Firestore;
using System;
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
    public string sessionId = "session-001";
    public string participantId = "participant-001";
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

            // 5. Memory (NEW)
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

    private void ApplyAttentionalBlindnessValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;

        if (configMap.TryGetValue("ab_InstructionText", out object v)) ab_InstructionText = v.ToString();
        if (configMap.TryGetValue("ab_AwarenessQuestionText", out object v2)) ab_AwarenessQuestionText = v2.ToString();
        if (configMap.TryGetValue("ab_NoticedText", out object v3)) ab_NoticedText = v3.ToString();
        if (configMap.TryGetValue("ab_NotNoticedText", out object v4)) ab_NotNoticedText = v4.ToString();
        if (configMap.TryGetValue("ab_FadeDelaySeconds", out object v5)) ab_FadeDelaySeconds = Convert.ToSingle(v5);
        if (configMap.TryGetValue("ab_FadeDurationSeconds", out object v6)) ab_FadeDurationSeconds = Convert.ToSingle(v6);
        if (configMap.TryGetValue("ab_PostFadePauseSeconds", out object v7)) ab_PostFadePauseSeconds = Convert.ToSingle(v7);
        if (configMap.TryGetValue("globalInstructionDelay", out object v8)) globalInstructionDelay = Convert.ToSingle(v8);
        if (configMap.TryGetValue("ab_UseRandomFadeTarget", out object v9)) ab_UseRandomFadeTarget = Convert.ToBoolean(v9);
    }

    private void ApplyOddItemValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;

        if (configMap.TryGetValue("oddItem_InstructionText", out object v)) oddItem_InstructionText = v.ToString();
        if (configMap.TryGetValue("oddItem_TargetDisplayName", out object v2)) oddItem_TargetDisplayName = v2.ToString();
        if (configMap.TryGetValue("oddItem_WrongItemFeedback", out object v3)) oddItem_WrongItemFeedback = v3.ToString();
        if (configMap.TryGetValue("oddItem_SearchTimeLimitSeconds", out object v4)) oddItem_SearchTimeLimitSeconds = Convert.ToSingle(v4);
        if (configMap.TryGetValue("oddItem_RaycastDistance", out object v5)) oddItem_RaycastDistance = Convert.ToSingle(v5);
    }

    private void ApplyDepthPerceptionValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;

        if (configMap.TryGetValue("depth_InstructionText", out object v)) depth_InstructionText = v.ToString();
        if (configMap.TryGetValue("depth_MaxHeightMetres", out object v2)) depth_MaxHeightMetres = Convert.ToSingle(v2);
        if (configMap.TryGetValue("depth_ActualHeightMetres", out object v3)) depth_ActualHeightMetres = Convert.ToSingle(v3);
        if (configMap.TryGetValue("depth_StepAmount", out object v4)) depth_StepAmount = Convert.ToSingle(v4);
    }

    private void ApplyDepthPerception2Values(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap)) return;

        if (configMap.TryGetValue("depth_InstructionText", out object v)) depth_InstructionText2 = v.ToString();
        if (configMap.TryGetValue("depth_MinDistanceMeters", out object v2)) depth_MinDistanceMeters = Mathf.Clamp(Convert.ToSingle(v2), 0f, 115f);
        if (configMap.TryGetValue("depth_MaxDistanceMeters", out object v3)) depth_MaxDistanceMeters = Mathf.Clamp(Convert.ToSingle(v3), 0f, 115f);
        if (configMap.TryGetValue("depth_ActualDistanceMeters", out object v4)) depth_ActualDistanceMeters = Mathf.Clamp(Convert.ToSingle(v4), depth_MinDistanceMeters, depth_MaxDistanceMeters);
        if (configMap.TryGetValue("depth_StepAmount", out object v5)) depth_StepAmount = Convert.ToSingle(v5);
    }

    private void ApplyMemoryValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap))
        {
            Debug.LogWarning("[ExperimentConfig] 'defaultConfig' field missing in Memory2 document.");
            return;
        }

        // Timing & settings
        if (configMap.TryGetValue("memory_TimePerRoomSeconds", out object v)) memory_TimePerRoomSeconds = Convert.ToSingle(v);
        if (configMap.TryGetValue("memory_TransitionFadeDuration", out object v2)) memory_TransitionFadeDuration = Convert.ToSingle(v2);
        if (configMap.TryGetValue("memory_DistractorTaskDuration", out object v3)) memory_DistractorTaskDuration = Convert.ToSingle(v3);
        if (configMap.TryGetValue("memory_UseDistractorTask", out object v4)) memory_UseDistractorTask = Convert.ToBoolean(v4);
        if (configMap.TryGetValue("memory_RandomizeQuestions", out object v5)) memory_RandomizeQuestions = Convert.ToBoolean(v5);

        // Texts
        if (configMap.TryGetValue("memory_BriefingText", out object v6)) memory_BriefingText = v6.ToString();
        if (configMap.TryGetValue("memory_RoomInstructionText", out object v7)) memory_RoomInstructionText = v7.ToString();
        if (configMap.TryGetValue("memory_DistractorInstructionText", out object v8)) memory_DistractorInstructionText = v8.ToString();
        if (configMap.TryGetValue("memory_RecallInstructionText", out object v9)) memory_RecallInstructionText = v9.ToString();

        if (configMap.TryGetValue("globalInstructionDelay", out object v10)) globalInstructionDelay = Convert.ToSingle(v10);

        // ── Targets (objects + questions) ─────────────────────────────────────
        if (snap.TryGetValue("targets", out object targetsObj) && targetsObj is List<object> targetsList)
        {
            memory_Targets.Clear();

            foreach (object targetObj in targetsList)
            {
                if (targetObj is not Dictionary<string, object> targetMap) continue;

                MemoryTargetEntry entry = new MemoryTargetEntry();

                if (targetMap.TryGetValue("gameID", out object id)) entry.gameID = id.ToString();
                if (targetMap.TryGetValue("objectName", out object name)) entry.objectName = name.ToString();
                if (targetMap.TryGetValue("room", out object room)) entry.room = room.ToString();

                // Questions
                if (targetMap.TryGetValue("questions", out object questionsObj) && questionsObj is List<object> questionsList)
                {
                    List<QuestionData> qList = new List<QuestionData>();

                    foreach (object qObj in questionsList)
                    {
                        if (qObj is not Dictionary<string, object> qMap) continue;

                        QuestionData q = new QuestionData();

                        if (qMap.TryGetValue("questionText", out object qt)) q.questionText = qt.ToString();
                        if (qMap.TryGetValue("correctAnswer", out object ca)) q.correctAnswer = ca.ToString();

                        // questionType
                        if (qMap.TryGetValue("questionType", out object typeObj))
                        {
                            if (System.Enum.TryParse(typeObj.ToString(), true, out QuestionType parsedType))
                                q.questionType = parsedType;
                        }

                        // multipleChoiceOptions
                        if (qMap.TryGetValue("multipleChoiceOptions", out object optsObj) && optsObj is List<object> optsList)
                        {
                            q.multipleChoiceOptions = optsList.ConvertAll(o => o.ToString()).ToArray();
                        }
                        else
                        {
                            q.multipleChoiceOptions = new string[0];
                        }
                        // enabled flag 
                        if (qMap.TryGetValue("enabled", out object enObj))
                            q.enabled = Convert.ToBoolean(enObj);
                        else
                            q.enabled = true;
                        qList.Add(q);
                    }

                    entry.questions = qList.ToArray();
                }

                memory_Targets.Add(entry);
            }

            Debug.Log($"[ExperimentConfig] Loaded {memory_Targets.Count} remote targets from Firestore.");
        }
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