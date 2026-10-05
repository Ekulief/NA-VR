using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;

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

    void Start()
    {
        sessionId = Guid.NewGuid().ToString("N");
        HideAllUI();
        if (roomNameText != null)
            roomNameText.text = "Memory Lab – Dinner / Spatial Recognition";
    }

    public void StartExperiment()
    {
        if (objectTracker == null)
        {
            Debug.LogError("[MemoryExperimentManager] objectTracker is not assigned.");
            return;
        }

        objectTracker.CaptureOriginalLayout();
        GenerateTrials();

        if (trials.Count == 0)
        {
            Debug.LogError("[MemoryExperimentManager] No trials generated.");
            return;
        }

        currentTrial = 0;
        correctCount = 0;
        trialResults.Clear();
        EnterPhase(Phase.Study);
    }

    void Update()
    {
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
                break;

            case Phase.Distractor:
                if (distractorPanel != null) distractorPanel.SetActive(true);
                SetQuestionText("Count backwards from 100 by 3s.");
                ShowProgress(true);
                break;

            case Phase.Test:
                if (currentTrial < 0 || currentTrial >= trials.Count)
                {
                    EnterPhase(Phase.Complete);
                    return;
                }
                objectTracker.ApplyTrialChanges(trials[currentTrial]);
                ShowQuestion(trials[currentTrial]);
                break;

            case Phase.Complete:
                if (endPanel != null) endPanel.SetActive(true);
                SetQuestionText("Experiment complete. Data uploaded.");
                SaveFinalResults();
                break;
        }
    }

    void NextPhase()
    {
        switch (currentPhase)
        {
            case Phase.Study:
                EnterPhase(Phase.Distractor);
                break;
            case Phase.Distractor:
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
        if (!acceptingInput || currentPhase != Phase.Test) return;
        acceptingInput = false;

        bool correct = choiceIndex == t.correctIndex;
        float rt = Time.time - t.onsetTime;
        if (correct) correctCount++;

        trialResults.Add(new Dictionary<string, object>
        {
            { "trial", currentTrial },
            { "questionType", t.type.ToString() },
            { "correct", correct },
            { "reactionTimeMs", (int)(rt * 1000) },
            { "choice", choiceIndex },
            { "sessionId", sessionId }
        });

        // Hide buttons while showing feedback
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
        yield return new WaitForSeconds(delay);
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

            string studentId = "Anonymous";
            string groupId = "";

            var config = ExperimentConfigLoader.Current;
            if (config != null)
            {
                // Use whatever fields your ExperimentConfig actually has
                var type = config.GetType();
                var studentProp = type.GetField("studentId") ?? (object)type.GetProperty("studentId");
                var groupProp = type.GetField("groupId") ?? (object)type.GetProperty("groupId");
                // Simpler: if you have public strings, assign directly:
                // studentId = config.participantId;
            }

            var trialData = new Dictionary<string, object>
            {
                { "studentId", studentId },
                { "groupId", groupId },
                { "sessionId", sessionId },
                { "experimentName", "MemoryDinner" },
                { "totalTrials", trials.Count },
                { "correctCount", correctCount },
                { "accuracy", trials.Count > 0 ? (float)correctCount / trials.Count : 0f },
                { "trials", trialResults },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "sessionControl", "completed" }
            };

            await db.Collection("experimentResult").AddAsync(trialData);
            Debug.Log($"[MemoryExperimentManager] Results saved ({correctCount}/{trials.Count})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MemoryExperimentManager] Failed to save results: {ex.Message}");
        }
    }
}