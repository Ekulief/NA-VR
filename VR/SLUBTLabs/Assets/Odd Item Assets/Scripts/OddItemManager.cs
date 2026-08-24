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
///
/// Participant must find and select the odd item among the shelves as fast as possible.
/// The moment they select the correct odd item, search time summary appears instantly.
/// No anomaly blinking, no yes/no report — just pure visual search timing.
///
/// FLOW:
///   1. Waits one frame for ShelfSpawners to finish
///   2. Injects unique odd item into a random shelf slot
///   3. Registers colliders with XRSimpleInteractable on all spawned items
///   4. Shows instruction — participant searches for the odd item
///   5. Correct item selected → summary appears instantly with search time
///   6. Wrong item selected → brief feedback, keep searching
/// </summary>
public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Assign: XRI Default Input Actions → XRI Right Hand Interaction → Activate")]
    public InputActionReference triggerAction;

    [Header("Experiment Config")]
    [Tooltip("The unique odd item prefab — must NOT be in any ShelfSpawner distractor list.")]
    public GameObject uniqueTargetPrefab;

    [Tooltip("Display name shown in the instruction e.g. 'odd item'.")]
    public string targetDisplayName = "odd item";

    [Tooltip("Max raycast distance for item selection.")]
    public float raycastDistance = 10f;

    [Tooltip("Seconds after scene load before instruction appears.")]
    public float instructionDelay = 1.5f;

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
        resultsPanel.SetActive(false);

        _feedbackDisplay = GetComponent<FeedbackDisplay>();

        StartCoroutine(InitialiseAfterSpawn());
    }

    // ── Initialisation ────────────────────────────────────────────────────────
    private IEnumerator InitialiseAfterSpawn()
    {
        yield return null;

        _rightControllerTransform = FindRightController();
        if (_rightControllerTransform == null)
            Debug.LogWarning("[OddItemDetection] Could not find Right Controller.");

        if (uniqueTargetPrefab == null)
        {
            Debug.LogError("[OddItemDetection] uniqueTargetPrefab is not assigned!");
            yield break;
        }

        // Inject unique odd item into a random shelf
        ShelfSpawner[] allShelves = FindObjectsByType<ShelfSpawner>(FindObjectsSortMode.None);
        if (allShelves.Length == 0)
        {
            Debug.LogError("[OddItemDetection] No ShelfSpawners found in scene!");
            yield break;
        }

        ShelfSpawner chosenShelf = allShelves[Random.Range(0, allShelves.Length)];
        _targetInstance = chosenShelf.InjectTarget(uniqueTargetPrefab);

        if (_targetInstance == null)
        {
            Debug.LogError("[OddItemDetection] Target injection failed!");
            yield break;
        }

        Debug.Log($"[OddItemDetection] Odd item injected on '{chosenShelf.gameObject.name}' " +
                  $"at {_targetInstance.transform.position}");

        // Collect all spawned items and set up interactables
        CollectSpawnedItems();

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
            Debug.LogWarning($"[OddItemDetection] '{item.name}' has no XRSimpleInteractable.");
            return;
        }

        Collider col = item.GetComponentInChildren<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[OddItemDetection] '{item.name}' has no Collider.");
            return;
        }

        if (!interactable.colliders.Contains(col))
            interactable.colliders.Add(col);

        interactable.selectEntered.AddListener((args) => OnItemSelected(item));
    }

    private void CollectSpawnedItems()
    {
        _allSpawnedItems.Clear();
        GameObject[] found = GameObject.FindGameObjectsWithTag("SpawnedItem");
        _allSpawnedItems.AddRange(found);
        Debug.Log($"[OddItemDetection] Collected {_allSpawnedItems.Count} spawned items.");
    }

    // ── Trial flow ────────────────────────────────────────────────────────────
    private void BeginTrial()
    {
        instructionText.text = $"Find the item that doesn't belong on the shelves.\n\n" +
                               $"Point at it and pull the <b>trigger</b> to confirm.";
        instructionPanel.SetActive(true);

        _trialStartTime = Time.time;
        _awaitingSelection = true;

        Debug.Log("[OddItemDetection] Trial started — participant searching for odd item.");
    }

    // ── Item selection via XRSimpleInteractable ───────────────────────────────
    private void OnItemSelected(GameObject item)
    {
        if (!_awaitingSelection || _trialComplete) return;

        if (item == _targetInstance)
            OnOddItemFound();
        else
            OnWrongItemSelected();
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
                OnOddItemFound();
            else
                OnWrongItemSelected();
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
    private void OnOddItemFound()
    {
        _awaitingSelection = false;
        _trialComplete = true;
        _foundTime = Time.time - _trialStartTime;

        instructionPanel.SetActive(false);

        // Show summary instantly
        string timeRating = _foundTime < 10f ? "Excellent!" : _foundTime < 20f ? "Good" : "Keep practicing";

        resultsSummaryText.text =
            $"Odd Item Found!\n\n" +
            $"Search time:    {_foundTime:F1}s\n" +
            $"Rating:         {timeRating}\n\n" +
            $"The odd item was the {targetDisplayName}.";

        resultsPanel.SetActive(true);

        Debug.Log($"[OddItemDetection] Odd item found in {_foundTime:F2}s");

        // TODO: SessionDataManager.Instance.RecordOddItemTrial(targetDisplayName, _foundTime);
    }

    private void OnWrongItemSelected()
    {
        _feedbackDisplay?.ShowError("That item belongs here. Keep looking!");
        Debug.Log("[OddItemDetection] Wrong item selected.");
    }
}