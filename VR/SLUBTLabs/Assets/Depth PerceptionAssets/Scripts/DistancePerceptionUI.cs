using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;

/// <summary>
/// SLUBT Labs — Distance Perception UI & Manager (Depth Perception 2)
/// 
/// Flow:
/// 1. Waits for Firestore config via ExperimentConfigLoader.IsReady
/// 2. Instruction Panel visible first (uses ExperimentConfig text)
/// 3. Click "Start Test" -> Shows Distance Input Panel
/// 4. Submit Distance -> Shows Results Panel & Uploads to Firestore directly
/// </summary>
public class DistancePerceptionUI : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("UI — Instruction Panel (Visible First)")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;
    public Button startTestButton;

    [Header("UI — Input Panel")]
    public GameObject inputPanel;
    public TMP_Text distanceDisplayText;
    public TMP_Text distancePromptText;
    public Button incrementButton;
    public Button decrementButton;
    public Slider distanceSlider;
    public Button submitButton;

    [Header("UI — Results Panel")]
    public GameObject resultsPanel;
    public TMP_Text resultsSummaryText;
    public Button returnHomeButton;

    [Header("Navigation Settings")]
    [Tooltip("World position the player returns to in the hub.")]
    public Vector3 hubReturnPosition = Vector3.zero;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _currentDistance = 0f;
    private float _actualDistance;
    private float _minDistanceMetres = 0f;
    private float _maxDistanceMetres = 100f;
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
        if (distanceSlider != null) distanceSlider.onValueChanged.AddListener(OnSliderChanged);

        StartCoroutine(BeginExperiment());
    }

    // ── Experiment flow ───────────────────────────────────────────────────────
    private IEnumerator BeginExperiment()
    {
        Debug.Log("[DistancePerception] Waiting for Firestore config to be ready...");
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
            _minDistanceMetres = config.depth_MinDistanceMeters;
            _maxDistanceMetres = config.depth_MaxDistanceMeters;
            _stepAmount = config.depth_StepAmount;
            _actualDistance = config.depth_ActualDistanceMeters;
            Debug.Log("[DistancePerception] Applied parameters from Firestore config.");
        }
        else
        {
            Debug.LogWarning("[DistancePerception] No ExperimentConfig found — using fallbacks.");
            _minDistanceMetres = 0f;
            _maxDistanceMetres = 100f;
            _stepAmount = 1f;
            _actualDistance = 63f;
        }

        // Configure Input Slider
        if (distanceSlider != null)
        {
            distanceSlider.minValue = _minDistanceMetres;
            distanceSlider.maxValue = _maxDistanceMetres;
            distanceSlider.wholeNumbers = true;
            distanceSlider.value = _minDistanceMetres;
        }

        // Configure Instruction Text
        if (instructionText != null)
        {
            instructionText.text = (config != null && !string.IsNullOrEmpty(config.depth_InstructionText))
                ? config.depth_InstructionText
                : "<b>Horizontal Distance Perception Test</b>\n\nObserve the target object ahead and estimate its horizontal distance in meters.\n\nPress <b>Start Test</b> when ready.";
        }

        // Show Instruction Panel FIRST
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    private void OnStartTest()
    {
        // Hide instructions & mark start time
        if (instructionPanel != null) instructionPanel.SetActive(false);

        _trialStartTime = Time.time;
        _currentDistance = _minDistanceMetres;

        UpdateDisplay();

        if (distancePromptText != null)
        {
            distancePromptText.text = "Estimate horizontal distance in meters:";
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
        float error = Mathf.Abs(_currentDistance - _actualDistance);

        if (resultsSummaryText != null)
        {
            resultsSummaryText.text =
                $"Trial Complete\n\n" +
                $"Your Estimate:   {_currentDistance:F0} m\n" +
                $"Actual Distance: {_actualDistance:F0} m\n" +
                $"Absolute Error:  {error:F1} m\n\n" +
                (error < 2f
                    ? "Excellent distance estimation accuracy!"
                    : "Your estimate differed from the target position.");
        }

        if (resultsPanel != null) resultsPanel.SetActive(true);

        // Save directly to Firestore
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
                { "experimentName", "Depth_Perception2" },
                { "estimatedDistance", _currentDistance },
                { "actualDistance", _actualDistance },
                { "errorMargin", error },
                { "completionTimeSeconds", _completionTime },
                { "timestamp", System.DateTime.UtcNow.ToString("o") }
            };

            await db.Collection("experimentResult").AddAsync(trialData);
            Debug.Log("[DistancePerception] Results saved successfully to Firestore.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DistancePerception] Failed to save results to Firestore: {ex.Message}");
        }
    }

    private void OnReturnHome()
    {
        if (_experimentLoader != null)
            _experimentLoader.ReturnToHub(hubReturnPosition);
        else
            Debug.LogWarning("[DistancePerception] ExperimentLoader not found.");
    }

    // ── Input controls and helpers ────────────────────────────────────────────

    private void OnIncrement() => SetDistance(_currentDistance + _stepAmount);
    private void OnDecrement() => SetDistance(_currentDistance - _stepAmount);

    private void OnSliderChanged(float value)
    {
        _currentDistance = value;
        UpdateDisplay(syncSlider: false);
    }

    private void SetDistance(float value)
    {
        _currentDistance = Mathf.Clamp(value, _minDistanceMetres, _maxDistanceMetres);
        UpdateDisplay(syncSlider: true);
    }

    private void UpdateDisplay(bool syncSlider = true)
    {
        if (distanceDisplayText != null)
        {
            distanceDisplayText.text = $"{_currentDistance:F0} m";

            float range = _maxDistanceMetres - _minDistanceMetres;
            float t = range > 0 ? (_currentDistance - _minDistanceMetres) / range : 0f;
            distanceDisplayText.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), t);
        }

        if (syncSlider && distanceSlider != null)
            distanceSlider.SetValueWithoutNotify(_currentDistance);

        if (decrementButton != null) decrementButton.interactable = _currentDistance > _minDistanceMetres;
        if (incrementButton != null) incrementButton.interactable = _currentDistance < _maxDistanceMetres;
    }
}