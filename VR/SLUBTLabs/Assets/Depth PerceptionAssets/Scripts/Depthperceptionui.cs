using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;

/// <summary>
/// SLUBT Labs — Depth Perception UI & Manager
/// 
/// Flow:
/// 1. Waits for Firestore config via ExperimentConfigLoader.IsReady
/// 2. Instruction Panel visible first (uses ExperimentConfig text)
/// 3. Click "Start Test" -> Shows Depth Input Panel
/// 4. Submit Height -> Shows Results Panel & Uploads to Firestore directly
/// </summary>
public class DepthPerceptionUI : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("UI — Instruction Panel (Visible First)")]
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

    [Header("Navigation Settings")]
    [Tooltip("World position the player returns to in the hub.")]
    public Vector3 hubReturnPosition = Vector3.zero;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _currentHeight = 0f;
    private float _actualHeight;
    private float _maxHeightMetres = 100f;
    private float _stepAmount = 1f;
    private float _trialStartTime;
    private float _completionTime;
    private bool _submitted = false;
    private ExperimentLoader _experimentLoader;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _experimentLoader = FindAnyObjectByType<ExperimentLoader>();

        // Hide all sub-panels initially
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (inputPanel != null) inputPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        // Wire UI Listeners
        if (startTestButton != null) startTestButton.onClick.AddListener(OnStartTest);
        if (incrementButton != null) incrementButton.onClick.AddListener(OnIncrement);
        if (decrementButton != null) decrementButton.onClick.AddListener(OnDecrement);
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        if (returnHomeButton != null) returnHomeButton.onClick.AddListener(OnReturnHome);
        if (heightSlider != null) heightSlider.onValueChanged.AddListener(OnSliderChanged);

        StartCoroutine(BeginExperiment());
    }

    // ── Experiment flow ───────────────────────────────────────────────────────
    private IEnumerator BeginExperiment()
    {
        Debug.Log("[DepthPerception] Waiting for Firestore config to be ready...");
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        // Assign active config from loader if missing in inspector
        if (config == null)
        {
            config = ExperimentConfigLoader.Current;
        }

        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        // Apply parameters from Firestore config
        if (config != null)
        {
            _maxHeightMetres = config.depth_MaxHeightMetres;
            _stepAmount = config.depth_StepAmount;
            _actualHeight = config.depth_ActualHeightMetres;
            Debug.Log("[DepthPerception] Applied parameters from Firestore config.");
        }
        else
        {
            Debug.LogWarning("[DepthPerception] No ExperimentConfig found — using fallbacks.");
            _maxHeightMetres = 100f;
            _stepAmount = 1f;
            _actualHeight = 15f;
        }

        // Configure Input Slider
        if (heightSlider != null)
        {
            heightSlider.minValue = 0f;
            heightSlider.maxValue = _maxHeightMetres;
            heightSlider.wholeNumbers = true;
            heightSlider.value = 0f;
        }

        // Configure Instruction Text
        if (instructionText != null)
        {
            instructionText.text = (config != null && !string.IsNullOrEmpty(config.depth_InstructionText))
                ? config.depth_InstructionText
                : "<b>Depth Perception Test</b>\n\nObserve the target building structure and estimate its height in meters.\n\nPress <b>Start Test</b> when ready.";
        }

        // Show Instruction Panel FIRST
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    private void OnStartTest()
    {
        // Hide instructions & mark start time
        if (instructionPanel != null) instructionPanel.SetActive(false);

        _trialStartTime = Time.time;
        _currentHeight = 0f;

        UpdateDisplay();

        if (heightPromptText != null)
        {
            heightPromptText.text = "Estimate the building height in meters:";
        }

        // Show Input Panel
        if (inputPanel != null) inputPanel.SetActive(true);
    }

    private void OnSubmit()
    {
        if (_submitted) return;
        _submitted = true;

        _completionTime = Time.time - _trialStartTime;

        // Hide Input Panel
        if (inputPanel != null) inputPanel.SetActive(false);

        // Show Results Panel & Save
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

        // Save directly to Firestore (no SessionDataManager)
        _ = SaveResultsToFirestoreAsync(error);
    }

    private async Task SaveResultsToFirestoreAsync(float error)
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            var trialData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "participantId", config != null ? config.participantId : "Anonymous" },
                { "experimentName", "DepthPerception" },
                { "estimatedHeight", _currentHeight },
                { "actualHeight", _actualHeight },
                { "errorMargin", error },
                { "completionTimeSeconds", _completionTime },
                { "timestamp", System.DateTime.UtcNow.ToString("o") }
            };

            await db.Collection("experimentResult").AddAsync(trialData);
            Debug.Log("[DepthPerception] Results saved successfully to Firestore.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DepthPerception] Failed to save results to Firestore: {ex.Message}");
        }
    }

    private void OnReturnHome()
    {
        if (_experimentLoader != null)
            _experimentLoader.ReturnToHub(hubReturnPosition);
        else
            Debug.LogWarning("[DepthPerception] ExperimentLoader not found.");
    }

    // ── Input controls and helpers ────────────────────────────────────────────

    private void OnIncrement() => SetHeight(_currentHeight + _stepAmount);
    private void OnDecrement() => SetHeight(_currentHeight - _stepAmount);

    private void OnSliderChanged(float value)
    {
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
}