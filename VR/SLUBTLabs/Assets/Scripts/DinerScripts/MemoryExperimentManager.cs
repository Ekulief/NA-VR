using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;

/// <summary>
/// Spatial memory / change-detection trial (study → distractor → test).
/// </summary>
public class MemoryExperimentManager : MonoBehaviour
{
    public enum Phase { Idle, Study, Distractor, Test, Complete }

    [Header("UI References")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI questionText;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Button[] choiceButtons;
    public GameObject distractorPanel;
    public GameObject endPanel;

    [Header("Timing")]
    public float studyDuration = 90f;
    public float distractorDuration = 30f;
    public int numberOfTestTrials = 12;

    [Header("References")]
    public Transform spawnPoint;
    public MemoryObjectTracker objectTracker;

    Phase currentPhase = Phase.Idle;
    float phaseTimer;
    int currentTrial;
    readonly List<MemoryTrial> trials = new();
    string sessionId;
    int correctCount;
    readonly List<Dictionary<string, object>> trialResults = new();
    bool acceptingInput;
    float _experimentStartRealtime;
    DateTime _startedAtUtc;

    void Start()
    {
        sessionId = Guid.NewGuid().ToString("N");
        HideAllUI();

        if (roomNameText != null)
            roomNameText.text = "Memory Lab – Dinner / Spatial Recognition";
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

    public void StartExperiment()
    {
        if (IsSessionPaused()) return;

        if (objectTracker == null)
        {
            Debug.LogError("[MemoryExperimentManager] objectTracker is not assigned.");
            ExperimentLogger.Log("error", "objectTracker is not assigned");
            return;
        }

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
            $"Memory Dinner experiment started with {trials.Count} trials",
            new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "numberOfTrials", trials.Count },
                { "studyDuration", studyDuration },
                { "distractorDuration", distractorDuration }
            });

        EnterPhase(Phase.Study);
    }

    void Update()
    {
        if (IsSessionPaused()) return;

        if (currentPhase != Phase.Study && currentPhase != Phase.Distractor)
            return;

        phaseTimer -= Time.deltaTime;

        if (progressSlider != null)
        {
            float dur = GetPhaseDuration();
            progressSlider.value = dur > 0 ? 1f - (phaseTimer / dur) : 1f;
        }

        if (progressText != null)
            progressText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, phaseTimer))}s";

        if (phaseTimer <= 0f)
            NextPhase();
    }

    void EnterPhase(Phase p)
    {
        currentPhase = p;
        phaseTimer = GetPhaseDuration();
        acceptingInput = false;
        HideAllUI();

        switch (p)
        {
            case Phase.Study:
                SetQuestionText("Memorize the room. Walk around freely.");
                ShowProgress(true);
               // ExperimentLogger.Log("study_phase_started",
             //       $"Study phase started ({studyDuration}s)");
                break;

            case Phase.Distractor:
                if (distractorPanel != null) distractorPanel.SetActive(true);
                SetQuestionText("Count backwards from 100 by 3s.");
                ShowProgress(true);
                ExperimentLogger.Log("distractor_started",
                    $"Distractor phase started ({distractorDuration}s)");
                break;

            case Phase.Test:
                if (currentTrial < 0 || currentTrial >= trials.Count)
                {
                    EnterPhase(Phase.Complete);
                    return;
                }
                objectTracker.ApplyTrialChanges(trials[currentTrial]);
                ShowQuestion(trials[currentTrial]);
                /*ExperimentLogger.Log("trial_started",
                    $"Test trial {currentTrial + 1}/{trials.Count}: {trials[currentTrial].type}",
                    new Dictionary<string, object>
                    {
                        { "trialIndex", currentTrial },
                        { "questionType", trials[currentTrial].type.ToString() },
                        { "question", trials[currentTrial].question ?? "" }
                    }); */
                break;

            case Phase.Complete:
                if (endPanel != null) endPanel.SetActive(true);
                SetQuestionText("Experiment complete. Data uploaded.");
              /*  ExperimentLogger.Log("experiment_finished",
                    $"All trials complete. Correct: {correctCount}/{trials.Count}"); */
                SaveFinalResults();
                break;
        }
    }

    void NextPhase()
    {
        switch (currentPhase)
        {
            case Phase.Study:
               // ExperimentLogger.Log("study_phase_ended", "Study phase finished");
                EnterPhase(Phase.Distractor);
                break;

            case Phase.Distractor:
              //  ExperimentLogger.Log("distractor_ended", "Distractor phase finished");
                EnterPhase(Phase.Test);
                break;

            case Phase.Test:
                currentTrial++;
                if (currentTrial >= trials.Count)
                    EnterPhase(Phase.Complete);
                else
                    EnterPhase(Phase.Test);
                break;
        }
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
                { "questionType", t.type.ToString() },
                { "correct", correct },
                { "reactionTimeMs", (int)(rt * 1000) },
                { "choice", choiceIndex },
                { "correctIndex", t.correctIndex }
            });

        if (choiceButtons != null)
        {
            foreach (var b in choiceButtons)
                if (b != null) b.gameObject.SetActive(false);
        }

        SetQuestionText(correct ? "Correct" : "Incorrect");
        StartCoroutine(DelayThenNext(1.2f));
    }

    IEnumerator DelayThenNext(float delay)
    {
        float waited = 0f;
        while (waited < delay)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }
        NextPhase();
    }

    float GetPhaseDuration() => currentPhase switch
    {
        Phase.Study => studyDuration,
        Phase.Distractor => distractorDuration,
        _ => 0f
    };

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

    void HideAllUI()
    {
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (distractorPanel != null) distractorPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);

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
        Debug.Log($"[MemoryExperimentManager] Generated {trials.Count} trials.");
    }

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
                { "experimentName", "MemoryDinner" },
                { "moduleName", "MemoryDinner" },
                { "completionStatus", "Completed" },
                { "duration", durationDisplay },
                { "durationSeconds", durationSeconds },
                { "startedAt", _startedAtUtc.ToString("o") },
                { "completedAt", completedAtUtc.ToString("o") },
                { "timestamp", completedAtUtc.ToString("o") },
                { "totalTrials", trials.Count },
                { "correctCount", correctCount },
                { "accuracy", trials.Count > 0 ? (float)correctCount / trials.Count : 0f },
                { "configurations", configurations },
                { "experimentalResults", experimentalResults }
            };

            await db.Collection("experimentResults").AddAsync(doc);
            Debug.Log($"[MemoryExperimentManager] Saved ({correctCount}/{trials.Count}, duration={durationDisplay})");

            ExperimentLogger.Log("experiment_completed",
                $"Memory Dinner completed. Score: {correctCount}/{trials.Count}",
                new Dictionary<string, object>
                {
                    { "totalTrials", trials.Count },
                    { "correctCount", correctCount },
                    { "accuracy", trials.Count > 0 ? (float)correctCount / trials.Count : 0f },
                    { "durationSeconds", durationSeconds },
                    { "sessionId", sessionId }
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
            Debug.LogError($"[MemoryExperimentManager] Failed to save results: {ex.Message}");
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
        int m = t / 60;
        int s = t % 60;
        return $"{m}:{s:D2}";
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 220, 160));
        if (GUILayout.Button("DEBUG: Start Experiment"))
            StartExperiment();
        if (GUILayout.Button("DEBUG: Skip Phase Timer"))
        {
            if (currentPhase == Phase.Study || currentPhase == Phase.Distractor)
                phaseTimer = 0f;
        }
        if (GUILayout.Button("DEBUG: Next Trial / Phase"))
            NextPhase();
        if (GUILayout.Button("DEBUG: Jump to Complete"))
            EnterPhase(Phase.Complete);
        GUILayout.Label($"Phase: {currentPhase}");
        GUILayout.Label($"Trial: {currentTrial + 1}/{trials.Count}");
        GUILayout.Label($"Paused: {IsSessionPaused()}");
        GUILayout.EndArea();
#endif
    }
}