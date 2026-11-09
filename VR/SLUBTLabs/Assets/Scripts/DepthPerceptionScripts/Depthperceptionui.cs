using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Depth / height estimation trial UI.
/// Waits for config, shows instructions, collects an estimate, then saves results.
/// </summary>
public class DepthPerceptionUI : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("UI — Instruction Panel")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;
    public Button startTestButton;
    [Tooltip("Seconds to wait after the player teleports via StartExperimentAnchor before showing the Start Test button.")]
    public float beginButtonAppearDelay = 2.5f;

    [Header("UI — Input Panel")]
    public GameObject inputPanel;
    public TMP_Text heightDisplayText;
    public TMP_Text heightPromptText;
    public Button incrementButton;
    public Button decrementButton;
    public Slider heightSlider;
    public Button submitButton;

    [Header("UI — Results Panel")]
    public GameObject resultsPanel;
    public TMP_Text resultsSummaryText;
    public Button returnHomeButton;

    [Header("Navigation")]
    public Vector3 hubReturnPosition = Vector3.zero;
    public Button[] returnHomeButtons;

    private float _currentHeight = 0f;
    private float _actualHeight;
    private float _maxHeightMetres = 100f;
    private float _stepAmount = 1f;
    private float _trialStartTime;
    private float _completionTime;
    private float _experimentStartRealtime;
    private DateTime _startedAtUtc;
    private bool _submitted = false;
    private ExperimentLoader _experimentLoader;

    [Header("Building Scaling (ExperimentArea only)")]
    [Tooltip("The main building whose height the student estimates (Building_I_2 under Building_I_2_Prefab (5))")]
    public Transform mainBuildingToScale;

    [Tooltip("Optional second / side building that should grow with the main building")]
    public Transform sideBuildingToScale;

    [Tooltip("All roof props / objects that must stay the same size and simply ride up with the roof")]
    public Transform[] roofPropsToReposition;

    [Tooltip("Height in metres that the current (unscaled) buildings represent")]
    public float referenceHeightMetres = 14f;

    [Tooltip("Height in metres that the side building's unscaled model represents")]
    public float sideReferenceHeightMetres = 13f;

    private Vector3 _mainBaseScale;
    private Vector3 _sideBaseScale;
    private Vector3[] _propBaseWorldPositions;
    private bool _scalingApplied;

    private TeleportationAnchor _startExperimentAnchor;

    // ------------------------------------------------------------------
    // Building scale + prop reposition
    // ------------------------------------------------------------------

    private void CacheBaseScalesAndPositions()
    {
        if (mainBuildingToScale != null)
            _mainBaseScale = mainBuildingToScale.localScale;

        if (sideBuildingToScale != null)
            _sideBaseScale = sideBuildingToScale.localScale;

        if (roofPropsToReposition != null && roofPropsToReposition.Length > 0)
        {
            _propBaseWorldPositions = new Vector3[roofPropsToReposition.Length];
            for (int i = 0; i < roofPropsToReposition.Length; i++)
            {
                if (roofPropsToReposition[i] != null)
                    _propBaseWorldPositions[i] = roofPropsToReposition[i].position; // world space
            }
        }
    }

    /// <summary>
    /// Scales the buildings and lifts the roof props by the same world-space height
    /// so they stay sitting on the roof. Props keep their original size.
    /// </summary>
    private void ApplyBuildingHeightScale(float actualHeightMetres)
    {
        if (_scalingApplied) return;

        if (referenceHeightMetres <= 0.01f)
            referenceHeightMetres = 14f;

        float mainScaleFactor = actualHeightMetres / referenceHeightMetres;

        // 1. Main building – local Z is the vertical axis in this prefab
        if (mainBuildingToScale != null)
        {
            Vector3 s = _mainBaseScale;
            s.z = _mainBaseScale.z * mainScaleFactor;
            mainBuildingToScale.localScale = s;
        }

        // 2. Side building – it is rotated -90° on Y, so local Z is horizontal.
        //    Scale local Y so it actually grows upward.
        if (sideBuildingToScale != null)
        {
            float sideRef = sideReferenceHeightMetres <= 0.01f ? 13f : sideReferenceHeightMetres;
            float sideScaleFactor = actualHeightMetres / sideRef;

            Vector3 s = _sideBaseScale;
            s.z = _sideBaseScale.z * sideScaleFactor;
            sideBuildingToScale.localScale = s;
        }

        // 3. Props – move in WORLD space by the exact height difference
        float heightDelta = actualHeightMetres - referenceHeightMetres;

        if (roofPropsToReposition != null && _propBaseWorldPositions != null)
        {
            for (int i = 0; i < roofPropsToReposition.Length; i++)
            {
                if (roofPropsToReposition[i] == null) continue;

                Vector3 worldPos = _propBaseWorldPositions[i];
                worldPos.y += heightDelta;
                roofPropsToReposition[i].position = worldPos;
            }
        }

        _scalingApplied = true;
        Debug.Log($"[DepthPerception] Main → {actualHeightMetres}m | Side scaled | Props +{heightDelta:F1}m");
    }

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Start()
    {
        _experimentLoader = FindAnyObjectByType<ExperimentLoader>();

        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (inputPanel != null) inputPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        if (startTestButton != null) startTestButton.onClick.AddListener(OnStartTest);
        if (incrementButton != null) incrementButton.onClick.AddListener(OnIncrement);
        if (decrementButton != null) decrementButton.onClick.AddListener(OnDecrement);
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        if (returnHomeButton != null) returnHomeButton.onClick.AddListener(OnReturnHome);
        if (heightSlider != null) heightSlider.onValueChanged.AddListener(OnSliderChanged);

        if (returnHomeButtons != null)
        {
            foreach (Button btn in returnHomeButtons)
            {
                if (btn != null)
                    btn.onClick.AddListener(OnReturnHome);
            }
        }

        StartCoroutine(BeginExperiment());
    }

    private void OnDestroy()
    {
        if (_startExperimentAnchor != null)
            _startExperimentAnchor.selectExited.RemoveListener(OnStartExperimentAnchorUsed);
    }

    private bool IsSessionPaused()
    {
        return SessionController.Instance != null && SessionController.Instance.IsPaused;
    }

    private IEnumerator BeginExperiment()
    {
        Debug.Log("[DepthPerception] Waiting for config...");
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        if (config == null)
            config = ExperimentConfigLoader.Current;

        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        if (config != null)
        {
            _maxHeightMetres = config.depth_MaxHeightMeters;
            _stepAmount = 1f;
            _actualHeight = config.depth_ActualHeightMeters;
            Debug.Log("[DepthPerception] Applied config parameters.");
        }
        else
        {
            _maxHeightMetres = 100f;
            _stepAmount = 1f;
            _actualHeight = 15f;
        }

        if (heightSlider != null)
        {
            heightSlider.minValue = 0f;
            heightSlider.maxValue = _maxHeightMetres;
            heightSlider.wholeNumbers = true;
            heightSlider.value = 0f;
        }

        if (instructionText != null)
        {
            instructionText.text = (config != null && !string.IsNullOrEmpty(config.depth_InstructionText))
                ? config.depth_InstructionText
                : "<b>Depth Perception Test</b>\n\nObserve the target building structure and estimate its height in meters.\n\nTeleport to the viewing position, then press <b>Start Test</b> when ready.";
        }

        // Keep the Start Test button hidden until the player uses StartExperimentAnchor
        if (startTestButton != null)
            startTestButton.gameObject.SetActive(false);

        if (instructionPanel != null) instructionPanel.SetActive(true);

        _actualHeight = config != null ? config.depth_ActualHeightMeters : 15f;
        CacheBaseScalesAndPositions();
        ApplyBuildingHeightScale(_actualHeight);
        SceneReadySignal.MarkReady();

        SubscribeToStartExperimentAnchor();
    }

    private void SubscribeToStartExperimentAnchor()
    {
        GameObject anchorGo = GameObject.Find("StartExperimentAnchor");
        if (anchorGo == null)
        {
            Debug.LogWarning("[DepthPerception] StartExperimentAnchor not found – showing Start Test button after delay as fallback.");
            StartCoroutine(ShowBeginButtonAfterDelay());
            return;
        }

        _startExperimentAnchor = anchorGo.GetComponent<TeleportationAnchor>();
        if (_startExperimentAnchor == null)
        {
            Debug.LogWarning("[DepthPerception] StartExperimentAnchor has no TeleportationAnchor component – showing button after delay as fallback.");
            StartCoroutine(ShowBeginButtonAfterDelay());
            return;
        }

        _startExperimentAnchor.selectExited.AddListener(OnStartExperimentAnchorUsed);
        Debug.Log("[DepthPerception] Waiting for player to teleport via StartExperimentAnchor before showing Start Test button.");
    }

    private void OnStartExperimentAnchorUsed(SelectExitEventArgs args)
    {
        // Only react once
        if (_startExperimentAnchor != null)
            _startExperimentAnchor.selectExited.RemoveListener(OnStartExperimentAnchorUsed);

        Debug.Log("[DepthPerception] Player entered via StartExperimentAnchor – showing Start Test button after delay.");
        StartCoroutine(ShowBeginButtonAfterDelay());
    }

    private IEnumerator ShowBeginButtonAfterDelay()
    {
        float delay = Mathf.Max(0f, beginButtonAppearDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (startTestButton != null)
            startTestButton.gameObject.SetActive(true);
    }

    private void OnStartTest()
    {
        if (IsSessionPaused()) return;

        if (instructionPanel != null) instructionPanel.SetActive(false);

        _trialStartTime = Time.time;
        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;
        _currentHeight = 0f;
        UpdateDisplay();

        if (heightPromptText != null)
            heightPromptText.text = "Estimate the building height in meters:";

        if (inputPanel != null) inputPanel.SetActive(true);

        ExperimentLogger.Log("trial_started", "Student began height estimation");
    }

    private void OnSubmit()
    {
        if (IsSessionPaused()) return;
        if (_submitted) return;

        _submitted = true;
        _completionTime = Time.time - _trialStartTime;

        if (inputPanel != null) inputPanel.SetActive(false);

        ExperimentLogger.Log("estimate_submitted",
            $"Student submitted height estimate: {_currentHeight:F0} m (actual: {_actualHeight:F0} m)",
            new Dictionary<string, object>
            {
                { "estimatedHeight", _currentHeight },
                { "actualHeight", _actualHeight },
                { "errorMargin", Mathf.Abs(_currentHeight - _actualHeight) },
                { "completionTimeSeconds", _completionTime }
            });

        ShowResults();
    }

    private void ShowResults()
    {
        float error = Mathf.Abs(_currentHeight - _actualHeight);

        string summary =
            $"Your Estimate:   {_currentHeight:F0} m\n" +
            $"Actual Height:   {_actualHeight:F0} m\n" +
            $"Absolute Error:  {error:F1} m\n\n" +
            (error < 2f
                ? "Excellent depth estimation accuracy!"
                : "Your estimate differed from the physical target size.");

        if (_experimentLoader != null)
        {
            _experimentLoader.QueueExperimentSummary("Depth Perception – Results", summary);
            _ = SaveResultsToFirestoreAsync(error);
            _experimentLoader.ReturnToHub();
        }
        else
        {
            _ = SaveResultsToFirestoreAsync(error);
        }
    }

    private async Task SaveResultsToFirestoreAsync(float error)
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            string studentId = GetConfigString("studentId", "Anonymous");
            string groupId = GetConfigString("groupId", "");
            string blockId = GetConfigString("blockId", "");
            string experimentId = GetConfigString("experimentId", "");
            string vrId = GetConfigString("vrId", "");
            string progressId = GetConfigString("progressId", "");

            if (string.IsNullOrEmpty(progressId) && SessionController.Instance != null)
                progressId = SessionController.Instance.progressDocumentId ?? "";

            float durationSeconds = 0f;
            if (_experimentStartRealtime > 0f)
                durationSeconds = Time.realtimeSinceStartup - _experimentStartRealtime;
            else if (_trialStartTime > 0f)
                durationSeconds = Time.time - _trialStartTime;

            string durationDisplay = FormatDuration(durationSeconds);
            DateTime completedAtUtc = DateTime.UtcNow;

            var configurations = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "maxHeightMetres", _maxHeightMetres },
                    { "stepAmount", _stepAmount },
                    { "actualHeightMetres", _actualHeight },
                    { "instructionText", config != null ? config.depth_InstructionText ?? "" : "" }
                }
            };

            var experimentalResults = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "trialIndex", 0 },
                    { "estimatedHeight", _currentHeight },
                    { "actualHeight", _actualHeight },
                    { "errorMargin", error },
                    { "completionTimeSeconds", _completionTime },
                    { "reactionTimeMs", (int)(_completionTime * 1000f) }
                }
            };

            var doc = new Dictionary<string, object>
            {
                { "blockId", blockId },
                { "experimentId", experimentId },
                { "groupId", groupId },
                { "studentId", studentId },
                { "vrId", vrId },
                { "progressId", progressId },
                { "experimentName", "Depth_Perception" },
                { "moduleName", "Depth_Perception" },
                { "completionStatus", "Completed" },
                { "duration", durationDisplay },
                { "durationSeconds", durationSeconds },
                { "startedAt", _startedAtUtc.ToString("o") },
                { "completedAt", completedAtUtc.ToString("o") },
                { "timestamp", completedAtUtc.ToString("o") },
                { "configurations", configurations },
                { "experimentalResults", experimentalResults }
            };

            await db.Collection("experimentResults").AddAsync(doc);
            Debug.Log($"[DepthPerception] Saved (duration={durationDisplay}, progressId={progressId})");

            ExperimentLogger.Log("experiment_completed",
                $"Depth Perception completed. Estimate={_currentHeight:F0}m, Actual={_actualHeight:F0}m, Error={error:F1}m",
                new Dictionary<string, object>
                {
                    { "estimatedHeight", _currentHeight },
                    { "actualHeight", _actualHeight },
                    { "errorMargin", error },
                    { "durationSeconds", durationSeconds }
                });

            if (!string.IsNullOrEmpty(progressId))
            {
                await db.Collection("experimentProgress").Document(progressId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "sessionControl", "ended" },
                        { "completionStatus", "Completed" },
                        { "completionAt", completedAtUtc.ToString("o") }
                    });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DepthPerception] Failed to save results: {ex.Message}");
            ExperimentLogger.Log("error", $"Failed to save results: {ex.Message}");
        }
    }

    private void OnReturnHome()
    {
        if (_experimentLoader != null)
            _experimentLoader.ReturnToHub(hubReturnPosition);
        else
            Debug.LogWarning("[DepthPerception] ExperimentLoader not found.");
    }

    private void OnIncrement()
    {
        if (IsSessionPaused()) return;
        SetHeight(_currentHeight + _stepAmount);
    }

    private void OnDecrement()
    {
        if (IsSessionPaused()) return;
        SetHeight(_currentHeight - _stepAmount);
    }

    private void OnSliderChanged(float value)
    {
        if (IsSessionPaused()) return;
        _currentHeight = value;
        UpdateDisplay(syncSlider: false);
    }

    private void SetHeight(float value)
    {
        _currentHeight = Mathf.Clamp(value, 0f, _maxHeightMetres);
        UpdateDisplay(syncSlider: true);
    }

    private void UpdateDisplay(bool syncSlider = true)
    {
        if (heightDisplayText != null)
        {
            heightDisplayText.text = $"{_currentHeight:F0} m";
            float t = _maxHeightMetres > 0 ? _currentHeight / _maxHeightMetres : 0f;
            heightDisplayText.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), t);
        }

        if (syncSlider && heightSlider != null)
            heightSlider.SetValueWithoutNotify(_currentHeight);

        if (decrementButton != null) decrementButton.interactable = _currentHeight > 0f;
        if (incrementButton != null) incrementButton.interactable = _currentHeight < _maxHeightMetres;
    }

    private string GetConfigString(string fieldName, string fallback)
    {
        if (config == null) return fallback;

        switch (fieldName)
        {
            case "studentId":
                return !string.IsNullOrEmpty(config.studentId) ? config.studentId : fallback;
            case "groupId":
                return !string.IsNullOrEmpty(config.groupId) ? config.groupId : fallback;
            case "blockId":
                return !string.IsNullOrEmpty(config.blockId) ? config.blockId : fallback;
            case "experimentId":
                return !string.IsNullOrEmpty(config.experimentId) ? config.experimentId : fallback;
            case "vrId":
                return !string.IsNullOrEmpty(config.vrId) ? config.vrId : fallback;
            case "progressId":
                return !string.IsNullOrEmpty(config.progressId) ? config.progressId : fallback;
            default:
                return fallback;
        }
    }

    private static string FormatDuration(float totalSeconds)
    {
        if (totalSeconds < 0f) totalSeconds = 0f;
        int t = Mathf.FloorToInt(totalSeconds);
        int m = t / 60;
        int s = t % 60;
        return $"{m}:{s:D2}";
    }
}