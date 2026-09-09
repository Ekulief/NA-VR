using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

/// <summary>
/// SLUBT Labs — Odd Item Detection Manager
/// All experiment parameters are read from ExperimentConfig at runtime.
/// Odd item is selected via dropdown in the Inspector (OddItemManagerEditor).
/// Items are shuffled across all shelves before the trial starts.
/// </summary>
public class OddItemManager : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("Input")]
    public InputActionReference triggerAction;

    [Header("Odd Items")]
    [Tooltip("List of possible odd item prefabs. Select which one to use via the dropdown below.")]
    public List<GameObject> oddItemPrefabs = new();

    [Tooltip("Index of the odd item to inject this trial — set via the Inspector dropdown.")]
    public int selectedOddItemIndex = 0;

    [Header("Shuffle")]
    [Tooltip("If true, item positions within each shelf are shuffled each trial " +
         "so participants cannot memorize slot positions. " +
         "Items stay on their own shelf — only positions within each shelf are randomized.")]
    public bool shuffleItems = true;

    [Header("UI — Instruction Panel")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;

    [Header("UI — Results Panel")]
    public GameObject resultsPanel;
    public TMP_Text resultsSummaryText;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _trialStartTime;
    private float _foundTime;
    private List<GameObject> _allSpawnedItems = new();
    private GameObject _targetInstance;
    private GameObject _chosenOddItemPrefab;
    private bool _awaitingSelection = false;
    private bool _trialComplete = false;
    private FeedbackDisplay _feedbackDisplay;
    private Transform _rightControllerTransform;

    // ── Resolved config values ────────────────────────────────────────────────
    private float _searchTimeLimit;
    private float _raycastDistance;
    private float _instructionDelay;

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
        resultsPanel.SetActive(false);
        _feedbackDisplay = GetComponent<FeedbackDisplay>();

        ExperimentConfig cfg = config;
        if (cfg != null)
        {
            _searchTimeLimit = cfg.oddItem_SearchTimeLimitSeconds;
            _raycastDistance = cfg.oddItem_RaycastDistance;
            _instructionDelay = cfg.globalInstructionDelay;
        }
        else
        {
            _searchTimeLimit = 0f;
            _raycastDistance = 10f;
            _instructionDelay = 1.5f;
        }

        StartCoroutine(InitialiseAfterSpawn());
    }

    // ── Initialisation ────────────────────────────────────────────────────────
    private IEnumerator InitialiseAfterSpawn()
    {
        // Wait one frame for ShelfSpawners to finish Start()
        yield return null;

        if (oddItemPrefabs == null || oddItemPrefabs.Count == 0)
        {
            Debug.LogError("[OddItemDetection] oddItemPrefabs list is empty!");
            yield break;
        }

        // Use selected index from dropdown — clamp for safety
        int index = Mathf.Clamp(selectedOddItemIndex, 0, oddItemPrefabs.Count - 1);
        _chosenOddItemPrefab = oddItemPrefabs[index];
        Debug.Log($"[OddItemDetection] Chosen odd item: '{_chosenOddItemPrefab.name}'");

        _rightControllerTransform = FindRightController();

        ShelfSpawner[] allShelves = FindObjectsByType<ShelfSpawner>(FindObjectsSortMode.None);
        if (allShelves.Length == 0)
        {
            Debug.LogError("[OddItemDetection] No ShelfSpawners found in scene!");
            yield break;
        }
        foreach (ShelfSpawner shelf in allShelves)
            Debug.Log($"Shelf '{shelf.gameObject.name}' has {shelf.GetSpawnedItems().Count} items");
        // Shuffle items across all shelves if enabled
        if (shuffleItems)
            ShuffleAcrossShelves(allShelves);

        // Inject odd item into a random shelf
        ShelfSpawner chosenShelf = allShelves[Random.Range(0, allShelves.Length)];
        _targetInstance = chosenShelf.InjectTarget(_chosenOddItemPrefab);

        if (_targetInstance == null)
        {
            Debug.LogError("[OddItemDetection] Target injection failed!");
            yield break;
        }

        Debug.Log($"[OddItemDetection] '{_chosenOddItemPrefab.name}' injected on " +
                  $"'{chosenShelf.gameObject.name}' at {_targetInstance.transform.position}");

        LayoutManager.Instance?.CaptureLayout(_targetInstance, _chosenOddItemPrefab);

        CollectSpawnedItems();

        foreach (GameObject item in _allSpawnedItems)
            SetupInteractable(item);

        yield return new WaitForSeconds(_instructionDelay);
        BeginTrial();
    }

    // ── Shuffle ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Collects all spawned items across every shelf and redistributes them
    /// randomly so items don't always appear on the same shelf.
    /// </summary>
    private void ShuffleAcrossShelves(ShelfSpawner[] shelves)
    {

        foreach (ShelfSpawner shelf in shelves)
        {

            List<GameObject> items = shelf.GetSpawnedItems();
            if (items == null || items.Count < 2) continue;

            // Collect actual world positions of spawned items
            List<Vector3> positions = new();
            foreach (GameObject item in items)
                if (item != null) positions.Add(item.transform.position);

            // Fisher-Yates shuffle
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }

            // Apply shuffled positions back
            int idx = 0;
            foreach (GameObject item in items)
            {
                if (item == null) continue;
                item.transform.position = positions[idx];
                idx++;
            }

            Debug.Log($"[Shuffle] '{shelf.gameObject.name}' — {positions.Count} positions shuffled.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Transform FindRightController()
    {
        string[] names = { "Ray Interactor", "RayInteractor", "Right Controller", "Right Hand" };
        foreach (string n in names)
        {
            GameObject found = GameObject.Find(n);
            if (found != null) return found.transform;
        }
        var ray = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        return ray != null ? ray.transform : null;
    }

    private void SetupInteractable(GameObject item)
    {
        XRSimpleInteractable interactable = item.GetComponent<XRSimpleInteractable>();
        if (interactable == null) return;

        Collider col = item.GetComponentInChildren<Collider>();
        if (col == null) return;

        if (!interactable.colliders.Contains(col))
            interactable.colliders.Add(col);

        interactable.selectEntered.AddListener((args) => OnItemSelected(item));
    }

    private void CollectSpawnedItems()
    {
        _allSpawnedItems.Clear();
        _allSpawnedItems.AddRange(GameObject.FindGameObjectsWithTag("SpawnedItem"));
        Debug.Log($"[OddItemDetection] Collected {_allSpawnedItems.Count} spawned items.");
    }

    // ── Trial ─────────────────────────────────────────────────────────────────
    private void BeginTrial()
    {
        ExperimentConfig cfg = config;
        instructionText.text = cfg != null
            ? cfg.oddItem_InstructionText
            : "Find the item that doesn't belong.\nPoint at it and pull the trigger.";

        instructionPanel.SetActive(true);
        _trialStartTime = Time.time;
        _awaitingSelection = true;

        if (_searchTimeLimit > 0)
            StartCoroutine(SearchTimeLimitCountdown());
    }

    private IEnumerator SearchTimeLimitCountdown()
    {
        yield return new WaitForSeconds(_searchTimeLimit);
        if (_awaitingSelection && !_trialComplete)
        {
            _awaitingSelection = false;
            _trialComplete = true;
            instructionPanel.SetActive(false);

            resultsSummaryText.text =
                $"Time's up!\n\n" +
                $"Search time: {_searchTimeLimit:F0}s (limit reached)\n\n" +
                $"The odd item was not found in time.";
            resultsPanel.SetActive(true);

            LayoutManager.Instance?.SaveSession(_searchTimeLimit, foundItem: false);
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────
    private void OnItemSelected(GameObject item)
    {
        if (!_awaitingSelection || _trialComplete) return;
        if (item == _targetInstance) OnOddItemFound();
        else OnWrongItemSelected();
    }

    private void OnTriggerPressed(InputAction.CallbackContext ctx)
    {
        if (!_awaitingSelection || _trialComplete) return;
        if (_rightControllerTransform == null) return;

        Ray ray = new Ray(_rightControllerTransform.position, _rightControllerTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance))
        {
            GameObject hitRoot = GetSpawnedItemRoot(hit.collider.gameObject);
            if (hitRoot == null) return;
            if (hitRoot == _targetInstance) OnOddItemFound();
            else OnWrongItemSelected();
        }
    }

    private GameObject GetSpawnedItemRoot(GameObject hit)
    {
        Transform t = hit.transform;
        while (t != null)
        {
            if (t.CompareTag("SpawnedItem")) return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    // ── Outcomes ──────────────────────────────────────────────────────────────
    private void OnOddItemFound()
    {
        _awaitingSelection = false;
        _trialComplete = true;
        _foundTime = Time.time - _trialStartTime;

        instructionPanel.SetActive(false);

        _feedbackDisplay?.ShowSuccess("Correct! That's the odd item!");

        // Get rating from config or fallback
        string timeRating;
        ExperimentConfig cfg = config;
        if (cfg != null)
        {
            if (_foundTime < cfg.oddItem_ExcellentThresholdSeconds)
                timeRating = cfg.oddItem_RatingExcellent;
            else if (_foundTime < cfg.oddItem_GoodThresholdSeconds)
                timeRating = cfg.oddItem_RatingGood;
            else
                timeRating = cfg.oddItem_RatingKeepPracticing;
        }
        else
        {
            timeRating = _foundTime < 10f ? "Excellent!" : _foundTime < 20f ? "Good" : "Keep Practicing";
        }

        resultsSummaryText.text =
            $"Odd Item Found!\n\n" +
            $"Search time:    {_foundTime:F1}s\n" +
            $"Rating:         {timeRating}\n\n" +
            $"The odd item was the {_chosenOddItemPrefab.name}.";

        StartCoroutine(ShowResultsAfterDelay(2f));

        Debug.Log($"[OddItemDetection] Found '{_chosenOddItemPrefab.name}' in {_foundTime:F2}s — " +
                  $"Participant: {config?.participantId}");

        LayoutManager.Instance?.SaveSession(_foundTime, foundItem: true);
    }

    private IEnumerator ShowResultsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        resultsPanel.SetActive(true);
    }

    private void OnWrongItemSelected()
    {
        ExperimentConfig cfg = config;
        string feedback = cfg != null ? cfg.oddItem_WrongItemFeedback : "That item belongs here. Keep looking!";
        _feedbackDisplay?.ShowError(feedback);
        Debug.Log("[OddItemDetection] Wrong item selected.");
    }
}