using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;               // ← needed for Button
using UnityEngine.EventSystems;

public class ExperimentManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────
    public static ExperimentManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);   // destroy the OLD one instead
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);  // only enable this when you really need it across scenes
    }

    // ─────────────────────────────────────────
    // EXPERIMENT STATE
    // ─────────────────────────────────────────
    public enum ExperimentState
    {
        Idle,
        Briefing,
        Exploring,
        Transitioning,
        DistractorTask,
        Recalling,
        Finished
    }

    public ExperimentState CurrentState { get; private set; } = ExperimentState.Idle;

    // ─────────────────────────────────────────
    // INSPECTOR SETTINGS
    // ─────────────────────────────────────────
    public int CurrentRoomIndex => currentRoomIndex;
    public int TotalRooms => rooms.Count;

    [Header("Room Settings")]
    public List<RoomConfig> rooms = new List<RoomConfig>();
    public Transform labRoomSpawnPoint;

    [Header("Timing Settings")]
    public float timePerRoom = 60f;
    public float transitionFadeDuration = 0.5f;
    public float distractorTaskDuration = 30f;

    [Header("Experiment Settings")]
    public bool useDistractorTask = true;
    public bool randomizeQuestions = true;

    [Header("References")]
    public FadeController fadeController;
    public Transform playerTransform;

    [Header("UI References")]
    public Button beginExperimentButton;        
    public GameObject startingPanel;

    [Header("Recall Instruction UI")]
    public GameObject recallInstructionPanel;         
    public Button beginQuestioningButton;
    public TMPro.TextMeshProUGUI recallInstructionTextUI;

    [Header("UI Text References (Optional)")]
    public TMPro.TextMeshProUGUI briefingTextUI;
    public TMPro.TextMeshProUGUI roomInstructionTextUI;
    public TMPro.TextMeshProUGUI distractorInstructionTextUI;
   
    // ─────────────────────────────────────────
    // INTERNAL TRACKING
    // ─────────────────────────────────────────
    private int currentRoomIndex = 0;
    private float roomTimeRemaining = 0f;
    private bool isTimerRunning = false;
    private string currentRoomName = "";

    // Events
    public System.Action<string, float> OnRoomStarted;
    public System.Action<string> OnRoomEnded;
    public System.Action OnAllRoomsExplored;
    public System.Action OnExperimentFinished;
    public System.Action<float> OnTimerTick;
    public System.Action<float> OnDistractorStarted;
    public System.Action OnDistractorEnded;
    public System.Action OnRecallStarted;
    public string BriefingText { get; private set; }
    public string RoomInstructionText { get; private set; }
    public string DistractorInstructionText { get; private set; }
    public string RecallInstructionText { get; private set; }
    // ─────────────────────────────────────────
    // START
    // ─────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(WaitForConfigAndApplyBriefing());
        // Find player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("[ExperimentManager] Player found at runtime.");
            }
            else
            {
                Debug.LogWarning("[ExperimentManager] No Player found. Check Player tag.");
            }

        }

        // Find FadeController
        if (fadeController == null)
        {
            fadeController = FindFirstObjectByType<FadeController>();
            if (fadeController != null)
                Debug.Log("[ExperimentManager] FadeController found at runtime.");
            else
                Debug.LogWarning("[ExperimentManager] No FadeController found.");
        }

        // Wire the Begin Experiment button
        if (beginExperimentButton != null)
        {
            beginExperimentButton.onClick.AddListener(StartExperiment);
            Debug.Log("[ExperimentManager] Begin Experiment button wired.");
        }
        else
        {
            Debug.LogWarning("[ExperimentManager] No Begin Experiment button assigned.");
        }
        // Inside Start(), after the existing beginExperimentButton wiring
        if (beginQuestioningButton != null)
        {
            beginQuestioningButton.onClick.AddListener(BeginQuestioning);
            Debug.Log("[ExperimentManager] Begin Questioning button wired.");
        }
        Debug.Log("[ExperimentManager] Ready. State: Idle");
        ChangeState(ExperimentState.Idle);
    }

    // ─────────────────────────────────────────
    // APPLY CONFIG
    // ─────────────────────────────────────────
    private void ApplyConfig()
    {
        if (ExperimentConfigLoader.Current == null)
        {
            Debug.Log("[ExperimentManager] No ExperimentConfig found — using Inspector values.");
            return;
        }

        var c = ExperimentConfigLoader.Current;

        // Timing & settings
        timePerRoom = c.memory_TimePerRoomSeconds;
        transitionFadeDuration = c.memory_TransitionFadeDuration;
        distractorTaskDuration = c.memory_DistractorTaskDuration;
        useDistractorTask = c.memory_UseDistractorTask;
        randomizeQuestions = c.memory_RandomizeQuestions;

        // Texts
        BriefingText = c.memory_BriefingText;
        RoomInstructionText = c.memory_RoomInstructionText;
        DistractorInstructionText = c.memory_DistractorInstructionText;
        RecallInstructionText = c.memory_RecallInstructionText;

        Debug.Log("[ExperimentManager] Applied Memory config values + texts from ExperimentConfig.");
        if (briefingTextUI != null) briefingTextUI.text = BriefingText;
        if (roomInstructionTextUI != null) roomInstructionTextUI.text = RoomInstructionText;
        if (distractorInstructionTextUI != null) distractorInstructionTextUI.text = DistractorInstructionText;
        if (recallInstructionTextUI != null) recallInstructionTextUI.text = RecallInstructionText;
    }

    // ─────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────
    private void Update()
    {
        if (isTimerRunning && CurrentState == ExperimentState.Exploring)
        {
            roomTimeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(roomTimeRemaining);

            if (roomTimeRemaining <= 0f)
            {
                roomTimeRemaining = 0f;
                isTimerRunning = false;
                OnRoomTimeUp();
            }
        }
    }

    // ─────────────────────────────────────────
    // STATE MACHINE
    // ─────────────────────────────────────────
    private void ChangeState(ExperimentState newState)
    {
        CurrentState = newState;
        Debug.Log($"[ExperimentManager] State changed to: {newState}");
    }

    // ─────────────────────────────────────────
    // PUBLIC METHODS
    // ─────────────────────────────────────────
    public void StartExperiment()
    {
        if (CurrentState != ExperimentState.Idle &&
            CurrentState != ExperimentState.Briefing)
        {
            Debug.LogWarning("[ExperimentManager] Cannot start — wrong state.");
            return;
        }

        // Hide the starting panel
        if (startingPanel != null)
        {
            startingPanel.SetActive(false);
            Debug.Log("[ExperimentManager] Starting panel hidden.");
        }
        else
        {
            Debug.LogWarning("[ExperimentManager] startingPanel is not assigned!");
        }

        ApplyConfig();

        Debug.Log("[ExperimentManager] Experiment started.");
        currentRoomIndex = 0;
        StartCoroutine(GoToNextRoom());
    }
    private IEnumerator WaitForConfigAndApplyBriefing()
    {
        // Wait until the loader has finished
        while (ExperimentConfigLoader.Current == null || !ExperimentConfigLoader.IsReady)
        {
            yield return null;
        }

        ApplyConfig();   // this now sets the briefing text while the panel is still visible
        Debug.Log("[ExperimentManager] Briefing text applied from config.");
    }
    public void OnPlayerEnterRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
        if (roomName != currentRoomName) return;

        Debug.Log($"[ExperimentManager] Player confirmed inside: {roomName}");
    }

    public void OnPlayerExitRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
        Debug.Log($"[ExperimentManager] Player exited: {roomName}");
    }

    // ─────────────────────────────────────────
    // INTERNAL FLOW
    // ─────────────────────────────────────────
    private IEnumerator GoToNextRoom()
    {
        if (currentRoomIndex >= rooms.Count)
        {
            Debug.Log("[ExperimentManager] All rooms explored.");
            OnAllRoomsExplored?.Invoke();
            StartCoroutine(BeginTransitionToLab());
            yield break;
        }

        RoomConfig room = rooms[currentRoomIndex];
        currentRoomName = room.roomName;

        ChangeState(ExperimentState.Transitioning);
        yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        TeleportPlayer(room.spawnPoint);
        Debug.Log($"[ExperimentManager] Teleported to room: {room.roomName}");

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        ChangeState(ExperimentState.Exploring);
        roomTimeRemaining = timePerRoom;
        isTimerRunning = true;

        OnRoomStarted?.Invoke(room.roomName, timePerRoom);
        Debug.Log($"[ExperimentManager] Exploring {room.roomName} — {timePerRoom}s");
    }

    private void OnRoomTimeUp()
    {
        string endedRoom = currentRoomName;
        OnRoomEnded?.Invoke(endedRoom);
        Debug.Log($"[ExperimentManager] Time up for: {endedRoom}");

        currentRoomIndex++;
        StartCoroutine(GoToNextRoom());
    }

    private IEnumerator BeginTransitionToLab()
    {
        ChangeState(ExperimentState.Transitioning);
        yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        TeleportPlayer(labRoomSpawnPoint);
        Debug.Log("[ExperimentManager] Teleported to Lab Room.");

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        // Go to the recall instruction panel first (no distractor yet)
        StartRecallPhase();
    }


    private void StartRecallPhase()
    {
        ChangeState(ExperimentState.Recalling);
        Debug.Log("[ExperimentManager] Recall phase started — showing instruction panel.");

        // Make sure we have the latest text from config
        ApplyConfig();

        // Show the recall instruction panel
        if (recallInstructionPanel != null)
        {
            recallInstructionPanel.SetActive(true);

            if (recallInstructionTextUI != null)
                recallInstructionTextUI.text = RecallInstructionText;
        }
        else
        {
            // Fallback: if no panel is assigned, start questions immediately
            Debug.LogWarning("[ExperimentManager] No recallInstructionPanel assigned — starting questions immediately.");
            BeginQuestioning();
        }
    }
    /// <summary>
    /// Called by the "Begin Questioning" button
    /// </summary>
    public void BeginQuestioning()
    {
        // Hide the recall instruction panel
        if (recallInstructionPanel != null)
            recallInstructionPanel.SetActive(false);

        Debug.Log("[ExperimentManager] Begin Questioning pressed.");

        if (useDistractorTask)
            StartCoroutine(RunDistractorThenQuestions());
        else
            OnRecallStarted?.Invoke();
    }

    private IEnumerator RunDistractorThenQuestions()
    {
        ChangeState(ExperimentState.DistractorTask);
        Debug.Log("[ExperimentManager] Distractor task started.");

        OnDistractorStarted?.Invoke(distractorTaskDuration);

        yield return new WaitForSeconds(distractorTaskDuration);

        OnDistractorEnded?.Invoke();
        Debug.Log("[ExperimentManager] Distractor ended — starting questions.");

        OnRecallStarted?.Invoke();
    }
    public void OnExperimentComplete()
    {
        ChangeState(ExperimentState.Finished);
        OnExperimentFinished?.Invoke();
        Debug.Log("[ExperimentManager] Experiment complete.");
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    private void TeleportPlayer(Transform destination)
    {
        if (destination == null) return;
        // Debug: freeze the player completely for 2 seconds
        CharacterController cc = playerTransform.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = playerTransform.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Debug.Log("[Teleport] CharacterController & Rigidbody disabled — if you still fall, something else is moving you.");
        // Find XR Origin at runtime
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player")
                             ?? GameObject.Find("XR Origin");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[ExperimentManager] No XR Origin found.");
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
            mainCam = playerTransform.GetComponentInChildren<Camera>();

        if (mainCam != null)
        {
            // Match rotation (Y only)
            float currentCamY = mainCam.transform.eulerAngles.y;
            float targetY = destination.eulerAngles.y;
            playerTransform.Rotate(0f, targetY - currentCamY, 0f, Space.World);

            // Horizontal offset (room-scale)
            Vector3 cameraOffset = mainCam.transform.position - playerTransform.position;
            cameraOffset.y = 0f;

            Vector3 finalPosition = destination.position - cameraOffset;

            // Height – match the loader’s fixed height style
            Transform cameraOffsetTf = playerTransform.Find("Camera Offset");
            float localCamY = cameraOffsetTf != null
                ? cameraOffsetTf.localPosition.y
                : mainCam.transform.localPosition.y;

            finalPosition.y = destination.position.y + 1.6f - localCamY; // same as fixedSpawnHeight

            playerTransform.position = finalPosition;
        }
        else
        {
            playerTransform.position = destination.position;
            playerTransform.rotation = destination.rotation;
        }
    }

    // ─────────────────────────────────────────
    // EDITOR HELPER
    // ─────────────────────────────────────────
    private void OnGUI()
    {
#if UNITY_EDITOR
        if (GUILayout.Button("DEBUG: Skip Room Timer"))
        {
            if (isTimerRunning)
                roomTimeRemaining = 0f;
        }
        if (GUILayout.Button("DEBUG: Start Experiment"))
        {
            StartExperiment();
        }
#endif
    }
}

[System.Serializable]
public class RoomConfig
{
    public string roomName;
    public Transform spawnPoint;
    public string displayName;
}