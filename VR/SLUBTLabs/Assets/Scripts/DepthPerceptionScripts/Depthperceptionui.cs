using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;

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

        StartCoroutine(BeginExperiment());
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
            _maxHeightMetres = config.depth_MaxHeightMetres;
            _stepAmount = config.depth_StepAmount;
            _actualHeight = config.depth_ActualHeightMetres;
            Debug.Log("[DepthPerception] Applied config parameters.");
        }
        else
        {
            Debug.LogWarning("[DepthPerception] No config — using fallbacks.");
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
                : "<b>Depth Perception Test</b>\n\nObserve the target building structure and estimate its height in meters.\n\nPress <b>Start Test</b> when ready.";
        }

        if (instructionPanel != null) instructionPanel.SetActive(true);
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
    }

    private void OnSubmit()
    {
        if (IsSessionPaused()) return;
        if (_submitted) return;

        _submitted = true;
        _completionTime = Time.time - _trialStartTime;

        if (inputPanel != null) inputPanel.SetActive(false);
        ShowResults();
    }

    private void ShowResults()
    {
        float error = Mathf.Abs(_currentHeight - _actualHeight);

        if (resultsSummaryText != null)
        {
            resultsSummaryText.text =
                $"Trial Complete\n\n" +
                $"Your Estimate:   {_currentHeight:F0} m\n" +
                $"Actual Height:   {_actualHeight:F0} m\n" +
                $"Absolute Error:  {error:F1} m\n\n" +
                (error < 2f
                    ? "Excellent depth estimation accuracy!"
                    : "Your estimate differed from the physical target size.");
        }

        if (resultsPanel != null) resultsPanel.SetActive(true);
        _ = SaveResultsToFirestoreAsync(error);
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