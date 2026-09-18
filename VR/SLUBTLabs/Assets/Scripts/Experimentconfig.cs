using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

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

    // ── Web App Integration ───────────────────────────────────────────────────
    [Header("Web App Integration")]
    [Tooltip("If enabled, configuration parameters are fetched directly from Firestore at startup.")]
    public bool useRemoteConfig = true;

    // ── Runtime Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Async fetch method querying your Firestore collections and updating config fields live.
    /// </summary>
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

            // 1. Fetch Attentional Blindness Document
            DocumentReference abRef = db.Collection("experimentModule").Document("Attentional_Blindness");
            DocumentSnapshot abSnapshot = await abRef.GetSnapshotAsync();

            if (abSnapshot.Exists)
            {
                ApplyAttentionalBlindnessValues(abSnapshot);
                Debug.Log("[ExperimentConfig] Attentional Blindness values updated from Firestore.");
            }

            // 2. Fetch Odd Item Detection Document
            DocumentReference oddItemRef = db.Collection("experimentModule").Document("Odd_Item_Detection");
            DocumentSnapshot oddItemSnapshot = await oddItemRef.GetSnapshotAsync();

            if (oddItemSnapshot.Exists)
            {
                ApplyOddItemValues(oddItemSnapshot);
                Debug.Log("[ExperimentConfig] Odd Item Detection values updated from Firestore.");
            }

            // 3. Fetch Depth Perception Document
            DocumentReference depthRef = db.Collection("experimentModule").Document("Depth_Perception");
            DocumentSnapshot depthSnapshot = await depthRef.GetSnapshotAsync();

            if (depthSnapshot.Exists)
            {
                ApplyDepthPerceptionValues(depthSnapshot);
                Debug.Log("[ExperimentConfig] Depth Perception values updated from Firestore.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExperimentConfig] Error fetching parameters from Firestore: {ex.Message}");
        }
    }

    private void ApplyAttentionalBlindnessValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap))
        {
            Debug.LogWarning("[ExperimentConfig] 'defaultConfig' field missing in Attentional_Blindness document.");
            return;
        }

        if (configMap.TryGetValue("ab_InstructionText", out object abInstruction))
            ab_InstructionText = abInstruction.ToString();

        if (configMap.TryGetValue("ab_AwarenessQuestionText", out object abAwareness))
            ab_AwarenessQuestionText = abAwareness.ToString();

        if (configMap.TryGetValue("ab_NoticedText", out object abNoticed))
            ab_NoticedText = abNoticed.ToString();

        if (configMap.TryGetValue("ab_NotNoticedText", out object abNotNoticed))
            ab_NotNoticedText = abNotNoticed.ToString();

        if (configMap.TryGetValue("ab_FadeDelaySeconds", out object abFadeDelay))
            ab_FadeDelaySeconds = Convert.ToSingle(abFadeDelay);

        if (configMap.TryGetValue("ab_FadeDurationSeconds", out object abFadeDuration))
            ab_FadeDurationSeconds = Convert.ToSingle(abFadeDuration);

        if (configMap.TryGetValue("ab_PostFadePauseSeconds", out object abPostFade))
            ab_PostFadePauseSeconds = Convert.ToSingle(abPostFade);

        if (configMap.TryGetValue("globalInstructionDelay", out object globalDelay))
            globalInstructionDelay = Convert.ToSingle(globalDelay);

        if (configMap.TryGetValue("ab_UseRandomFadeTarget", out object abRandomTarget))
            ab_UseRandomFadeTarget = Convert.ToBoolean(abRandomTarget);
    }

    private void ApplyOddItemValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap))
        {
            Debug.LogWarning("[ExperimentConfig] 'defaultConfig' field missing in Odd_Item_Detection document.");
            return;
        }

        if (configMap.TryGetValue("oddItem_InstructionText", out object oddInstruction))
            oddItem_InstructionText = oddInstruction.ToString();

        if (configMap.TryGetValue("oddItem_TargetDisplayName", out object oddTarget))
            oddItem_TargetDisplayName = oddTarget.ToString();

        if (configMap.TryGetValue("oddItem_WrongItemFeedback", out object oddFeedback))
            oddItem_WrongItemFeedback = oddFeedback.ToString();

        if (configMap.TryGetValue("oddItem_SearchTimeLimitSeconds", out object oddSearchTime))
            oddItem_SearchTimeLimitSeconds = Convert.ToSingle(oddSearchTime);

        if (configMap.TryGetValue("oddItem_RaycastDistance", out object oddRaycast))
            oddItem_RaycastDistance = Convert.ToSingle(oddRaycast);
    }

    private void ApplyDepthPerceptionValues(DocumentSnapshot snap)
    {
        if (!snap.TryGetValue("defaultConfig", out Dictionary<string, object> configMap))
        {
            Debug.LogWarning("[ExperimentConfig] 'defaultConfig' field missing in Depth_Perception document.");
            return;
        }

        if (configMap.TryGetValue("depth_InstructionText", out object depthInstruction))
            depth_InstructionText = depthInstruction.ToString();

        if (configMap.TryGetValue("depth_MaxHeightMetres", out object depthMaxHeight))
            depth_MaxHeightMetres = Convert.ToSingle(depthMaxHeight);

        if (configMap.TryGetValue("depth_ActualHeightMetres", out object depthActualHeight))
            depth_ActualHeightMetres = Convert.ToSingle(depthActualHeight);
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