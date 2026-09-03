using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

/// <summary>
/// SLUBT Labs — Odd Item Detection Manager
/// All experiment parameters are read from ExperimentConfig at runtime.
/// To change parameters: edit the ExperimentConfig asset or push values
/// from the web app via ExperimentConfig.ApplyFromJson().
/// </summary>
public class OddItemManager : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Assign your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    [Header("Input")]
    public InputActionReference triggerAction;

    [Header("Odd Items")]
    [Tooltip("List of possible odd item prefabs — one is randomly chosen each trial. " +
             "None of these should appear in any ShelfSpawner distractor list.")]
    public List<GameObject> oddItemPrefabs = new();

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

    // ── Resolved config values (cached at init) ───────────────────────────────
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

        // Apply config immediately — no blocking wait
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
            Debug.LogError("[OddItemDetection] oddItemPrefabs list is empty! Add at least one odd item prefab.");
            yield break;
        }

        // Randomly pick one odd item from the list
        _chosenOddItemPrefab = oddItemPrefabs[Random.Range(0, oddItemPrefabs.Count)];
        Debug.Log($"[OddItemDetection] Chosen odd item: '{_chosenOddItemPrefab.name}'");

        _rightControllerTransform = FindRightController();

        // Inject chosen odd item into a random shelf
        ShelfSpawner[] allShelves = FindObjectsByType<ShelfSpawner>(FindObjectsSortMode.None);
        if (allShelves.Length == 0)
        {
            Debug.LogError("[OddItemDetection] No ShelfSpawners found in scene!");
            yield break;
        }

        ShelfSpawner chosenShelf = allShelves[Random.Range(0, allShelves.Length)];
        _targetInstance = chosenShelf.InjectTarget(_chosenOddItemPrefab);

        if (_targetInstance == null)
        {
            Debug.LogError("[OddItemDetection] Target injection failed!");
            yield break;
        }

        Debug.Log($"[OddItemDetection] '{_chosenOddItemPrefab.name}' injected on " +
                  $"'{chosenShelf.gameObject.name}' at {_targetInstance.transform.position}");

        // Capture layout for participant comparison
        LayoutManager.Instance?.CaptureLayout(_targetInstance, _chosenOddItemPrefab);

        CollectSpawnedItems();

        foreach (GameObject item in _allSpawnedItems)
            SetupInteractable(item);

        yield return new WaitForSeconds(_instructionDelay);
        BeginTrial();
    }

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

        string timeRating = _foundTime < 10f ? "Excellent!" : _foundTime < 20f ? "Good" : "Keep practicing";

        resultsSummaryText.text =
            $"Odd Item Found!\n\n" +
            $"Search time:    {_foundTime:F1}s\n" +
            $"Rating:         {timeRating}\n\n" +
            $"The odd item was the {_chosenOddItemPrefab.name}.";

        resultsPanel.SetActive(true);

        Debug.Log($"[OddItemDetection] Found '{_chosenOddItemPrefab.name}' in {_foundTime:F2}s — " +
                  $"Participant: {config?.participantId}");

        // Save session with layout for comparison
        LayoutManager.Instance?.SaveSession(_foundTime, foundItem: true);

        // TODO: SessionDataManager.Instance.RecordOddItemTrial(_chosenOddItemPrefab.name, _foundTime);
    }

    private void OnWrongItemSelected()
    {
        ExperimentConfig cfg = config;
        string feedback = cfg != null ? cfg.oddItem_WrongItemFeedback : "That item belongs here. Keep looking!";
        _feedbackDisplay?.ShowError(feedback);
    }
}