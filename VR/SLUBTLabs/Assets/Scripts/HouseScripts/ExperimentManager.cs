using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using TMPro;

/// <summary>
/// Memory experiment flow: rooms → optional distractor → recall questions.
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;
    }

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
    public TextMeshProUGUI recallInstructionTextUI;

    [Header("UI Text References (Optional)")]
    public TextMeshProUGUI briefingTextUI;
    public TextMeshProUGUI roomInstructionTextUI;
    public TextMeshProUGUI distractorInstructionTextUI;

    private int currentRoomIndex = 0;
    private float roomTimeRemaining = 0f;
    private bool isTimerRunning = false;
    private string currentRoomName = "";
    private float _experimentStartRealtime;
    private DateTime _startedAtUtc;
    private readonly List<Dictionary<string, object>> _roomResults = new();
    private readonly List<Dictionary<string, object>> _questionResults = new();

    public Action<string, float> OnRoomStarted;
    public Action<string> OnRoomEnded;
    public Action OnAllRoomsExplored;
    public Action OnExperimentFinished;
    public Action<float> OnTimerTick;
    public Action<float> OnDistractorStarted;
    public Action OnDistractorEnded;
    public Action OnRecallStarted;

    public string BriefingText { get; private set; }
    public string RoomInstructionText { get; private set; }
    public string DistractorInstructionText { get; private set; }
    public string RecallInstructionText { get; private set; }

    private void Start()
    {
        StartCoroutine(WaitForConfigAndApplyBriefing());

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (fadeController == null)
            fadeController = FindFirstObjectByType<FadeController>();

        if (beginExperimentButton != null)
            beginExperimentButton.onClick.AddListener(StartExperiment);

        if (beginQuestioningButton != null)
            beginQuestioningButton.onClick.AddListener(BeginQuestioning);

        StartCoroutine(SubscribeQuestionAnswers());
        ChangeState(ExperimentState.Idle);
    }

    private IEnumerator SubscribeQuestionAnswers()
    {
        while (QuestionManager.Instance == null)
            yield return new WaitForSeconds(0.1f);

        QuestionManager.Instance.OnAnswerSubmitted -= OnQuestionAnswered;
        QuestionManager.Instance.OnAnswerSubmitted += OnQuestionAnswered;
    }

    private void OnDestroy()
    {
        if (QuestionManager.Instance != null)
            QuestionManager.Instance.OnAnswerSubmitted -= OnQuestionAnswered;

        if (Instance == this)
            Instance = null;
    }

    private void OnQuestionAnswered(QuestionEntry entry, string answer, bool correct, float rtMs)
    {
        _questionResults.Add(new Dictionary<string, object>
        {
            { "objectID", entry.objectID ?? "" },
            { "objectName", entry.objectName ?? "" },
            { "room", entry.room ?? "" },
            { "questionText", entry.questionText ?? "" },
            { "questionType", entry.questionType.ToString() },
            { "correctAnswer", entry.correctAnswer ?? "" },
            { "selectedAnswer", answer ?? "" },
            { "correct", correct },
            { "reactionTimeMs", rtMs }
        });

        ExperimentLogger.Log("question_answered",
            $"Q: {(entry.questionText ?? "").Substring(0, Math.Min(40, (entry.questionText ?? "").Length))}... → {(correct ? "Correct" : "Wrong")}",
            new Dictionary<string, object>
            {
                { "objectName", entry.objectName ?? "" },
                { "room", entry.room ?? "" },
                { "correct", correct },
                { "reactionTimeMs", rtMs },
                { "selectedAnswer", answer ?? "" }
            });
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

    private void ApplyConfig()
    {
        if (ExperimentConfigLoader.Current == null)
        {
            Debug.Log("[ExperimentManager] No config — using Inspector values.");
            return;
        }

        var c = ExperimentConfigLoader.Current;

        timePerRoom = c.memory2_TimePerRoomSeconds > 0f ? c.memory2_TimePerRoomSeconds : timePerRoom;
        transitionFadeDuration = c.memory2_TransitionFadeDuration > 0f ? c.memory2_TransitionFadeDuration : transitionFadeDuration;
        distractorTaskDuration = c.memory2_DistractorTaskDuration > 0f ? c.memory2_DistractorTaskDuration : distractorTaskDuration;
        useDistractorTask = c.memory2_UseDistractorTask;
        randomizeQuestions = c.memory2_RandomizeQuestions;

        BriefingText = c.memory2_BriefingText;
        RoomInstructionText = c.memory2_RoomInstructionText;
        DistractorInstructionText = c.memory2_DistractorInstructionText;
        RecallInstructionText = c.memory2_RecallInstructionText;

        if (briefingTextUI != null && !string.IsNullOrEmpty(BriefingText)) briefingTextUI.text = BriefingText;
        if (roomInstructionTextUI != null && !string.IsNullOrEmpty(RoomInstructionText)) roomInstructionTextUI.text = RoomInstructionText;
        if (distractorInstructionTextUI != null && !string.IsNullOrEmpty(DistractorInstructionText)) distractorInstructionTextUI.text = DistractorInstructionText;
        if (recallInstructionTextUI != null && !string.IsNullOrEmpty(RecallInstructionText)) recallInstructionTextUI.text = RecallInstructionText;

        Debug.Log("[ExperimentManager] Applied Memory2 config.");
    }

    private void Update()
    {
        if (IsSessionPaused()) return;

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

    private void ChangeState(ExperimentState newState)
    {
        CurrentState = newState;
        Debug.Log($"[ExperimentManager] State: {newState}");
    }

    public void StartExperiment()
    {
        if (IsSessionPaused()) return;

        if (CurrentState != ExperimentState.Idle && CurrentState != ExperimentState.Briefing)
        {
            Debug.LogWarning("[ExperimentManager] Cannot start — wrong state.");
            return;
        }

        if (startingPanel != null)
            startingPanel.SetActive(false);

        ApplyConfig();

        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;
        _roomResults.Clear();
        _questionResults.Clear();
        currentRoomIndex = 0;

        Debug.Log("[ExperimentManager] Experiment started.");
        ExperimentLogger.Log("experiment_started", "Memory experiment started");

        StartCoroutine(GoToNextRoom());
    }

    private IEnumerator WaitForConfigAndApplyBriefing()
    {
        while (ExperimentConfigLoader.Current == null || !ExperimentConfigLoader.IsReady)
            yield return null;

        ApplyConfig();
        SceneReadySignal.MarkReady();
    }
    public void OnPlayerEnterRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
        if (roomName != currentRoomName) return;
        Debug.Log($"[ExperimentManager] Player successfully entered active room: {roomName}");
    }

    public void OnPlayerExitRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
        Debug.Log($"[ExperimentManager] Player exited room: {roomName}");
    }
    private IEnumerator GoToNextRoom()
    {
        if (currentRoomIndex >= rooms.Count)
        {
            OnAllRoomsExplored?.Invoke();
            StartCoroutine(BeginTransitionToLab());
            yield break;
        }

        RoomConfig room = rooms[currentRoomIndex];
        currentRoomName = room.roomName;

        ChangeState(ExperimentState.Transitioning);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        TeleportPlayer(room.spawnPoint);
        yield return new WaitForSeconds(0.5f);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        ChangeState(ExperimentState.Exploring);
        roomTimeRemaining = timePerRoom;
        isTimerRunning = true;

        OnRoomStarted?.Invoke(room.roomName, timePerRoom);
    }

    private void OnRoomTimeUp()
    {
        string endedRoom = currentRoomName;

        _roomResults.Add(new Dictionary<string, object>
        {
            { "roomIndex", currentRoomIndex },
            { "roomName", endedRoom ?? "" },
            { "timeAllottedSeconds", timePerRoom }
        });

        OnRoomEnded?.Invoke(endedRoom);

        currentRoomIndex++;
        StartCoroutine(GoToNextRoom());
    }

    private IEnumerator BeginTransitionToLab()
    {
        ChangeState(ExperimentState.Transitioning);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        TeleportPlayer(labRoomSpawnPoint);
        yield return new WaitForSeconds(0.5f);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        StartRecallPhase();
    }

    private void StartRecallPhase()
    {
        ChangeState(ExperimentState.Recalling);
        ApplyConfig();

        // FIXED: Explicitly show the recall instruction panel and STOP here 
        // until the user manually clicks the "Begin Questions" button.
        if (recallInstructionPanel != null)
        {
            recallInstructionPanel.SetActive(true);
            if (recallInstructionTextUI != null)
                recallInstructionTextUI.text = RecallInstructionText;
        }
        else
        {
            // Fallback if no panel is assigned: start immediately
            BeginQuestioning();
        }
    }

    public void BeginQuestioning()
    {
        if (IsSessionPaused()) return;

        // 1. Hide the Recall Instruction panel
        if (recallInstructionPanel != null)
            recallInstructionPanel.SetActive(false);

        // 2. If distractor is enabled, start the distractor phase sequence
        if (useDistractorTask)
        {
            StartCoroutine(RunDistractorThenQuestions());
        }
        else
        {
            // Skip distractor and jump straight to questions
            OnRecallStarted?.Invoke();
        }
    }

    private bool isDistractorTimerRunning = false;

    private IEnumerator RunDistractorThenQuestions()
    {
        ChangeState(ExperimentState.DistractorTask);

        string source = (ExperimentConfigLoader.Current != null) ? "Firestore / Remote Config" : "Local Inspector Fallback";
        Debug.Log($"[ExperimentManager] Starting Distractor Task immediately. Duration: {distractorTaskDuration}s | Source: {source}");

        OnDistractorStarted?.Invoke(distractorTaskDuration);

        float timeRemaining = distractorTaskDuration;
        while (timeRemaining > 0f)
        {
            yield return WaitWhilePaused();
            timeRemaining -= Time.deltaTime;

            OnTimerTick?.Invoke(timeRemaining);
            yield return null;
        }

        Debug.Log("[ExperimentManager] Distractor Task finished. Launching questions instantly.");
        OnDistractorEnded?.Invoke();
        OnRecallStarted?.Invoke();
    }
    public void StartDistractorTimer()
    {
        isDistractorTimerRunning = true;
    }


    public void OnExperimentComplete()
    {
        if (CurrentState == ExperimentState.Finished) return;
        ChangeState(ExperimentState.Finished);

        int correctCount = 0;
        foreach (var q in _questionResults)
        {
            if (q.TryGetValue("correct", out object c) && c is bool b && b)
                correctCount++;
        }

        string summary =
            $"Rooms completed: {_roomResults.Count}\n" +
            $"Questions:       {_questionResults.Count}\n" +
            $"Correct:         {correctCount}\n\n" +
            $"Time per room:   {timePerRoom:F0}s\n" +
            (useDistractorTask ? $"Distractor:      {distractorTaskDuration:F0}s\n" : "") +
            "\nData has been uploaded.";

        var loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader != null)
        {
            loader.QueueExperimentSummary("Memory 2 – Results", summary);
            SaveResultsToFirestore();
            loader.ReturnToHub();
        }
        else
        {
            // Fallback
            SaveResultsToFirestore();
        }

        OnExperimentFinished?.Invoke();
        Debug.Log("[ExperimentManager] Experiment complete – returning to hub.");
    }
    // Teleport logic mirrors MemoryExperimentManager.TeleportPlayer, which is known to be solid on the headset.
    private void TeleportPlayer(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogError("[ExperimentManager] Teleport failed: destination is not assigned.");
            return;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player")
                ?? GameObject.Find("VR Player")
                ?? GameObject.Find("XR Origin")
                ?? GameObject.Find("XR Origin (XR Rig)");

            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[ExperimentManager] Teleport failed: player rig not found.");
            return;
        }

        CharacterController cc = playerTransform.GetComponentInChildren<CharacterController>();
        Rigidbody rb = playerTransform.GetComponentInChildren<Rigidbody>();

        // Remember the rig's ORIGINAL physics state so it can be put back exactly as it was.
        // (The old code force-enabled gravity afterwards, which let the player fall through the floor.)
        bool controllerWasEnabled = cc != null && cc.enabled;
        bool wasKinematic = rb != null && rb.isKinematic;
        bool usedGravity = rb != null && rb.useGravity;

        if (cc != null)
            cc.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Camera vrCamera = Camera.main;
        if (vrCamera == null)
            vrCamera = playerTransform.GetComponentInChildren<Camera>();

        Vector3 targetForward = Vector3.ProjectOnPlane(destination.forward, Vector3.up).normalized;

        if (targetForward.sqrMagnitude > 0.001f)
        {
            if (vrCamera != null)
            {
                float cameraYaw = vrCamera.transform.eulerAngles.y;
                float targetYaw = Quaternion.LookRotation(targetForward).eulerAngles.y;
                playerTransform.Rotate(0f, targetYaw - cameraYaw, 0f, Space.World);
            }
            else
            {
                playerTransform.rotation = Quaternion.LookRotation(targetForward, Vector3.up);
            }
        }

        // Rig origin goes ON the floor. Like the Memory manager, the spawn point's own height is used;
        // if the spawn point is floating (e.g. at head height), snap down to the real floor below it.
        float originY = FindFloorY(destination.position);

        if (vrCamera != null)
        {
            Vector3 cameraOffset = vrCamera.transform.position - playerTransform.position;
            cameraOffset.y = 0f;

            playerTransform.position = new Vector3(
                destination.position.x - cameraOffset.x,
                originY,
                destination.position.z - cameraOffset.z);
        }
        else
        {
            playerTransform.position = new Vector3(destination.position.x, originY, destination.position.z);
        }

        Debug.Log($"[ExperimentManager] Teleported to {destination.name}. " +
                  $"Spawn={destination.position}, Origin={playerTransform.position}, " +
                  $"Camera={(vrCamera != null ? vrCamera.transform.position.ToString() : "not found")}");

        StartCoroutine(RestorePlayerAfterTeleport(cc, controllerWasEnabled, rb, wasKinematic, usedGravity));
    }

    // Looks for the floor just under the spawn point. If none is found (or the spawn point already
    // sits on the floor) the spawn point's own height is used, exactly like MemoryExperimentManager.
    private float FindFloorY(Vector3 from)
    {
        Vector3 start = from + Vector3.up * 0.5f;
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 3.5f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (playerTransform != null && hit.collider.transform.IsChildOf(playerTransform))
                continue;
            return hit.point.y;
        }

        Debug.LogWarning("[ExperimentManager] No floor collider found under the spawn point; using the spawn point's height. " +
                         "Make sure the floor has a collider and the spawn point sits on it.");
        return from.y;
    }

    // Wait for a physics step before handing control back, so the CharacterController/Rigidbody
    // pick up the NEW position instead of the old one, and restore the original gravity state.
    private IEnumerator RestorePlayerAfterTeleport(
        CharacterController cc, bool controllerWasEnabled,
        Rigidbody rb, bool wasKinematic, bool usedGravity)
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        if (cc != null)
            cc.enabled = controllerWasEnabled;

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = usedGravity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private async void SaveResultsToFirestore()
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            var config = ExperimentConfigLoader.Current;

            string studentId = GetId(config, "studentId", "Anonymous");
            string groupId = GetId(config, "groupId", "");
            string blockId = GetId(config, "blockId", "");
            string experimentId = GetId(config, "experimentId", "");
            string vrId = GetId(config, "vrId", "");
            string progressId = GetId(config, "progressId", "");

            if (string.IsNullOrEmpty(progressId) && SessionController.Instance != null)
                progressId = SessionController.Instance.progressDocumentId ?? "";

            float durationSeconds = _experimentStartRealtime > 0f
                ? Time.realtimeSinceStartup - _experimentStartRealtime
                : 0f;

            string durationDisplay = FormatDuration(durationSeconds);
            DateTime completedAtUtc = DateTime.UtcNow;

            var configurations = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "timePerRoomSeconds", timePerRoom },
                    { "transitionFadeDuration", transitionFadeDuration },
                    { "distractorTaskDuration", distractorTaskDuration },
                    { "useDistractorTask", useDistractorTask },
                    { "randomizeQuestions", randomizeQuestions },
                    { "totalRooms", rooms.Count },
                    { "briefingText", BriefingText ?? "" },
                    { "recallInstructionText", RecallInstructionText ?? "" }
                }
            };

            var experimentalResults = new List<object>();
            foreach (var r in _roomResults)
                experimentalResults.Add(r);
            foreach (var q in _questionResults)
                experimentalResults.Add(q);

            int correctCount = 0;
            foreach (var q in _questionResults)
            {
                if (q.TryGetValue("correct", out object c) && c is bool b && b)
                    correctCount++;
            }

            var doc = new Dictionary<string, object>
            {
                { "blockId", blockId },
                { "experimentId", experimentId },
                { "groupId", groupId },
                { "studentId", studentId },
                { "vrId", vrId },
                { "progressId", progressId },
                { "experimentName", "Memory2" },
                { "moduleName", "Memory2" },
                { "completionStatus", "Completed" },
                { "duration", durationDisplay },
                { "durationSeconds", durationSeconds },
                { "startedAt", _startedAtUtc.ToString("o") },
                { "completedAt", completedAtUtc.ToString("o") },
                { "timestamp", completedAtUtc.ToString("o") },
                { "roomsCompleted", _roomResults.Count },
                { "questionsAnswered", _questionResults.Count },
                { "questionsCorrect", correctCount },
                { "configurations", configurations },
                { "experimentalResults", experimentalResults }
            };

            await db.Collection("experimentResults").AddAsync(doc);
            Debug.Log($"[ExperimentManager] Saved (duration={durationDisplay}, Rooms={_roomResults.Count}, Q={_questionResults.Count}, progressId={progressId})");

            ExperimentLogger.Log("experiment_completed",
                $"Memory2 experiment completed. Rooms={_roomResults.Count}, Questions={_questionResults.Count}, Correct={correctCount}",
                new Dictionary<string, object>
                {
                    { "roomsCompleted", _roomResults.Count },
                    { "questionsAnswered", _questionResults.Count },
                    { "questionsCorrect", correctCount },
                    { "durationSeconds", durationSeconds }
                });

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
            Debug.LogError($"[ExperimentManager] Failed to save results: {ex.Message}");
            ExperimentLogger.Log("error", $"Failed to save results: {ex.Message}");
        }
    }

    private static string GetId(ExperimentConfig config, string field, string fallback)
    {
        if (config == null) return fallback;

        switch (field)
        {
            case "studentId": return string.IsNullOrEmpty(config.studentId) ? fallback : config.studentId;
            case "groupId": return string.IsNullOrEmpty(config.groupId) ? fallback : config.groupId;
            case "blockId": return string.IsNullOrEmpty(config.blockId) ? fallback : config.blockId;
            case "experimentId": return string.IsNullOrEmpty(config.experimentId) ? fallback : config.experimentId;
            case "vrId": return string.IsNullOrEmpty(config.vrId) ? fallback : config.vrId;
            case "progressId": return string.IsNullOrEmpty(config.progressId) ? fallback : config.progressId;
            default: return fallback;
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

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (GUILayout.Button("DEBUG: Skip Room Timer"))
        {
            if (isTimerRunning)
                roomTimeRemaining = 0f;
        }

        if (GUILayout.Button("DEBUG: Skip to Questions / Lab"))
        {
            StopAllCoroutines();
            currentRoomIndex = rooms.Count;
            StartCoroutine(BeginTransitionToLab());
        }

        if (GUILayout.Button("DEBUG: Start Distractor"))
        {
            StopAllCoroutines();
            if (recallInstructionPanel != null) recallInstructionPanel.SetActive(false);
            StartCoroutine(RunDistractorThenQuestions());
        }

        if (GUILayout.Button("DEBUG: Start Experiment"))
            StartExperiment();
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