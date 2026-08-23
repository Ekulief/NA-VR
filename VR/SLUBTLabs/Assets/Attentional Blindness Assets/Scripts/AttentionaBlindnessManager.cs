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
/// Injects a UNIQUE target prefab into a random shelf slot so there is no ambiguity
/// about which item is the target. The target prefab is separate from all distractor prefabs.
///
/// FLOW:
///   1. Waits one frame for ShelfSpawners to finish
///   2. Picks a random shelf and injects the unique target prefab into a random slot
///   3. Registers existing colliders with XRSimpleInteractable on all spawned items
///   4. Picks anomaly items (excluding target) and starts blinking them
///   5. Shows instruction — participant searches and points ray at target + pulls trigger
///   6. Correct item → blinking stops, report panel appears
///   7. Wrong item → face-following feedback text for 2 seconds
///   8. Post-task awareness report — Yes / No
///   9. Results recorded
///
/// SETUP:
///   a) Attach to empty GameObject "AttentionalBlindnessManager"
///   b) Assign uniqueTargetPrefab — this is the one item that IS the target
///      It should NOT appear in any ShelfSpawner's distractor list
///   c) Assign UI panels
///   d) triggerAction → XRI Default Input Actions → XRI Right Hand Interaction → Activate
/// </summary>
public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Assign: XRI Default Input Actions → XRI Right Hand Interaction → Activate")]
    public InputActionReference triggerAction;

    [Header("Experiment Config")]
    [Tooltip("The unique target prefab — must NOT be in any ShelfSpawner distractor list. " +
             "The manager spawns exactly one of these somewhere on a random shelf.")]
    public GameObject uniqueTargetPrefab;

    [Tooltip("Display name shown in the instruction e.g. 'Milk'.")]
    public string targetDisplayName = "Milk";

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
    private GameObject _targetInstance;          // direct reference — no name matching needed
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

        // Inject unique target into a random shelf
        if (uniqueTargetPrefab == null)
        {
            Debug.LogError("[AttentionalBlindness] uniqueTargetPrefab is not assigned!");
            yield break;
        }

        ShelfSpawner[] allShelves = FindObjectsByType<ShelfSpawner>(FindObjectsSortMode.None);
        if (allShelves.Length == 0)
        {
            Debug.LogError("[AttentionalBlindness] No ShelfSpawners found in scene!");
            yield break;
        }

        // Pick a random shelf to inject the target into
        ShelfSpawner chosenShelf = allShelves[Random.Range(0, allShelves.Length)];
        _targetInstance = chosenShelf.InjectTarget(uniqueTargetPrefab);

        if (_targetInstance == null)
        {
            Debug.LogError("[AttentionalBlindness] Target injection failed!");
            yield break;
        }

        Debug.Log($"[AttentionalBlindness] Target '{targetDisplayName}' injected on shelf '{chosenShelf.gameObject.name}' " +
                  $"at {_targetInstance.transform.position}");

        // Collect all spawned items across all shelves
        CollectSpawnedItems();

        if (_allSpawnedItems.Count == 0)
        {
            Debug.LogError("[AttentionalBlindness] No spawned items found! " +
                           "Make sure ShelfSpawner tags items as 'SpawnedItem'.");
            yield break;
        }

        // Register existing colliders with XRSimpleInteractable on each item
        foreach (GameObject item in _allSpawnedItems)
            SetupInteractable(item);

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

        // Use existing collider from prefab
        Collider col = item.GetComponentInChildren<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[AttentionalBlindness] '{item.name}' has no Collider — add one to the prefab.");
            return;
        }

        // Register with XRSimpleInteractable's collider list
        if (!interactable.colliders.Contains(col))
            interactable.colliders.Add(col);

        // Listen for selection — uses direct reference, no name matching
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
        // All items except the target are potential anomaly blinkers
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

        instructionText.text = $"Find the <b>{targetDisplayName}</b> on the shelves.\n\n" +
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

        _feedbackDisplay?.ShowSuccess($"Correct! You found the {targetDisplayName}!");

        Debug.Log($"[AttentionalBlindness] Correct! '{targetDisplayName}' found in {_foundTime:F2}s");

        StartCoroutine(ShowReportAfterDelay(2f));
    }

    private void OnWrongItemSelected(string itemName)
    {
        Debug.Log($"[AttentionalBlindness] Wrong item: '{itemName}'");
        _feedbackDisplay?.ShowError($"That's not the {targetDisplayName}. Keep looking!");
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
            $"Target:             {targetDisplayName}\n" +
            $"Search time:        {_foundTime:F1}s\n" +
            $"Noticed anomaly:    {(noticed ? "Yes" : "No")}\n\n" +
            (noticed
                ? "You noticed the blinking items while searching."
                : "You did not notice the blinking items.\nThis is the attentional blindness effect.");

        resultsPanel.SetActive(true);

        Debug.Log($"[AttentionalBlindness] Result — " +
                  $"Target: {targetDisplayName} | " +
                  $"Search time: {_foundTime:F2}s | " +
                  $"Noticed anomaly: {noticed}");

        // TODO: SessionDataManager.Instance.RecordAttentionalBlindnessTrial(targetDisplayName, _foundTime, noticed);
    }
}