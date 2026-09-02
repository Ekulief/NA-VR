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

    // ── Odd Item Detection (Attentional Blindness) ────────────────────────────
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
        // TODO: Replace with Firebase Remote Config implementation
        // Example pattern:
        //   var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        //   await remoteConfig.FetchAndActivateAsync();
        //   oddItem_SearchTimeLimitSeconds = (float)remoteConfig.GetValue("oddItem_SearchTimeLimitSeconds").DoubleValue;
        //   rt_TotalTrials = (int)remoteConfig.GetValue("rt_TotalTrials").LongValue;
        //   ... etc for all fields

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