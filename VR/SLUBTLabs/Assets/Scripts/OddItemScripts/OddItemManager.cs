using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Firebase.Firestore;

/// <summary>
/// Odd-item search trial. Config drives timings/text; results save to experimentResults.
/// </summary>
public class OddItemManager : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("Input")]
    public InputActionReference triggerAction;

    [Header("Odd Items")]
    public List<GameObject> oddItemPrefabs = new();
    public int selectedOddItemIndex = 0;

    [Header("Shuffle")]
    public bool shuffleItems = true;

    [Header("UI — Instruction Panel")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;
    public Button startButton;

    [Header("UI — Results Panel")]
    public GameObject resultsPanel;
    public TMP_Text resultsSummaryText;

    private float _trialStartTime;
    private float _foundTime;
    private float _experimentStartRealtime;
    private DateTime _startedAtUtc;
    private List<GameObject> _allSpawnedItems = new();
    private GameObject _targetInstance;
    private GameObject _chosenOddItemPrefab;
    private bool _awaitingSelection = false;
    private bool _trialComplete = false;
    private FeedbackDisplay _feedbackDisplay;
    private Transform _rightControllerTransform;

    private float _searchTimeLimit;
    private float _raycastDistance;
    private float _instructionDelay;

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

    private void Awake()
    {
        if (!shuffleItems) return;

        ShelfSpawner[] shelves = FindObjectsByType<ShelfSpawner>(FindObjectsSortMode.None);
        if (shelves.Length < 2) return;

        List<List<GameObject>> allLists = new();
        foreach (ShelfSpawner shelf in shelves)
            allLists.Add(new List<GameObject>(shelf.distractorPrefabs));

        for (int i = allLists.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (allLists[i], allLists[j]) = (allLists[j], allLists[i]);
        }

        for (int i = 0; i < shelves.Length; i++)
            shelves[i].distractorPrefabs = allLists[i];

        Debug.Log("[OddItemDetection] Shelf categories shuffled between shelves.");
    }

    private void Start()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        _feedbackDisplay = GetComponent<FeedbackDisplay>();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);

        ExperimentConfig cfg = config != null ? config : ExperimentConfigLoader.Current;
        config = cfg;

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

    private bool IsSessionPaused()
    {
        return SessionController.Instance != null && SessionController.Instance.IsPaused;
    }

    private IEnumerator WaitWhilePaused()
    {
        while (IsSessionPaused())
            yield return null;
    }

    private IEnumerator InitialiseAfterSpawn()
    {
        yield return null;

        if (oddItemPrefabs == null || oddItemPrefabs.Count == 0)
        {
            Debug.LogError("[OddItemDetection] oddItemPrefabs list is empty!");
            yield break;
        }

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

        ShelfSpawner chosenShelf = allShelves[UnityEngine.Random.Range(0, allShelves.Length)];
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
        ShowInstructions();
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

    private void ShowInstructions()
    {
        ExperimentConfig cfg = config;
        if (instructionText != null)
        {
            instructionText.text = cfg != null && !string.IsNullOrEmpty(cfg.oddItem_InstructionText)
                ? cfg.oddItem_InstructionText
                : "Find the item that doesn't belong.\nPoint at it and pull the trigger.";
        }

        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    private void OnStartButtonClicked()
    {
        if (IsSessionPaused()) return;

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        _trialStartTime = Time.time;
        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;
        _awaitingSelection = true;

        if (_searchTimeLimit > 0)
            StartCoroutine(SearchTimeLimitCountdown());
    }

    private IEnumerator SearchTimeLimitCountdown()
    {
        float waited = 0f;
        while (waited < _searchTimeLimit)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }

        if (_awaitingSelection && !_trialComplete)
        {
            _awaitingSelection = false;
            _trialComplete = true;

            if (resultsSummaryText != null)
            {
                resultsSummaryText.text =
                    $"Time's up!\n\n" +
                    $"Search time: {_searchTimeLimit:F0}s (limit reached)\n\n" +
                    $"The odd item was not found in time.";
            }

            if (resultsPanel != null)
                resultsPanel.SetActive(true);

            LayoutManager.Instance?.SaveSession(_searchTimeLimit, foundItem: false);
            SaveResultsToFirestore(_searchTimeLimit, false);
        }
    }

    private void OnItemSelected(GameObject item)
    {
        if (IsSessionPaused()) return;
        if (!_awaitingSelection || _trialComplete) return;

        if (item == _targetInstance) OnOddItemFound();
        else OnWrongItemSelected();
    }

    private void OnTriggerPressed(InputAction.CallbackContext ctx)
    {
        if (IsSessionPaused()) return;
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

    private void OnOddItemFound()
    {
        _awaitingSelection = false;
        _trialComplete = true;
        _foundTime = Time.time - _trialStartTime;

        _feedbackDisplay?.ShowSuccess("Correct! That's the odd item!");

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

        if (resultsSummaryText != null)
        {
            resultsSummaryText.text =
                $"Odd Item Found!\n\n" +
                $"Search time:    {_foundTime:F1}s\n" +
                $"Rating:         {timeRating}\n\n" +
                $"The odd item was the {_chosenOddItemPrefab.name}.";
        }

        StartCoroutine(ShowResultsAfterDelay(2f));
        Debug.Log($"[OddItemDetection] Found '{_chosenOddItemPrefab.name}' in {_foundTime:F2}s");

        LayoutManager.Instance?.SaveSession(_foundTime, foundItem: true);
        SaveResultsToFirestore(_foundTime, true);
    }

    private IEnumerator ShowResultsAfterDelay(float delay)
    {
        float waited = 0f;
        while (waited < delay)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }

        if (resultsPanel != null)
            resultsPanel.SetActive(true);
    }

    private void OnWrongItemSelected()
    {
        if (IsSessionPaused()) return;

        ExperimentConfig cfg = config;
        string feedback = cfg != null ? cfg.oddItem_WrongItemFeedback : "That item belongs here. Keep looking!";
        _feedbackDisplay?.ShowError(feedback);
        Debug.Log("[OddItemDetection] Wrong item selected.");
    }

    private async void SaveResultsToFirestore(float searchTime, bool foundItem)
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
            else
                durationSeconds = searchTime;

            string durationDisplay = FormatDuration(durationSeconds);
            DateTime completedAtUtc = DateTime.UtcNow;

            var configurations = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "searchTimeLimitSeconds", _searchTimeLimit },
                    { "raycastDistance", _raycastDistance },
                    { "shuffleItems", shuffleItems },
                    { "selectedOddItemIndex", selectedOddItemIndex },
                    { "oddItemName", _chosenOddItemPrefab != null ? _chosenOddItemPrefab.name : "" },
                    { "instructionText", config != null ? config.oddItem_InstructionText ?? "" : "" }
                }
            };

            var experimentalResults = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "trialIndex", 0 },
                    { "searchTimeSeconds", searchTime },
                    { "itemFound", foundItem },
                    { "oddItemName", _chosenOddItemPrefab != null ? _chosenOddItemPrefab.name : "" },
                    { "reactionTimeMs", (int)(searchTime * 1000f) }
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
                { "experimentName", "Odd_Item_Detection" },
                { "moduleName", "Odd_Item_Detection" },
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
            Debug.Log($"[OddItemDetection] Saved (duration={durationDisplay}, found={foundItem}, progressId={progressId})");

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
            Debug.LogError($"[OddItemDetection] Failed to save results: {ex.Message}");
        }
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