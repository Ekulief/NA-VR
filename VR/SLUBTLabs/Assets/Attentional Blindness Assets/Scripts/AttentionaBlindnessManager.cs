using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro; // still needed for reportQuestionText and resultsSummaryText

/// <summary>
/// SLUBT Labs — Attentional Blindness Manager
///
/// Participant points ray at target item and pulls trigger to confirm they found it.
/// Uses a physics raycast from the right controller — same ray the XR Interactor uses.
///
/// FLOW:
///   1. Waits one frame for ShelfSpawners to finish
///   2. Picks anomaly items and starts blinking them
///   3. Shows instruction — participant searches and points at target
///   4. When ray hits target and trigger is pulled → confirmed
///   5. Blinking stops, wrong item feedback if they picked wrong
///   6. Post-task awareness report panel
///   7. Results recorded
///
/// SETUP:
///   a) Attach to empty GameObject "AttentionalBlindnessManager"
///   b) Assign rightControllerTransform — drag Right Controller GameObject
///   c) Assign triggerAction — XRI Default / Activate
///   d) Assign UI panels
///   e) Set targetItemName to match your target prefab name
///   f) Tag all item prefabs as "SpawnedItem" in Project window
/// </summary>
public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag your Right Controller (or Ray Interactor) GameObject here.")]
    public Transform rightControllerTransform;

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

    // Feedback display is created at runtime by FeedbackDisplay component
    private FeedbackDisplay _feedbackDisplay;

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

        CollectSpawnedItems();

        if (_allSpawnedItems.Count == 0)
        {
            Debug.LogError("[AttentionalBlindness] No spawned items found! " +
                           "Make sure item prefabs are tagged 'SpawnedItem'.");
            yield break;
        }

        _targetInstance = _allSpawnedItems.Find(item =>
            item.name.StartsWith(targetItemName, System.StringComparison.OrdinalIgnoreCase));

        if (_targetInstance == null)
            Debug.LogWarning($"[AttentionalBlindness] No item matching '{targetItemName}' found among spawned items.");
        else
            Debug.Log($"[AttentionalBlindness] Target: '{_targetInstance.name}' at {_targetInstance.transform.position}");

        // Make sure target has a collider so raycast can hit it
        if (_targetInstance != null && _targetInstance.GetComponentInChildren<Collider>() == null)
        {
            _targetInstance.AddComponent<BoxCollider>();
            Debug.Log("[AttentionalBlindness] Added BoxCollider to target item.");
        }

        // Make sure all items have colliders for wrong-selection detection
        foreach (GameObject item in _allSpawnedItems)
        {
            if (item.GetComponentInChildren<Collider>() == null)
                item.AddComponent<BoxCollider>();
        }

        yield return new WaitForSeconds(instructionDelay);
        BeginTrial();
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
        // Add AnomalyBlinker to random distractor items
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

    // ── Input ─────────────────────────────────────────────────────────────────
    private void OnTriggerPressed(InputAction.CallbackContext ctx)
    {
        if (!_awaitingSelection || _trialComplete) return;

        // Raycast from right controller forward
        if (rightControllerTransform == null)
        {
            Debug.LogWarning("[AttentionalBlindness] rightControllerTransform not assigned!");
            return;
        }

        Ray ray = new Ray(rightControllerTransform.position, rightControllerTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Walk up to find the root spawned item
            GameObject hitRoot = GetSpawnedItemRoot(hitObject);

            if (hitRoot == null)
            {
                Debug.Log($"[AttentionalBlindness] Ray hit '{hitObject.name}' — not a spawned item.");
                return;
            }

            if (hitRoot == _targetInstance)
            {
                OnTargetFound();
            }
            else
            {
                OnWrongItemSelected(hitRoot.name);
            }
        }
        else
        {
            Debug.Log("[AttentionalBlindness] Trigger pressed but ray hit nothing.");
        }
    }

    /// <summary>Walks up the hierarchy to find the root SpawnedItem tagged GameObject.</summary>
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

    private void OnTargetFound()
    {
        _awaitingSelection = false;
        _trialComplete = true;
        _foundTime = Time.time - _trialStartTime;

        StopAnomalies();
        instructionPanel.SetActive(false);

        _feedbackDisplay?.HideFeedback();

        Debug.Log($"[AttentionalBlindness] Correct! Target found in {_foundTime:F2}s");

        // Highlight the target briefly then show report
        StartCoroutine(ShowReportAfterDelay(0.5f));
    }

    private void OnWrongItemSelected(string itemName)
    {
        Debug.Log($"[AttentionalBlindness] Wrong item selected: '{itemName}'");

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