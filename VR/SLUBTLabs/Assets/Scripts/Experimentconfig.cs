using UnityEngine;

/// <summary>
/// SLUBT Labs — Experiment Config
/// Central configuration asset for all experiments.
/// Create one instance via Assets → Create → SLUBT Labs → Experiment Config
/// and assign it to each experiment manager in the Inspector.
///
/// WEB APP INTEGRATION (future):
///   Replace LoadFromFirebase() stub with actual Firebase Remote Config calls.
///   All parameters here map 1:1 to Firebase Remote Config keys.
///   The web dashboard sets these values, VR app fetches them at session start.
/// </summary>
[CreateAssetMenu(fileName = "ExperimentConfig", menuName = "SLUBT Labs/Experiment Config")]
public class ExperimentConfig : ScriptableObject
{
    // ── General ───────────────────────────────────────────────────────────────
    [Header("General")]
    [Tooltip("Instructor or session identifier — set from web app per session.")]
    public string sessionId = "session-001";

    [Tooltip("Student/participant identifier.")]
    public string participantId = "participant-001";

    [Tooltip("Seconds to wait after scene loads before experiment begins.")]
    public float globalInstructionDelay = 1.5f;

    // ── Attentional Blindness (Furniture Counting) ───────────────────────────
    [Header("Attentional Blindness")]
    [Tooltip("Seconds after counting starts before the target object begins fading.")]
    public float ab_FadeDelaySeconds = 8f;

    [Tooltip("Duration in seconds for the target object to complete fading to invisible.")]
    public float ab_FadeDurationSeconds = 3f;

    [Tooltip("Delay in seconds after the fade finishes before prompting the user for input.")]
    public float ab_PostFadePauseSeconds = 2f;

    [Tooltip("If true, picks a random furniture target. If false, relies on specificFadeTarget in Manager.")]
    public bool ab_UseRandomFadeTarget = true;

    [Tooltip("Instruction text shown before participant begins counting.")]
    [TextArea(2, 4)]
    public string ab_InstructionText = "<b>Count the furniture</b>\n\nWalk around the scene and count how many\nfurniture items you can see.\n\nPress <b>Start Counting</b> when you are ready.";

    [Tooltip("Question text asked when querying about the anomaly.")]
    [TextArea(2, 4)]
    public string ab_AwarenessQuestionText = "While counting the furniture,\ndid you notice anything unusual\nhappening in the scene?";

    [Tooltip("Result text shown when participant noticed the anomaly.")]
    [TextArea(2, 3)]
    public string ab_NoticedText = "You noticed the furniture item fading —\nyour attention was broadly distributed.";

    [Tooltip("Result text shown when participant did not notice the anomaly.")]
    [TextArea(2, 3)]
    public string ab_NotNoticedText = "You did not notice the fading item.\nThis is the inattentional blindness effect.";

    // ── Odd Item Detection ───────────────────────────────────────────────────
    [Header("Odd Item Detection")]
    [Tooltip("Display name of the odd item shown in the instruction text.")]
    public string oddItem_TargetDisplayName = "the odd item";

    [Tooltip("Maximum time in seconds the participant has to find the odd item. 0 = no limit.")]
    public float oddItem_SearchTimeLimitSeconds = 120f;

    [Tooltip("Instruction text shown to participant at start.")]
    [TextArea(2, 4)]
    public string oddItem_InstructionText = "Find the item that doesn't belong on the shelves.\n\nPoint at it and pull the trigger to confirm.";

    [Tooltip("Feedback shown when wrong item is selected.")]
    public string oddItem_WrongItemFeedback = "That item belongs here. Keep looking!";

    [Tooltip("Max ray distance for item selection.")]
    public float oddItem_RaycastDistance = 10f;

    [Header("Odd Item Detection — Time Ratings")]
    [Tooltip("Search time in seconds to get an Excellent rating.")]
    public float oddItem_ExcellentThresholdSeconds = 10f;

    [Tooltip("Search time in seconds to get a Good rating. Above this = Keep Practicing.")]
    public float oddItem_GoodThresholdSeconds = 20f;

    [Tooltip("Label shown when time is under Excellent threshold.")]
    public string oddItem_RatingExcellent = "Excellent!";

    [Tooltip("Label shown when time is between Excellent and Good thresholds.")]
    public string oddItem_RatingGood = "Good";

    [Tooltip("Label shown when time is above Good threshold.")]
    public string oddItem_RatingKeepPracticing = "Keep Practicing";

    // ── Depth Perception ──────────────────────────────────────────────────────
    [Header("Depth Perception")]
    [Tooltip("Maximum selectable height in metres.")]
    public float depth_MaxHeightMetres = 100f;

    [Tooltip("How much each +/- button press changes the value.")]
    public float depth_StepAmount = 1f;

    [Tooltip("Instruction shown to participant.")]
    [TextArea(2, 4)]
    public string depth_InstructionText = "You are standing on top of a building.\nHow high up do you think you are?\n\nUse the +/- buttons or slider to input your estimate.";

    [Tooltip("The actual height of the building in Unity world units — used to calculate error.")]
    public float depth_ActualHeightMetres = 50f;

    // ── Reaction Time ─────────────────────────────────────────────────────────
    [Header("Reaction Time")]
    [Tooltip("Total number of trials per session.")]
    [Range(5, 40)]
    public int rt_TotalTrials = 10;

    [Tooltip("Minimum foreperiod in seconds before stimulus appears.")]
    [Range(0.5f, 2f)]
    public float rt_MinForeperiodSeconds = 1.0f;

    [Tooltip("Maximum foreperiod in seconds.")]
    [Range(2f, 5f)]
    public float rt_MaxForeperiodSeconds = 3.5f;

    [Tooltip("Seconds the stimulus stays on if participant doesn't respond.")]
    [Range(1f, 5f)]
    public float rt_StimulusTimeoutSeconds = 3.0f;

    [Tooltip("Inter-trial interval in seconds.")]
    [Range(0.5f, 2f)]
    public float rt_InterTrialIntervalSeconds = 1.0f;

    [Tooltip("Instruction text shown before trials begin.")]
    [TextArea(2, 4)]
    public string rt_InstructionText = "Press the trigger when you see the green panel.\n\nPress trigger to begin.";

    // ── Shelf Spawner ─────────────────────────────────────────────────────────
    [Header("Shelf Spawner")]
    [Tooltip("Number of columns across each shelf.")]
    public int shelf_Columns = 5;

    [Tooltip("Number of rows on each shelf.")]
    public int shelf_Rows = 2;

    [Tooltip("Max random position offset so items don't look perfectly aligned.")]
    public float shelf_RandomPositionOffset = 0.04f;

    [Tooltip("Max random Y rotation per item.")]
    public float shelf_RandomRotationOffset = 15f;

    // ── Web App Integration ───────────────────────────────────────────────────
    [Header("Web App Integration")]
    [Tooltip("If true, config will be fetched from Firebase Remote Config at session start. " +
             "If false, values set in this asset are used directly.")]
    public bool useRemoteConfig = false;

    [Tooltip("Firebase Remote Config fetch timeout in seconds.")]
    public float remoteConfigTimeoutSeconds = 5f;

    // ── Runtime Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Stub for Firebase Remote Config integration.
    /// Replace body with actual Firebase SDK calls when ready.
    /// Called by ExperimentConfigLoader at scene start.
    /// </summary>
    public void LoadFromRemoteConfig()
    {
        Debug.Log("[ExperimentConfig] Remote config loaded (stub — using local values).");
    }

    /// <summary>
    /// Apply a JSON string from the web app to override config values at runtime.
    /// The web app sends a JSON payload, this method parses and applies it.
    /// </summary>
    public void ApplyFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        Debug.Log("[ExperimentConfig] Config updated from JSON payload.");
    }

    /// <summary>Export current config as JSON for the web dashboard.</summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, prettyPrint: true);
    }
}