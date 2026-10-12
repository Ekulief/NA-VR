using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;

/// <summary>
/// Memory Dinner flow aligned with ExperimentManager:
/// Study (diner, timed, TimerHUD events) → teleport to lab/question room →
/// optional distractor → change-detection test trials → save.
/// </summary>
public class MemoryExperimentManager : MonoBehaviour
{
    public static MemoryExperimentManager Instance { get; private set; }

    public enum Phase { Idle, Study, Transitioning, Recalling, Distractor, Test, Complete }

    // ── Events (same signatures as ExperimentManager so TimerHUD can hook them) ──
    public Action<string, float> OnRoomStarted;
    public Action<string> OnRoomEnded;
    public Action OnAllRoomsExplored;
    public Action OnExperimentFinished;
    public Action<float> OnTimerTick;
    public Action<float> OnDistractorStarted;
    public Action OnDistractorEnded;
    public Action OnRecallStarted;

    public Phase CurrentPhase => currentPhase;
    public int CurrentRoomIndex => 0;
    public int TotalRooms => 1;

    [Header("UI References – Start / Intro")]
    public Button startButton;
    public GameObject startPanel;

    [Header("UI References – Recall (before distractor)")]
    [Tooltip("Panel with instructions + Begin Questions button (shown after teleport to lab)")]
    public GameObject recallInstructionPanel;
    [Tooltip("StartQuestioningButton – player must click before distractor")]
    public Button beginQuestioningButton;
    public TextMeshProUGUI recallInstructionTextUI;

    [Header("UI References – Core")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI questionText;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Button[] choiceButtons;
    public GameObject distractorPanel;
    public GameObject endPanel;

    [Header("UI Text (optional – from Firebase)")]
    public TextMeshProUGUI briefingTextUI;
    public TextMeshProUGUI roomInstructionTextUI;
    public TextMeshProUGUI distractorInstructionTextUI;

    [Header("Timing (overridden by Firebase when available)")]
    public float studyDuration = 90f;
    public float distractorDuration = 30f;
    public float transitionFadeDuration = 0.5f;
    public int numberOfTestTrials = 12;
    public bool useDistractorTask = true;
    public bool randomizeQuestions = true;

    [Header("References – Teleport / Lab")]
    [Tooltip("Where the player starts the study phase (diner). Optional.")]
    public Transform studySpawnPoint;
    [Tooltip("Lab / question room entry – assign LabRoom_SpawnPoint from the scene")]
    public Transform labRoomSpawnPoint;
    public Transform playerTransform;
    public FadeController fadeController;
    public MemoryObjectTracker objectTracker;

    public string BriefingText { get; private set; }
    public string RoomInstructionText { get; private set; }
    public string DistractorInstructionText { get; private set; }
    public string RecallInstructionText { get; private set; }

    Phase currentPhase = Phase.Idle;
    float phaseTimer;
    int currentTrial;
    readonly List<MemoryTrial> trials = new();
    string sessionId;
    int correctCount;
    readonly List<Dictionary<string, object>> trialResults = new();
    bool acceptingInput;
    bool isTimerRunning;
    float _experimentStartRealtime;
    DateTime _startedAtUtc;
    bool _configApplied;
    string _studyRoomName = "Diner";

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;
    }

    void Start()
    {
        sessionId = Guid.NewGuid().ToString("N");
        HideAllUI();
        ShowStartUI(true);

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player")
                ?? GameObject.Find("XR Origin")
                ?? GameObject.Find("XR Origin (XR Rig)");
            if (player != null)
                playerTransform = player.transform;
        }

        if (fadeController == null)
            fadeController = FindFirstObjectByType<FadeController>();

        if (roomNameText != null)
            roomNameText.text = "Memory Lab – Dinner / Spatial Recognition";

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartExperiment);
        }
        else
        {
            Debug.LogWarning("[MemoryExperimentManager] startButton is not assigned.");
        }

        if (beginQuestioningButton != null)
        {
            beginQuestioningButton.onClick.RemoveAllListeners();
            beginQuestioningButton.onClick.AddListener(BeginQuestioning);
        }
        else
        {
            Debug.LogWarning("[MemoryExperimentManager] beginQuestioningButton is not assigned. " +
                             "Drag StartQuestioningButton onto Begin Questioning Button.");
        }

        // Hide recall panel until lab
        if (recallInstructionPanel != null)
            recallInstructionPanel.SetActive(false);

        StartCoroutine(WaitForConfigAndApply());
    }

    void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartExperiment);
        if (beginQuestioningButton != null)
            beginQuestioningButton.onClick.RemoveListener(BeginQuestioning);
        if (Instance == this)
            Instance = null;
    }

    IEnumerator WaitForConfigAndApply()
    {
        float timeout = 15f;
        float elapsed = 0f;
        while ((ExperimentConfigLoader.Current == null || !ExperimentConfigLoader.IsReady) && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        ApplyConfig();
    }

    void ApplyConfig()
    {
        if (ExperimentConfigLoader.Current == null)
        {
            Debug.Log("[MemoryExperimentManager] No config — using Inspector values.");
            ApplyFallbackTexts();
            return;
        }

        var c = ExperimentConfigLoader.Current;

        studyDuration = c.memory_TimePerRoomSeconds > 0f ? c.memory_TimePerRoomSeconds : studyDuration;
        distractorDuration = c.memory_DistractorTaskDuration > 0f ? c.memory_DistractorTaskDuration : distractorDuration;
        if (c.memory_TransitionFadeDuration > 0f)
            transitionFadeDuration = c.memory_TransitionFadeDuration;
        useDistractorTask = c.memory_UseDistractorTask;
        randomizeQuestions = c.memory_RandomizeQuestions;

        BriefingText = c.memory_BriefingText;
        RoomInstructionText = c.memory_RoomInstructionText;
        DistractorInstructionText = c.memory_DistractorInstructionText;
        RecallInstructionText = c.memory_RecallInstructionText;

        if (briefingTextUI != null && !string.IsNullOrEmpty(BriefingText))
            briefingTextUI.text = BriefingText;
        if (roomInstructionTextUI != null && !string.IsNullOrEmpty(RoomInstructionText))
            roomInstructionTextUI.text = RoomInstructionText;
        if (distractorInstructionTextUI != null && !string.IsNullOrEmpty(DistractorInstructionText))
            distractorInstructionTextUI.text = DistractorInstructionText;
        if (recallInstructionTextUI != null && !string.IsNullOrEmpty(RecallInstructionText))
            recallInstructionTextUI.text = RecallInstructionText;

        _configApplied = true;
        Debug.Log($"[MemoryExperimentManager] Applied config. study={studyDuration}s distractor={distractorDuration}s labSpawn={(labRoomSpawnPoint != null)}");
    }

    void ApplyFallbackTexts()
    {
        if (string.IsNullOrEmpty(BriefingText))
            BriefingText = "You will explore the room. Pay close attention to objects and details.";
        if (string.IsNullOrEmpty(RoomInstructionText))
            RoomInstructionText = "Memorize the room. Walk around freely.";
        if (string.IsNullOrEmpty(DistractorInstructionText))
            DistractorInstructionText = "Count backwards from 100 by 3s.";
        if (string.IsNullOrEmpty(RecallInstructionText))
            RecallInstructionText = "Answer the questions about what you saw.";
    }

    bool IsSessionPaused()
    {
        return SessionController.Instance != null && SessionController.Instance.IsPaused;
    }

    IEnumerator WaitWhilePaused()
    {
        while (IsSessionPaused())
            yield return null;
    }

    public void StartExperiment()
    {
        if (IsSessionPaused()) return;

        if (currentPhase != Phase.Idle)
        {
            Debug.LogWarning("[MemoryExperimentManager] Experiment already running.");
            return;
        }

        if (objectTracker == null)
        {
            Debug.LogError("[MemoryExperimentManager] objectTracker is not assigned.");
            ExperimentLogger.Log("error", "objectTracker is not assigned");
            return;
        }

        if (!_configApplied)
            ApplyConfig();

        ShowStartUI(false);

        objectTracker.CaptureOriginalLayout();
        GenerateTrials();

        if (trials.Count == 0)
        {
            Debug.LogError("[MemoryExperimentManager] No trials generated.");
            ExperimentLogger.Log("error", "No trials generated");
            return;
        }

        currentTrial = 0;
        correctCount = 0;
        trialResults.Clear();
        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;

        ExperimentLogger.Log("experiment_started",
            $"Memory experiment started with {trials.Count} trials",
            new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "numberOfTrials", trials.Count },
                { "studyDuration", studyDuration },
                { "distractorDuration", distractorDuration }
            });

        StartCoroutine(BeginStudyPhase());
    }

    IEnumerator BeginStudyPhase()
    {
        currentPhase = Phase.Study;

        // Optional: place player at study spawn
        if (studySpawnPoint != null)
        {
            if (fadeController != null)
                yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));
            TeleportPlayer(studySpawnPoint);
            yield return new WaitForSeconds(0.3f);
            if (fadeController != null)
                yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));
        }

        phaseTimer = studyDuration;
        isTimerRunning = true;

        SetQuestionText(
            !string.IsNullOrEmpty(RoomInstructionText)
                ? RoomInstructionText
                : "Memorize the room. Walk around freely.");
        ShowProgress(true);

        // Fire events so TimerHUD (and anything else) can show the study timer
        OnRoomStarted?.Invoke(_studyRoomName, studyDuration);

        Debug.Log($"[MemoryExperimentManager] Study phase started ({studyDuration}s).");
    }

    void Update()
    {
        if (IsSessionPaused()) return;

        if (!isTimerRunning || currentPhase != Phase.Study)
            return;

        phaseTimer -= Time.deltaTime;
        OnTimerTick?.Invoke(phaseTimer);

        if (progressSlider != null)
        {
            float dur = studyDuration;
            progressSlider.value = dur > 0 ? 1f - (phaseTimer / dur) : 1f;
        }
        if (progressText != null)
            progressText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, phaseTimer))}s";

        if (phaseTimer <= 0f)
        {
            phaseTimer = 0f;
            isTimerRunning = false;
            OnRoomTimeUp();
        }
    }

    void OnRoomTimeUp()
    {
        OnRoomEnded?.Invoke(_studyRoomName);
        OnAllRoomsExplored?.Invoke();
        Debug.Log("[MemoryExperimentManager] Study timer finished → teleport to lab / question room.");
        StartCoroutine(TransitionToLabAndContinue());
    }

    IEnumerator TransitionToLabAndContinue()
    {
        currentPhase = Phase.Transitioning;
        HideAllUI();

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeOut(transitionFadeDuration));

        if (labRoomSpawnPoint != null)
        {
            TeleportPlayer(labRoomSpawnPoint);
            Debug.Log("[MemoryExperimentManager] Teleported to LabRoom_SpawnPoint.");
        }
        else
        {
            Debug.LogWarning("[MemoryExperimentManager] labRoomSpawnPoint is NOT assigned. " +
                             "Drag LabRoom_SpawnPoint onto the Lab Room Spawn Point field.");
        }

        yield return new WaitForSeconds(0.5f);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeIn(transitionFadeDuration));

        // Wait for player to press "Begin Questions" before distractor / questions
        StartRecallPhase();
    }

    /// <summary>
    /// After teleport to lab: show recall instruction panel.
    /// Player must click Begin Questions (StartQuestioningButton) to continue.
    /// </summary>
    void StartRecallPhase()
    {
        currentPhase = Phase.Recalling;
        HideAllUI();
        ApplyConfig();

        if (recallInstructionPanel != null)
        {
            recallInstructionPanel.SetActive(true);
            if (recallInstructionTextUI != null && !string.IsNullOrEmpty(RecallInstructionText))
                recallInstructionTextUI.text = RecallInstructionText;
            Debug.Log("[MemoryExperimentManager] Recall panel shown — waiting for Begin Questions.");
        }
        else
        {
            // No panel assigned: skip straight to distractor / questions
            Debug.LogWarning("[MemoryExperimentManager] recallInstructionPanel not assigned — auto-continuing.");
            BeginQuestioning();
        }
    }

    /// <summary>
    /// Wired to StartQuestioningButton. Hides recall panel, then distractor → questions.
    /// </summary>
    public void BeginQuestioning()
    {
        // Do not hard-block on pause — log and continue so the flow cannot soft-lock
        if (IsSessionPaused())
            Debug.LogWarning("[MemoryExperimentManager] BeginQuestioning while session paused — continuing anyway.");

        if (currentPhase != Phase.Recalling && currentPhase != Phase.Transitioning)
        {
            Debug.LogWarning($"[MemoryExperimentManager] BeginQuestioning from phase={currentPhase} (expected Recalling).");
        }

        if (recallInstructionPanel != null)
            recallInstructionPanel.SetActive(false);

        if (useDistractorTask)
            StartCoroutine(RunDistractorThenQuestions());
        else
            StartTestPhase();
    }

    IEnumerator RunDistractorThenQuestions()
    {
        currentPhase = Phase.Distractor;
        if (distractorPanel != null) distractorPanel.SetActive(true);

        SetQuestionText(
            !string.IsNullOrEmpty(DistractorInstructionText)
                ? DistractorInstructionText
                : "Count backwards from 100 by 3s.");
        ShowProgress(true);

        float duration = distractorDuration > 0f ? distractorDuration : 30f;
        OnDistractorStarted?.Invoke(duration);
        ExperimentLogger.Log("distractor_started", $"Distractor ({duration}s)");
        Debug.Log($"[MemoryExperimentManager] Distractor started for {duration}s.");

        // Use realtime so pause / timescale cannot freeze the distractor forever
        // while DinerDistractorTaskUI still counts to 0 with Time.deltaTime.
        float endAt = Time.realtimeSinceStartup + duration;
        while (Time.realtimeSinceStartup < endAt)
        {
            float remaining = endAt - Time.realtimeSinceStartup;
            if (progressSlider != null)
                progressSlider.value = duration > 0 ? 1f - (remaining / duration) : 1f;
            if (progressText != null)
                progressText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, remaining))}s";
            yield return null;
        }

        if (distractorPanel != null) distractorPanel.SetActive(false);
        ShowProgress(false);

        Debug.Log("[MemoryExperimentManager] Distractor finished → starting questions.");
        OnDistractorEnded?.Invoke();
        StartTestPhase();
    }

    void StartTestPhase()
    {
        currentPhase = Phase.Test;
        currentTrial = 0;

        Debug.Log("[MemoryExperimentManager] StartTestPhase — invoking OnRecallStarted.");
        OnRecallStarted?.Invoke();

        // If DinerQuestionManager is in the scene, it owns the question flow
        // (Firestore / ScriptableObject targets). Skip MemoryObjectTracker trials.
        if (DinerQuestionManager.Instance != null)
        {
            ExperimentLogger.Log("questions_started",
                "Recall started — DinerQuestionManager will drive questions");
            Debug.Log("[MemoryExperimentManager] Handing questions to DinerQuestionManager. " +
                      "If no panel appears, check question list + DinerQuestionPanelUI.");
            return;
        }

        Debug.LogWarning("[MemoryExperimentManager] No DinerQuestionManager — using MemoryObjectTracker trials.");
        ExperimentLogger.Log("questions_started", "Test trials started (MemoryObjectTracker)");
        ShowNextTrial();
    }

    /// <summary>
    /// Called by DinerQuestionManager when all recall questions are done.
    /// </summary>
    public void OnExperimentComplete()
    {
        if (currentPhase == Phase.Complete) return;
        EnterComplete();
    }

    void ShowNextTrial()
    {
        if (currentTrial < 0 || currentTrial >= trials.Count)
        {
            EnterComplete();
            return;
        }

        HideAllUI();
        objectTracker.ApplyTrialChanges(trials[currentTrial]);
        ShowQuestion(trials[currentTrial]);
    }

    void ShowQuestion(MemoryTrial t)
    {
        t.onsetTime = Time.time;
        acceptingInput = true;
        SetQuestionText(t.question);
        ShowProgress(true);

        if (progressSlider != null)
            progressSlider.value = trials.Count > 0 ? (float)currentTrial / trials.Count : 0f;
        if (progressText != null)
            progressText.text = $"Trial {currentTrial + 1}/{trials.Count}";

        if (choiceButtons == null) return;

        string[] choices = t.choices ?? Array.Empty<string>();
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button btn = choiceButtons[i];
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();

            if (i < choices.Length)
            {
                btn.gameObject.SetActive(true);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = choices[i];
                int idx = i;
                btn.onClick.AddListener(() => OnChoiceSelected(idx, t));
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    void OnChoiceSelected(int choiceIndex, MemoryTrial t)
    {
        if (IsSessionPaused()) return;
        if (!acceptingInput || currentPhase != Phase.Test) return;

        acceptingInput = false;
        bool correct = choiceIndex == t.correctIndex;
        float rt = Time.time - t.onsetTime;
        if (correct) correctCount++;

        trialResults.Add(new Dictionary<string, object>
        {
            { "trialIndex", currentTrial },
            { "questionType", t.type.ToString() },
            { "question", t.question ?? "" },
            { "correct", correct },
            { "reactionTimeMs", (int)(rt * 1000) },
            { "choice", choiceIndex },
            { "correctIndex", t.correctIndex },
            { "sessionId", sessionId }
        });

        ExperimentLogger.Log("trial_answered",
            $"Trial {currentTrial + 1}: {(correct ? "Correct" : "Incorrect")} (RT: {rt:F2}s)",
            new Dictionary<string, object>
            {
                { "trialIndex", currentTrial },
                { "correct", correct },
                { "reactionTimeMs", (int)(rt * 1000) }
            });

        if (choiceButtons != null)
        {
            foreach (var b in choiceButtons)
                if (b != null) b.gameObject.SetActive(false);
        }

        SetQuestionText(correct ? "Correct" : "Incorrect");
        StartCoroutine(DelayThenNextTrial(1.2f));
    }

    IEnumerator DelayThenNextTrial(float delay)
    {
        float waited = 0f;
        while (waited < delay)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }
        currentTrial++;
        ShowNextTrial();
    }

    void EnterComplete()
    {
        currentPhase = Phase.Complete;
        HideAllUI();
        if (endPanel != null) endPanel.SetActive(true);
        SetQuestionText("Experiment complete. Data uploaded.");
        OnExperimentFinished?.Invoke();
        SaveFinalResults();
    }

    // ── Teleport (same approach as ExperimentManager) ──────────────────────

    void TeleportPlayer(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogError("[MemoryExperimentManager] Teleport failed: destination is not assigned.");
            return;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player")
                ?? GameObject.Find("VR Player")
                ?? GameObject.Find("XR Origin")
                ?? GameObject.Find("XR Origin (XR Rig)");

            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[MemoryExperimentManager] Teleport failed: player rig not found.");
            return;
        }

        CharacterController cc =
            playerTransform.GetComponentInChildren<CharacterController>();

        Rigidbody rb =
            playerTransform.GetComponentInChildren<Rigidbody>();

        bool controllerWasEnabled = cc != null && cc.enabled;
        bool wasKinematic = rb != null && rb.isKinematic;
        bool usedGravity = rb != null && rb.useGravity;

        // Temporarily stop physics from interfering with the teleport.
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

        // Align the player's horizontal facing direction with the spawn marker.
        Vector3 targetForward =
            Vector3.ProjectOnPlane(destination.forward, Vector3.up).normalized;

        if (targetForward.sqrMagnitude > 0.001f)
        {
            if (vrCamera != null)
            {
                float cameraYaw = vrCamera.transform.eulerAngles.y;
                float targetYaw = Quaternion.LookRotation(targetForward).eulerAngles.y;

                playerTransform.Rotate(
                    0f, targetYaw - cameraYaw, 0f, Space.World);
            }
            else
            {
                playerTransform.rotation =
                    Quaternion.LookRotation(targetForward, Vector3.up);
            }
        }

        // Put the XR Origin directly at the spawn marker's floor position.
        // Do not add a hard-coded 1.6 m height.
        if (vrCamera != null)
        {
            Vector3 cameraOffset =
                vrCamera.transform.position - playerTransform.position;

            cameraOffset.y = 0f;

            playerTransform.position = new Vector3(
                destination.position.x - cameraOffset.x,
                destination.position.y,
                destination.position.z - cameraOffset.z
            );
        }
        else
        {
            playerTransform.position = destination.position;
        }

        Debug.Log(
            $"[MemoryExperimentManager] Teleported to {destination.name}. " +
            $"Spawn={destination.position}, Origin={playerTransform.position}, " +
            $"Camera={(vrCamera != null ? vrCamera.transform.position.ToString() : "not found")}"
        );

        StartCoroutine(RestorePlayerAfterTeleport(
            cc, controllerWasEnabled, rb, wasKinematic, usedGravity));
    }

    IEnumerator RestorePlayerAfterTeleport(
        CharacterController cc,
        bool controllerWasEnabled,
        Rigidbody rb,
        bool wasKinematic,
        bool usedGravity)
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

    IEnumerator ReenableLocomotion(CharacterController cc, Rigidbody rb)
    {
        // Wait a couple of physics frames so the pose settles before gravity returns
        yield return null;
        yield return new WaitForFixedUpdate();

        if (cc != null) cc.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;
            // Keep gravity OFF for XR locomotion providers that drive motion themselves.
            // If you use a non-XR CharacterController walker, set this true.
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ── UI helpers ─────────────────────────────────────────────────────────

    void SetQuestionText(string msg)
    {
        if (questionText == null) return;
        questionText.gameObject.SetActive(true);
        questionText.text = msg;
    }

    void ShowProgress(bool on)
    {
        if (progressSlider != null) progressSlider.gameObject.SetActive(on);
        if (progressText != null) progressText.gameObject.SetActive(on);
    }

    void ShowStartUI(bool on)
    {
        if (startPanel != null)
            startPanel.SetActive(on);
        else if (startButton != null)
            startButton.gameObject.SetActive(on);
    }

    void HideAllUI()
    {
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (distractorPanel != null) distractorPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (recallInstructionPanel != null) recallInstructionPanel.SetActive(false);
        if (choiceButtons != null)
        {
            foreach (var b in choiceButtons)
                if (b != null) b.gameObject.SetActive(false);
        }
    }

    void GenerateTrials()
    {
        trials.Clear();
        for (int i = 0; i < numberOfTestTrials; i++)
        {
            MemoryTrial trial = objectTracker.CreateRandomTrial();
            if (trial != null)
                trials.Add(trial);
        }

        if (randomizeQuestions && trials.Count > 1)
        {
            for (int i = trials.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (trials[i], trials[j]) = (trials[j], trials[i]);
            }
        }

        Debug.Log($"[MemoryExperimentManager] Generated {trials.Count} trials.");
    }

    // ── Firebase save ──────────────────────────────────────────────────────

    async void SaveFinalResults()
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
                    { "studyDuration", studyDuration },
                    { "distractorDuration", distractorDuration },
                    { "useDistractorTask", useDistractorTask },
                    { "randomizeQuestions", randomizeQuestions },
                    { "numberOfTestTrials", numberOfTestTrials },
                    { "sessionId", sessionId }
                }
            };

            var experimentalResults = new List<object>();
            foreach (var t in trialResults)
                experimentalResults.Add(t);

            var doc = new Dictionary<string, object>
            {
                { "blockId", blockId },
                { "experimentId", experimentId },
                { "groupId", groupId },
                { "studentId", studentId },
                { "vrId", vrId },
                { "progressId", progressId },
                { "sessionId", sessionId },
                { "experimentName", "Memory" },
                { "moduleName", "Memory" },
                { "completionStatus", "Completed" },
                { "duration", durationDisplay },
                { "durationSeconds", durationSeconds },
                { "startedAt", _startedAtUtc.ToString("o") },
                { "completedAt", completedAtUtc.ToString("o") },
                { "timestamp", completedAtUtc.ToString("o") },
                { "totalTrials", trials.Count },
                { "correctCount", correctCount },
                { "questionsAnswered", trialResults.Count },
                { "questionsCorrect", correctCount },
                { "accuracy", trials.Count > 0 ? (float)correctCount / trials.Count : 0f },
                { "configurations", configurations },
                { "experimentalResults", experimentalResults }
            };

            await db.Collection("experimentResults").AddAsync(doc);
            Debug.Log($"[MemoryExperimentManager] Saved ({correctCount}/{trials.Count}, duration={durationDisplay})");

            ExperimentLogger.Log("experiment_completed",
                $"Memory completed. Score: {correctCount}/{trials.Count}",
                new Dictionary<string, object>
                {
                    { "totalTrials", trials.Count },
                    { "correctCount", correctCount },
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
            Debug.LogError($"[MemoryExperimentManager] Failed to save: {ex.Message}");
            ExperimentLogger.Log("error", $"Failed to save results: {ex.Message}");
        }
    }

    static string GetId(ExperimentConfig config, string field, string fallback)
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

    static string FormatDuration(float totalSeconds)
    {
        if (totalSeconds < 0f) totalSeconds = 0f;
        int t = Mathf.FloorToInt(totalSeconds);
        return $"{t / 60}:{t % 60:D2}";
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 260, 220));
        if (GUILayout.Button("DEBUG: Start Experiment"))
            StartExperiment();
        if (GUILayout.Button("DEBUG: Skip Study Timer (→ Lab)"))
        {
            if (currentPhase == Phase.Study && isTimerRunning)
            {
                phaseTimer = 0f;
                isTimerRunning = false;
                OnRoomTimeUp();
            }
        }
        if (GUILayout.Button("DEBUG: Next Trial"))
        {
            if (currentPhase == Phase.Test)
            {
                currentTrial++;
                ShowNextTrial();
            }
        }
        if (GUILayout.Button("DEBUG: Jump to Complete"))
            EnterComplete();
        GUILayout.Label($"Phase: {currentPhase}");
        GUILayout.Label($"Trial: {currentTrial + 1}/{trials.Count}");
        GUILayout.Label($"LabSpawn: {(labRoomSpawnPoint != null ? "OK" : "MISSING")}");
        GUILayout.Label($"StartBtn: {(startButton != null ? "OK" : "MISSING")}");
        GUILayout.Label($"Tracker: {(objectTracker != null ? "OK" : "MISSING")}");
        GUILayout.EndArea();
#endif
    }
}