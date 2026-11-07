using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Distance Perception UI
/// Waits for config, shows instructions, collects an estimate, then saves results.
/// </summary>
public class DistancePerceptionUI : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("UI — Instruction Panel")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;
    public Button startTestButton;
    [Tooltip("Seconds to wait after the instruction panel appears before showing the Start Test button.")]
    public float beginButtonAppearDelay = 2.5f;

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

    [Header("Navigation")]
    public Vector3 hubReturnPosition = Vector3.zero;

    [Header("Locomotion Lock")]
    [Tooltip("Optional: leave empty – LocomotionLocker / runtime Find will locate the XR Origin")]
    public GameObject xrOriginOverride;

    private float _currentDistance = 0f;
    private float _actualDistance;
    private float _minDistanceMetres = 0f;
    private float _maxDistanceMetres = 100f;
    private float _stepAmount = 1f;
    private float _trialStartTime;
    private float _completionTime;
    private float _experimentStartRealtime;
    private DateTime _startedAtUtc;
    private bool _submitted = false;
    private ExperimentLoader _experimentLoader;

    // Locomotion Lock State (Matching Attentional Blindness)
    private bool _locomotionLocked = false;
    private Vector3 _lockedPosition;
    private readonly List<Behaviour> _disabledLocomotion = new();
    private CharacterController _characterController;

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
        if (distanceSlider != null) distanceSlider.onValueChanged.AddListener(OnSliderChanged);

        StartCoroutine(BeginExperiment());
    }

    private void OnDestroy()
    {
        UnlockLocomotion();

        if (startTestButton != null) startTestButton.onClick.RemoveListener(OnStartTest);
        if (incrementButton != null) incrementButton.onClick.RemoveListener(OnIncrement);
        if (decrementButton != null) decrementButton.onClick.RemoveListener(OnDecrement);
        if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
        if (returnHomeButton != null) returnHomeButton.onClick.RemoveListener(OnReturnHome);
        if (distanceSlider != null) distanceSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    // ─────────────────────────────────────────────
    // Locomotion lock / unlock (same as Attentional Blindness)
    // ─────────────────────────────────────────────

    private void LockLocomotion()
    {
        if (_locomotionLocked) return;

        GameObject origin = xrOriginOverride;
        Unity.XR.CoreUtils.XROrigin xr = null;

        if (origin == null)
        {
            xr = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xr != null) origin = xr.gameObject;
        }
        else
        {
            xr = origin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        }

        if (origin == null)
        {
            Debug.LogWarning("[DistancePerception] VR Player / XROrigin not found.");
            return;
        }

        Debug.Log($"[DistancePerception] Found VR Player: '{origin.name}' " +
                  $"(pos={origin.transform.position}, children={origin.transform.childCount})");

        DisableIfFound<ContinuousMoveProvider>(origin, "Move (thumbstick walk)");
        DisableIfFound<TeleportationProvider>(origin, "Teleportation");
        DisableIfFound<ContinuousTurnProvider>(origin, "ContinuousTurn (stick)");
        DisableIfFound<SnapTurnProvider>(origin, "SnapTurn (stick)");

        _characterController = origin.GetComponent<CharacterController>();
        if (_characterController != null && _characterController.enabled)
        {
            _characterController.enabled = false;
            Debug.Log("[DistancePerception] Disabled: CharacterController on VR Player");
        }

        _lockedPosition = origin.transform.position;
        _locomotionLocked = true;

        Debug.Log($"[DistancePerception] Locomotion locked. " +
                  $"Disabled {_disabledLocomotion.Count} providers. " +
                  $"Position frozen at {_lockedPosition}. Head look + UI trigger still enabled.");
    }

    private void DisableIfFound<T>(GameObject root, string label) where T : Behaviour
    {
        var comps = root.GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            _disabledLocomotion.Add(c);
            Debug.Log($"[DistancePerception] Disabled: {typeof(T).Name} on '{c.gameObject.name}' ({label})");
        }
    }

    private void UnlockLocomotion()
    {
        if (!_locomotionLocked) return;

        foreach (var c in _disabledLocomotion)
        {
            if (c != null) c.enabled = true;
        }
        _disabledLocomotion.Clear();

        if (_characterController != null)
            _characterController.enabled = true;

        _locomotionLocked = false;
        LocomotionLocker.Unlock();
        Debug.Log("[DistancePerception] Locomotion unlocked.");
    }

    private void LateUpdate()
    {
        if (!_locomotionLocked) return;

        GameObject origin = xrOriginOverride;
        if (origin == null)
        {
            var xr = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xr != null) origin = xr.gameObject;
        }
        if (origin == null) return;

        //origin.transform.position = _lockedPosition;
    }

    private bool IsSessionPaused()
    {
        return SessionController.Instance != null && SessionController.Instance.IsPaused;
    }

    private IEnumerator BeginExperiment()
    {
        Debug.Log("[DistancePerception] Waiting for config...");
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        if (config == null)
            config = ExperimentConfigLoader.Current;

        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        if (config != null)
        {
            _minDistanceMetres = config.distance_MinDistanceMeters;
            _maxDistanceMetres = config.distance_MaxDistanceMeters;
            _actualDistance = config.distance_ActualDistanceMeters;
        }
        else
        {
            _minDistanceMetres = 0f;
            _maxDistanceMetres = 100f;
            _stepAmount = 1f;
            _actualDistance = 63f;
        }

        if (distanceSlider != null)
        {
            distanceSlider.minValue = _minDistanceMetres;
            distanceSlider.maxValue = _maxDistanceMetres;
            distanceSlider.wholeNumbers = true;
            distanceSlider.value = _minDistanceMetres;
        }

        if (instructionText != null)
        {
            string loadedInstruction = config != null ? config.distance_InstructionText2 : null;
            instructionText.text = !string.IsNullOrEmpty(loadedInstruction)
                ? loadedInstruction
                : "<b>Horizontal Distance Perception Test</b>\n\nObserve the target object ahead and estimate its horizontal distance in meters.\n\nPress <b>Start Test</b> when ready.";
        }

        // Full lock via LocomotionLocker + hard freeze (same as Attentional Blindness)
        LocomotionLocker.Lock(LocomotionLocker.LockMode.Full, xrOriginOverride);
        LockLocomotion();

        // Hide the button first so it can appear after a short delay
        if (startTestButton != null)
            startTestButton.gameObject.SetActive(false);

        if (instructionPanel != null) instructionPanel.SetActive(true);

        SceneReadySignal.MarkReady();

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

        // Allow teleport + turning from this point. Continuous walk stays locked.
        UnlockTeleportAndTurn();

        _trialStartTime = Time.time;
        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;
        _currentDistance = _minDistanceMetres;
        UpdateDisplay();

        if (distancePromptText != null)
            distancePromptText.text = "Estimate horizontal distance in meters:";

        if (inputPanel != null) inputPanel.SetActive(true);

        ExperimentLogger.Log("trial_started", "Student began distance estimation");
    }
    private async void OnSubmit()
    {
        if (IsSessionPaused() || _submitted) return;
        _submitted = true;

        if (submitButton != null) submitButton.interactable = false;

        _completionTime = Time.time - _trialStartTime;

        if (inputPanel != null) inputPanel.SetActive(false);

        ExperimentLogger.Log("estimate_submitted",
            $"Student submitted distance estimate: {_currentDistance:F0} m (actual: {_actualDistance:F0} m)",
            new Dictionary<string, object>
            {
                { "estimatedDistance", _currentDistance },
                { "actualDistance", _actualDistance },
                { "errorMargin", Mathf.Abs(_currentDistance - _actualDistance) },
                { "completionTimeSeconds", _completionTime }
            });

        await ShowResultsAsync();
    }

    private async Task ShowResultsAsync()
    {
        float error = Mathf.Abs(_currentDistance - _actualDistance);

        string summary =
            $"Your Estimate:   {_currentDistance:F0} m\n" +
            $"Actual Distance: {_actualDistance:F0} m\n" +
            $"Absolute Error:  {error:F1} m\n\n" +
            (error < 2f
                ? "Excellent distance estimation accuracy!"
                : "Your estimate differed from the target position.");

        UnlockLocomotion();

        if (_experimentLoader != null)
        {
            _experimentLoader.QueueExperimentSummary("Distance Perception – Results", summary);
            await SaveResultsToFirestoreAsync(error);
            _experimentLoader.ReturnToHub();
        }
        else
        {
            await SaveResultsToFirestoreAsync(error);
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
                    { "minDistanceMeters", _minDistanceMetres },
                    { "maxDistanceMeters", _maxDistanceMetres },
                    { "stepAmount", _stepAmount },
                    { "actualDistanceMeters", _actualDistance },
                    { "instructionText", config != null ? config.distance_InstructionText2 ?? "" : "" }
                }
            };

            var experimentalResults = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "trialIndex", 0 },
                    { "estimatedDistance", _currentDistance },
                    { "actualDistance", _actualDistance },
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
                { "experimentName", "Distance Perception" },
                { "moduleName", "Distance Perception" },
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
            Debug.Log($"[DistancePerception] Saved (duration={durationDisplay}, progressId={progressId})");

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
            Debug.LogError($"[DistancePerception] Failed to save results: {ex.Message}");
            ExperimentLogger.Log("error", $"Failed to save results: {ex.Message}");
        }
    }

    private void OnReturnHome()
    {
        UnlockLocomotion();

        if (_experimentLoader != null)
            _experimentLoader.ReturnToHub(hubReturnPosition);
        else
            Debug.LogWarning("[DistancePerception] ExperimentLoader not found.");
    }

    private void OnIncrement()
    {
        if (IsSessionPaused()) return;
        SetDistance(_currentDistance + _stepAmount);
    }

    private void OnDecrement()
    {
        if (IsSessionPaused()) return;
        SetDistance(_currentDistance - _stepAmount);
    }

    private void OnSliderChanged(float value)
    {
        if (IsSessionPaused()) return;
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
    /// <summary>
    /// Re-enables teleport + turning while leaving continuous move locked.
    /// Also stops the hard position freeze so the player can move via teleport.
    /// </summary>
    private void UnlockTeleportAndTurn()
    {
        if (!_locomotionLocked) return;

        for (int i = _disabledLocomotion.Count - 1; i >= 0; i--)
        {
            var c = _disabledLocomotion[i];
            if (c == null) continue;

            if (c is TeleportationProvider ||
                c is ContinuousTurnProvider ||
                c is SnapTurnProvider)
            {
                c.enabled = true;
                _disabledLocomotion.RemoveAt(i);
                Debug.Log($"[DistancePerception] Unlocked: {c.GetType().Name} on {c.gameObject.name}");
            }
        }
        _locomotionLocked = false;

        if (_characterController != null)
            _characterController.enabled = true;

        Debug.Log("[DistancePerception] Teleport + Turn unlocked (continuous move remains locked).");
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