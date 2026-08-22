using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

/// <summary>
/// SLUBT Labs — Attentional Blindness Manager
///
/// Works with ShelfSpawner — finds spawned items at runtime, no pre-assignment needed.
/// Registers existing colliders from prefabs with XRSimpleInteractable at runtime.
///
/// FLOW:
///   1. Waits one frame for ShelfSpawners to finish
///   2. Registers existing colliders with XRSimpleInteractable on each spawned item
///   3. Picks anomaly items and starts blinking them
///   4. Shows instruction — participant searches and points ray at target + pulls trigger
///   5. Correct item → blinking stops, report panel appears
///   6. Wrong item → face-following feedback text for 2 seconds
///   7. Post-task awareness report — Yes / No
///   8. Results recorded
/// </summary>
public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Assign: XRI Default Input Actions → XRI Right Hand Interaction → Activate")]
    public InputActionReference triggerAction;

    [Header("Experiment Config")]
    [Tooltip("Must match the name of your target prefab exactly.")]
    public string targetItemName = "Milk";

    [Tooltip("How many distractor items should blink. -1 = random subset.")]
    public int anomalyCount = 3;

    [Tooltip("Max raycast distance for item selection.")]
    public float raycastDistance = 10f;

    [Tooltip("Seconds after scene load before instruction appears.")]
    public float instructionDelay = 1.5f;

    [Header("UI — Instruction Panel")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;

    [Header("UI — Awareness Report Panel")]
    public GameObject reportPanel;
    public TMP_Text reportQuestionText;
    public Button yesButton;
    public Button noButton;

    [Header("UI — Results Panel")]
    public GameObject resultsPanel;
    public TMP_Text resultsSummaryText;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _trialStartTime;
    private float _foundTime;
    private List<AnomalyManager> _activeBlinkers = new();
    private List<GameObject> _allSpawnedItems = new();
    private GameObject _targetInstance;
    private bool _awaitingSelection = false;
    private bool _trialComplete = false;
    private FeedbackDisplay _feedbackDisplay;
    private Transform _rightControllerTransform;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPressed;
        }
    }

    private void OnDisable()
    {
        if (triggerAction != null)
            triggerAction.action.performed -= OnTriggerPressed;
    }

    private void Start()
    {
        instructionPanel.SetActive(false);
        reportPanel.SetActive(false);
        resultsPanel.SetActive(false);

        _feedbackDisplay = GetComponent<FeedbackDisplay>();

        yesButton.onClick.AddListener(() => OnAwarenessReport(true));
        noButton.onClick.AddListener(() => OnAwarenessReport(false));

        StartCoroutine(InitialiseAfterSpawn());
    }

    // ── Initialisation ────────────────────────────────────────────────────────
    private IEnumerator InitialiseAfterSpawn()
    {
        // Wait one frame for all ShelfSpawners to finish Start()
        yield return null;

        // Find right controller automatically from VR Player in main scene
        _rightControllerTransform = FindRightController();
        if (_rightControllerTransform == null)
            Debug.LogWarning("[AttentionalBlindness] Could not find Right Controller — manual trigger fallback disabled.");

        CollectSpawnedItems();

        if (_allSpawnedItems.Count == 0)
        {
            Debug.LogError("[AttentionalBlindness] No spawned items found! " +
                           "Make sure item prefabs are tagged 'SpawnedItem'.");
            yield break;
        }

        // Register existing colliders with XRSimpleInteractable on each item
        foreach (GameObject item in _allSpawnedItems)
            SetupInteractable(item);

        // Find the target instance
        _targetInstance = _allSpawnedItems.Find(item =>
            item.name.StartsWith(targetItemName, System.StringComparison.OrdinalIgnoreCase));

        if (_targetInstance == null)
            Debug.LogWarning($"[AttentionalBlindness] No item matching '{targetItemName}' found.");
        else
            Debug.Log($"[AttentionalBlindness] Target: '{_targetInstance.name}' at {_targetInstance.transform.position}");

        yield return new WaitForSeconds(instructionDelay);
        BeginTrial();
    }

    private Transform FindRightController()
    {
        string[] names = { "Ray Interactor", "RayInteractor", "Right Controller", "Right Hand" };
        foreach (string n in names)
        {
            GameObject found = GameObject.Find(n);
            if (found != null)
                return found.transform;
        }

        // Fallback — find XRRayInteractor component anywhere in loaded scenes
        var rayInteractor = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        if (rayInteractor != null)
            return rayInteractor.transform;

        return null;
    }

    private void SetupInteractable(GameObject item)
    {
        XRSimpleInteractable interactable = item.GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning($"[AttentionalBlindness] '{item.name}' has no XRSimpleInteractable.");
            return;
        }

        // Use existing collider from prefab — added directly in Project window
        Collider col = item.GetComponentInChildren<Collider>();
        if (col is BoxCollider box)
        {
            box.size *= 0.85f; 
        }
        if (col == null)
        {
            Debug.LogWarning($"[AttentionalBlindness] '{item.name}' has no Collider — add one to the prefab.");
            return;
        }

        // Register with XRSimpleInteractable's collider list
        if (!interactable.colliders.Contains(col))
            interactable.colliders.Add(col);

        // Listen for selection
        interactable.selectEntered.AddListener((args) => OnItemSelected(item));

        Debug.Log($"[AttentionalBlindness] Interactable set up on '{item.name}'");
    }

    private void CollectSpawnedItems()
    {
        _allSpawnedItems.Clear();
        GameObject[] found = GameObject.FindGameObjectsWithTag("SpawnedItem");
        _allSpawnedItems.AddRange(found);
        Debug.Log($"[AttentionalBlindness] Collected {_allSpawnedItems.Count} spawned items.");
    }

    // ── Trial flow ────────────────────────────────────────────────────────────
    private void BeginTrial()
    {
        List<GameObject> distractors = _allSpawnedItems.FindAll(item => item != _targetInstance);

        for (int i = distractors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (distractors[i], distractors[j]) = (distractors[j], distractors[i]);
        }

        int count = anomalyCount < 0
            ? Random.Range(2, Mathf.Max(3, distractors.Count / 3))
            : Mathf.Min(anomalyCount, distractors.Count);

        for (int i = 0; i < count; i++)
        {
            AnomalyManager blinker = distractors[i].AddComponent<AnomalyManager>();
            _activeBlinkers.Add(blinker);
        }

        instructionText.text = $"Find the <b>{targetItemName}</b> on the shelves.\n\n" +
                               $"Point at it and pull the <b>trigger</b> to confirm.";
        instructionPanel.SetActive(true);

        StartCoroutine(StartBlinkingAfterDelay(1.5f));
        _trialStartTime = Time.time;
        _awaitingSelection = true;

        Debug.Log($"[AttentionalBlindness] Trial started — {count} anomaly items blinking.");
    }

    private IEnumerator StartBlinkingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (AnomalyManager blinker in _activeBlinkers)
            blinker.StartBlinking();
    }

    private void StopAnomalies()
    {
        foreach (AnomalyManager blinker in _activeBlinkers)
            if (blinker != null) blinker.StopBlinking();
        _activeBlinkers.Clear();
    }

    // ── Item selection via XRSimpleInteractable ───────────────────────────────
    private void OnItemSelected(GameObject item)
    {
        if (!_awaitingSelection || _trialComplete) return;

        if (item == _targetInstance)
            OnTargetFound();
        else
            OnWrongItemSelected(item.name);
    }

    // ── Manual trigger fallback ───────────────────────────────────────────────
    private void OnTriggerPressed(InputAction.CallbackContext ctx)
    {
        if (!_awaitingSelection || _trialComplete) return;
        if (_rightControllerTransform == null) return;

        Ray ray = new Ray(_rightControllerTransform.position, _rightControllerTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            GameObject hitRoot = GetSpawnedItemRoot(hit.collider.gameObject);
            if (hitRoot == null) return;

            if (hitRoot == _targetInstance)
                OnTargetFound();
            else
                OnWrongItemSelected(hitRoot.name);
        }
    }

    private GameObject GetSpawnedItemRoot(GameObject hit)
    {
        Transform t = hit.transform;
        while (t != null)
        {
            if (t.CompareTag("SpawnedItem"))
                return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    // ── Outcomes ──────────────────────────────────────────────────────────────
    private void OnTargetFound()
    {
        _awaitingSelection = false;
        _trialComplete = true;
        _foundTime = Time.time - _trialStartTime;

        StopAnomalies();
        instructionPanel.SetActive(false);
        _feedbackDisplay?.HideFeedback();

        Debug.Log($"[AttentionalBlindness] Correct! Target found in {_foundTime:F2}s");

        StartCoroutine(ShowReportAfterDelay(0.5f));
    }

    private void OnWrongItemSelected(string itemName)
    {
        Debug.Log($"[AttentionalBlindness] Wrong item: '{itemName}'");
        _feedbackDisplay?.ShowFeedback($"That's not the {targetItemName}. Keep looking!", Color.red);
    }

    private IEnumerator ShowReportAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        reportQuestionText.text = "While searching for the item,\ndid you notice anything unusual\nhappening on the shelves?";
        reportPanel.SetActive(true);
    }

    // ── Awareness report ──────────────────────────────────────────────────────
    private void OnAwarenessReport(bool noticed)
    {
        reportPanel.SetActive(false);

        resultsSummaryText.text =
            $"Trial Complete\n\n" +
            $"Target:             {targetItemName}\n" +
            $"Search time:        {_foundTime:F1}s\n" +
            $"Noticed anomaly:    {(noticed ? "Yes" : "No")}\n\n" +
            (noticed
                ? "You noticed the blinking items while searching."
                : "You did not notice the blinking items.\nThis is the attentional blindness effect.");

        resultsPanel.SetActive(true);

        Debug.Log($"[AttentionalBlindness] Result — " +
                  $"Target: {targetItemName} | " +
                  $"Search time: {_foundTime:F2}s | " +
                  $"Noticed anomaly: {noticed}");

        // TODO: SessionDataManager.Instance.RecordAttentionalBlindnessTrial(targetItemName, _foundTime, noticed);
    }
}