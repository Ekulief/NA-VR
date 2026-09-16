using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
    // (Instructor configures these)
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

    // ─────────────────────────────────────────
    // INTERNAL TRACKING
    // ─────────────────────────────────────────
    private int currentRoomIndex = 0;
    private float roomTimeRemaining = 0f;
    private bool isTimerRunning = false;
    private string currentRoomName = "";

    // Events — other scripts listen to these
    public System.Action<string, float> OnRoomStarted;
    public System.Action<string> OnRoomEnded;
    public System.Action OnAllRoomsExplored;
    public System.Action OnExperimentFinished;
    public System.Action<float> OnTimerTick;

    // ─────────────────────────────────────────
    // START
    // ─────────────────────────────────────────
    private void Start()
    {
        // Find player at runtime instead of Inspector drag
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

        // Find FadeController at runtime
        if (fadeController == null)
        {
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (fadeController != null)
                Debug.Log("[ExperimentManager] FadeController found at runtime.");
            else
                Debug.LogWarning("[ExperimentManager] No FadeController found.");
        }

        Debug.Log("[ExperimentManager] Ready. State: Idle");
        ChangeState(ExperimentState.Idle);
    }

    // ─────────────────────────────────────────
    // UPDATE — runs every frame
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
    // PUBLIC — Called by UI buttons / RoomBoundary
    // ─────────────────────────────────────────

    /// <summary>
    /// Called by the Start Experiment button in the briefing screen
    /// </summary>
    public void StartExperiment()
    {
        if (CurrentState != ExperimentState.Idle &&
            CurrentState != ExperimentState.Briefing)
        {
            Debug.LogWarning("[ExperimentManager] Cannot start — wrong state.");
            return;
        }

        Debug.Log("[ExperimentManager] Experiment started.");
        currentRoomIndex = 0;
        StartCoroutine(GoToNextRoom());
    }

    /// <summary>
    /// Called by RoomBoundary when player enters a room
    /// </summary>
    public void OnPlayerEnterRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
        if (roomName != currentRoomName) return;

        Debug.Log($"[ExperimentManager] Player confirmed inside: {roomName}");
    }

    /// <summary>
    /// Called by RoomBoundary when player exits a room
    /// </summary>
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
        // Check if all rooms are done
        if (currentRoomIndex >= rooms.Count)
        {
            Debug.Log("[ExperimentManager] All rooms explored.");
            OnAllRoomsExplored?.Invoke();
            StartCoroutine(BeginTransitionToLab());
            yield break;
        }

        RoomConfig room = rooms[currentRoomIndex];
        currentRoomName = room.roomName;

        // Fade out
        ChangeState(ExperimentState.Transitioning);
        yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        // Teleport player to this room
        TeleportPlayer(room.spawnPoint);
        Debug.Log($"[ExperimentManager] Teleported to room: {room.roomName}");

        // Short pause while screen is black
        yield return new WaitForSeconds(0.5f);

        // Fade in
        yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        // Start exploration timer
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
        // Fade out
        ChangeState(ExperimentState.Transitioning);
        yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        // Teleport to lab
        TeleportPlayer(labRoomSpawnPoint);
        Debug.Log("[ExperimentManager] Teleported to Lab Room.");

        yield return new WaitForSeconds(0.5f);

        // Fade in
        yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        // Either start distractor or go straight to recall
        if (useDistractorTask)
        {
            StartCoroutine(RunDistractorTask());
        }
        else
        {
            StartRecallPhase();
        }
    }

    private IEnumerator RunDistractorTask()
    {
        ChangeState(ExperimentState.DistractorTask);
        Debug.Log("[ExperimentManager] Distractor task started.");

        // DistractorTaskUI will listen to this state change and show itself
        yield return new WaitForSeconds(distractorTaskDuration);

        Debug.Log("[ExperimentManager] Distractor task ended.");
        StartRecallPhase();
    }

    private void StartRecallPhase()
    {
        ChangeState(ExperimentState.Recalling);
        Debug.Log("[ExperimentManager] Recall phase started.");
        // RecallManager will listen to this state and begin showing questions
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
        if (playerTransform == null || destination == null)
        {
            Debug.LogWarning("[ExperimentManager] Missing player or destination.");
            return;
        }

        playerTransform.position = destination.position;
        playerTransform.rotation = destination.rotation;
    }

    // ─────────────────────────────────────────
    // EDITOR HELPER — Press G in Play Mode to skip to next room
    // ─────────────────────────────────────────
    private void OnGUI()
    {
#if UNITY_EDITOR
        if (GUILayout.Button("DEBUG: Skip Room Timer"))
        {
            if (isTimerRunning)
            {
                roomTimeRemaining = 0f;
            }
        }
        if (GUILayout.Button("DEBUG: Start Experiment"))
        {
            StartExperiment();
        }
#endif
    }
}

// ─────────────────────────────────────────
// ROOM CONFIG — Defined per room in Inspector
// ─────────────────────────────────────────
[System.Serializable]
public class RoomConfig
{
    public string roomName;
    public Transform spawnPoint;
    public string displayName;  // shown on the HUD e.g. "Kitchen"
}