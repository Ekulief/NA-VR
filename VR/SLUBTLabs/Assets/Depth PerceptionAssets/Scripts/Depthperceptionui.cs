using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SLUBT Labs — Depth Perception UI
/// All experiment parameters are read from ExperimentConfig at runtime.
/// </summary>
public class DepthPerceptionUI : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Assign your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    [Header("UI References")]
    public TMP_Text heightDisplayText;
    public TMP_Text heightLabelText;
    public Button incrementButton;
    public Button decrementButton;
    public Slider heightSlider;
    public Button submitButton;
    public Button returnHomeButton;
    public GameObject confirmationPanel;

    [Tooltip("World position the player returns to in the hub.")]
    public Vector3 hubReturnPosition = Vector3.zero;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _currentHeight = 0f;
    private bool _submitted = false;
    private ExperimentLoader _experimentLoader;

    // ── Resolved config values ────────────────────────────────────────────────
    private float _maxHeightMetres;
    private float _stepAmount;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _experimentLoader = FindAnyObjectByType<ExperimentLoader>();

        // Read from config — falls back to defaults if config not assigned
        ExperimentConfig cfg = config ?? ExperimentConfigLoader.Current;
        if (cfg != null)
        {
            _maxHeightMetres = cfg.depth_MaxHeightMetres;
            _stepAmount = cfg.depth_StepAmount;

            if (heightLabelText != null)
                heightLabelText.text = cfg.depth_InstructionText;
        }
        else
        {
            Debug.LogWarning("[DepthPerceptionUI] No ExperimentConfig found — using defaults.");
            _maxHeightMetres = 100f;
            _stepAmount = 1f;
        }

        heightSlider.minValue = 0f;
        heightSlider.maxValue = _maxHeightMetres;
        heightSlider.wholeNumbers = true;
        heightSlider.value = 0f;

        incrementButton.onClick.AddListener(OnIncrement);
        decrementButton.onClick.AddListener(OnDecrement);
        submitButton.onClick.AddListener(OnSubmit);
        returnHomeButton.onClick.AddListener(OnReturnHome);
        heightSlider.onValueChanged.AddListener(OnSliderChanged);

        confirmationPanel.SetActive(false);
        UpdateDisplay();
    }

    // ── Button handlers ───────────────────────────────────────────────────────
    private void OnIncrement() => SetHeight(_currentHeight + _stepAmount);
    private void OnDecrement() => SetHeight(_currentHeight - _stepAmount);

    private void OnSliderChanged(float value)
    {
        _currentHeight = value;
        UpdateDisplay(syncSlider: false);
    }

    private void OnSubmit()
    {
        if (_submitted) return;
        _submitted = true;

        ExperimentConfig cfg = config ?? ExperimentConfigLoader.Current;
        float actualHeight = cfg != null ? cfg.depth_ActualHeightMetres : _maxHeightMetres;
        float error = Mathf.Abs(_currentHeight - actualHeight);

        Debug.Log($"[DepthPerception] Estimate: {_currentHeight}m | " +
                  $"Actual: {actualHeight}m | " +
                  $"Error: {error:F1}m | " +
                  $"Participant: {cfg?.participantId}");

        submitButton.interactable = false;
        incrementButton.interactable = false;
        decrementButton.interactable = false;
        heightSlider.interactable = false;

        confirmationPanel.SetActive(true);

        // TODO: SessionDataManager.Instance.RecordDepthEstimate(_currentHeight, actualHeight, error);
    }

    private void OnReturnHome()
    {
        if (_experimentLoader != null)
            _experimentLoader.ReturnToHub(hubReturnPosition);
        else
            Debug.LogWarning("[DepthPerceptionUI] ExperimentLoader not found.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetHeight(float value)
    {
        _currentHeight = Mathf.Clamp(value, 0f, _maxHeightMetres);
        UpdateDisplay(syncSlider: true);
    }

    private void UpdateDisplay(bool syncSlider = true)
    {
        heightDisplayText.text = $"{_currentHeight:F0} m";

        float t = _currentHeight / _maxHeightMetres;
        heightDisplayText.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), t);

        if (syncSlider)
            heightSlider.SetValueWithoutNotify(_currentHeight);

        decrementButton.interactable = _currentHeight > 0f;
        incrementButton.interactable = _currentHeight < _maxHeightMetres;
    }
}