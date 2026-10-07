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

    // Optional: QuestionManager can push answers here
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

        // Collect answers if QuestionManager fires them
        StartCoroutine(SubscribeQuestionAnswers());

        ChangeState(ExperimentState.Idle);
    }

    private IEnumerator SubscribeQuestionAnswers()
    {
        while (QuestionManager.Instance == null)
            yield return new WaitForSeconds(0.1f);

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
        timePerRoom = c.memory_TimePerRoomSeconds;
        transitionFadeDuration = c.memory_TransitionFadeDuration;
        distractorTaskDuration = c.memory_DistractorTaskDuration;
        useDistractorTask = c.memory_UseDistractorTask;
        randomizeQuestions = c.memory_RandomizeQuestions;

        BriefingText = c.memory_BriefingText;
        RoomInstructionText = c.memory_RoomInstructionText;
        DistractorInstructionText = c.memory_DistractorInstructionText;
        RecallInstructionText = c.memory_RecallInstructionText;

        if (briefingTextUI != null) briefingTextUI.text = BriefingText;
        if (roomInstructionTextUI != null) roomInstructionTextUI.text = RoomInstructionText;
        if (distractorInstructionTextUI != null) distractorInstructionTextUI.text = DistractorInstructionText;
        if (recallInstructionTextUI != null) recallInstructionTextUI.text = RecallInstructionText;

        Debug.Log("[ExperimentManager] Applied Memory config.");
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
        StartCoroutine(GoToNextRoom());
    }

    private IEnumerator WaitForConfigAndApplyBriefing()
    {
        while (ExperimentConfigLoader.Current == null || !ExperimentConfigLoader.IsReady)
            yield return null;

        ApplyConfig();
    }

    public void OnPlayerEnterRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
        if (roomName != currentRoomName) return;
    }

    public void OnPlayerExitRoom(string roomName)
    {
        if (CurrentState != ExperimentState.Exploring) return;
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

        if (recallInstructionPanel != null)
        {
            recallInstructionPanel.SetActive(true);
            if (recallInstructionTextUI != null)
                recallInstructionTextUI.text = RecallInstructionText;
        }
        else
        {
            BeginQuestioning();
        }
    }

    public void BeginQuestioning()
    {
        if (IsSessionPaused()) return;

        if (recallInstructionPanel != null)
            recallInstructionPanel.SetActive(false);

        if (useDistractorTask)
            StartCoroutine(RunDistractorThenQuestions());
        else
            OnRecallStarted?.Invoke();
    }

    private IEnumerator RunDistractorThenQuestions()
    {
        ChangeState(ExperimentState.DistractorTask);
        OnDistractorStarted?.Invoke(distractorTaskDuration);

        float waited = 0f;
        while (waited < distractorTaskDuration)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }

        OnDistractorEnded?.Invoke();
        OnRecallStarted?.Invoke();
    }

    public void OnExperimentComplete()
    {
        ChangeState(ExperimentState.Finished);
        OnExperimentFinished?.Invoke();
        Debug.Log("[ExperimentManager] Experiment complete.");
        SaveResultsToFirestore();
    }

    private void TeleportPlayer(Transform destination)
    {
        if (destination == null) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("XR Origin");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[ExperimentManager] No player transform.");
            return;
        }

        CharacterController cc = playerTransform.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = playerTransform.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
            mainCam = playerTransform.GetComponentInChildren<Camera>();

        if (mainCam != null)
        {
            float currentCamY = mainCam.transform.eulerAngles.y;
            float targetY = destination.eulerAngles.y;
            playerTransform.Rotate(0f, targetY - currentCamY, 0f, Space.World);

            Vector3 cameraOffset = mainCam.transform.position - playerTransform.position;
            cameraOffset.y = 0f;
            Vector3 finalPosition = destination.position - cameraOffset;

            Transform cameraOffsetTf = playerTransform.Find("Camera Offset");
            float localCamY = cameraOffsetTf != null
                ? cameraOffsetTf.localPosition.y
                : mainCam.transform.localPosition.y;

            finalPosition.y = destination.position.y + 1.6f - localCamY;
            playerTransform.position = finalPosition;
        }
        else
        {
            playerTransform.position = destination.position;
            playerTransform.rotation = destination.rotation;
        }

        StartCoroutine(ReenableLocomotion(cc, rb));
    }

    private IEnumerator ReenableLocomotion(CharacterController cc, Rigidbody rb)
    {
        yield return null;
        if (cc != null) cc.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
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

            // experimentalResults = rooms explored + question answers
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
                { "experimentName", "Memory" },
                { "moduleName", "Memory" },
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
            Debug.Log($"[ExperimentManager] Saved (duration={durationDisplay}, Q={_questionResults.Count}, progressId={progressId})");

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