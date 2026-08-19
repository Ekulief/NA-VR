using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SLUBT Labs — Depth Perception UI
///
/// SETUP:
///   a) Create an empty GameObject in your Depth Perception scene called "ExperimentPanel".
///   b) Add a World Space Canvas as a child — see positioning notes below.
///   c) Build the UI hierarchy (see UI_LAYOUT_GUIDE or the companion prefab).
///   d) Attach this script to the ExperimentPanel GameObject.
///   e) Wire all fields in the Inspector.
///
/// CANVAS POSITIONING:
///   Parent the Canvas to your XR Origin's Camera Offset so it follows the player,
///   then set Local Position to (0, -0.3, 1.2) — slightly below eye level, 1.2 m ahead.
///   Scale: (0.001, 0.001, 0.001) with Width/Height 600 x 400.
///
/// HEIGHT RANGE:
///   Min is always 0 m. Set maxHeightMetres in the Inspector to match
///   the actual height of your building in Unity world units.
///
/// SAVING RESULTS:
///   Requires a SessionDataManager present in a loaded scene (see SessionDataManager.cs).
///   On submit, this writes {userId, experimentName, heightEstimateMetres, timestampUtc}
///   to a local JSON file under Application.persistentDataPath.
/// </summary>
public class DepthPerceptionUI : MonoBehaviour
{
    [Header("Height Config")]
    [Tooltip("Maximum selectable height in metres. Match your building height.")]
    public float maxHeightMetres = 100f;

    [Tooltip("How much each +/- button press changes the value.")]
    public float stepAmount = 1f;

    [Header("UI References")]
    public TMP_Text heightDisplayText;      // Large number in the centre e.g. "42 m"
    public TMP_Text heightLabelText;        // Small label above e.g. "Your estimate"
    public Button incrementButton;          // + button
    public Button decrementButton;          // - button
    public Slider heightSlider;             // Bottom slider for large jumps
    public Button submitButton;              // Confirm estimate
    public Button returnHomeButton;          // Return to hub
    public GameObject confirmationPanel;    // Shown after submit, hidden by default

    // ── State ─────────────────────────────────────────────────────────────────
    private float _currentHeight = 0f;
    private bool _submitted = false;
    private ExperimentLoader _experimentLoader;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        // Find ExperimentLoader in the always-loaded Main scene at runtime
        // avoids cross-scene Inspector reference
        _experimentLoader = FindAnyObjectByType<ExperimentLoader>();

        // Slider setup
        heightSlider.minValue = 0f;
        heightSlider.maxValue = maxHeightMetres;
        heightSlider.wholeNumbers = true;
        heightSlider.value = 0f;

        // Wire up buttons
        incrementButton.onClick.AddListener(OnIncrement);
        decrementButton.onClick.AddListener(OnDecrement);
        submitButton.onClick.AddListener(OnSubmit);
        returnHomeButton.onClick.AddListener(OnReturnHome);

        // Slider drives the number display directly
        heightSlider.onValueChanged.AddListener(OnSliderChanged);

        confirmationPanel.SetActive(false);
        UpdateDisplay();
    }

    // ── Button handlers ───────────────────────────────────────────────────────
    private void OnIncrement()
    {
        SetHeight(_currentHeight + stepAmount);
    }

    private void OnDecrement()
    {
        SetHeight(_currentHeight - stepAmount);
    }

    private void OnSliderChanged(float value)
    {
        // Slider change updates the number without looping back into slider
        _currentHeight = value;
        UpdateDisplay(syncSlider: false);
    }

    private void OnSubmit()
    {
        if (_submitted) return;
        _submitted = true;

        Debug.Log($"[SLUBT Labs] Participant estimate: {_currentHeight} m");

        if (SessionDataManager.Instance != null)
        {
            SessionDataManager.Instance.RecordDepthEstimate(_currentHeight);
        }
        else
        {
            Debug.LogWarning("[SLUBT Labs] SessionDataManager not found in any loaded scene — result was NOT saved.");
        }

        submitButton.interactable = false;
        incrementButton.interactable = false;
        decrementButton.interactable = false;
        heightSlider.interactable = false;

        confirmationPanel.SetActive(true);
    }

    private void OnReturnHome()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (_experimentLoader != null)
        {
            // Loader now handles matching and teleporting directly to "Respawn"
            _experimentLoader.ReturnToHub();
        }
        else
        {
            Debug.LogWarning("[SLUBT Labs] ExperimentLoader not found in any loaded scene.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetHeight(float value)
    {
        _currentHeight = Mathf.Clamp(value, 0f, maxHeightMetres);
        UpdateDisplay(syncSlider: true);
    }

    private void UpdateDisplay(bool syncSlider = true)
    {
        heightDisplayText.text = $"{_currentHeight:F0} m";

        // Clamp display colour: white at 0, red at max
        float t = _currentHeight / maxHeightMetres;
        heightDisplayText.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), t);

        if (syncSlider)
            heightSlider.SetValueWithoutNotify(_currentHeight);

        // Disable buttons at bounds
        decrementButton.interactable = _currentHeight > 0f;
        incrementButton.interactable = _currentHeight < maxHeightMetres;
    }
}