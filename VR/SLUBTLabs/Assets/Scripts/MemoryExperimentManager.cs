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

    [Header("UI References (match Memory Scene 2)")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI questionText;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Button[] choiceButtons;          // A B C D
    public GameObject distractorPanel;
    public GameObject endPanel;

    [Header("Timing (Psychology Module 7 style)")]
    public float studyDuration = 90f;
    public float distractorDuration = 30f;
    public int numberOfTestTrials = 12;

    [Header("References")]
    public Transform spawnPoint;
    public MemoryObjectTracker objectTracker;

    Phase currentPhase = Phase.Idle;
    float phaseTimer;
    int currentTrial;
    List<MemoryTrial> trials = new();
    string sessionId;

    // Track results for final summary
    int correctCount = 0;
    List<Dictionary<string, object>> trialResults = new();

    void Start()
    {
        sessionId = Guid.NewGuid().ToString("N");
        HideAllUI();
        if (roomNameText) roomNameText.text = "Memory Lab – Spatial Recognition";
    }

    public void StartExperiment()
    {
        objectTracker.CaptureOriginalLayout();
        GenerateTrials();
        currentTrial = 0;
        correctCount = 0;
        trialResults.Clear();
        EnterPhase(Phase.Study);
    }

    void Update()
    {
        if (currentPhase == Phase.Study || currentPhase == Phase.Distractor)
        {
            phaseTimer -= Time.deltaTime;
            if (progressSlider)
            {
                progressSlider.value = 1f - (phaseTimer / GetPhaseDuration());
                if (progressText) progressText.text = $"{Mathf.CeilToInt(phaseTimer)}s";
            }
            if (phaseTimer <= 0) NextPhase();
        }
    }

    void EnterPhase(Phase p)
    {
        currentPhase = p;
        phaseTimer = GetPhaseDuration();
        HideAllUI();

        switch (p)
        {
            case Phase.Study:
                questionText.text = "Memorize the room. Walk around freely.";
                questionText.gameObject.SetActive(true);
                progressSlider.gameObject.SetActive(true);
                break;

            case Phase.Distractor:
                distractorPanel.SetActive(true);
                questionText.text = "Count backwards from 100 by 3s.";
                questionText.gameObject.SetActive(true);
                break;

            case Phase.Test:
                objectTracker.ApplyTrialChanges(trials[currentTrial]);
                ShowQuestion(trials[currentTrial]);
                break;

            case Phase.Complete:
                endPanel.SetActive(true);
                questionText.text = "Experiment complete. Data uploaded.";
                SaveFinalResults();
                break;
        }
    }

    void NextPhase()
    {
        switch (currentPhase)
        {
            case Phase.Study: EnterPhase(Phase.Distractor); break;
            case Phase.Distractor: EnterPhase(Phase.Test); break;
            case Phase.Test:
                currentTrial++;
                if (currentTrial >= trials.Count) EnterPhase(Phase.Complete);
                else EnterPhase(Phase.Test);
                break;
        }
    }

    void ShowQuestion(MemoryTrial t)
    {
        t.onsetTime = Time.time;
        questionText.gameObject.SetActive(true);
        questionText.text = t.question;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].gameObject.SetActive(true);
            choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = t.choices[i];
            int idx = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(idx, t));
        }

        progressSlider.gameObject.SetActive(true);
        progressSlider.value = (float)currentTrial / trials.Count;
        progressText.text = $"Trial {currentTrial + 1}/{trials.Count}";
    }

    void OnChoiceSelected(int choiceIndex, MemoryTrial t)
    {
        bool correct = choiceIndex == t.correctIndex;
        float rt = Time.time - t.onsetTime;

        if (correct) correctCount++;

        // Store for final summary
        trialResults.Add(new Dictionary<string, object>
        {
            { "trial", currentTrial },
            { "questionType", t.type.ToString() },
            { "correct", correct },
            { "reactionTimeMs", (int)(rt * 1000) },
            { "choice", choiceIndex }
        });

        questionText.text = correct ? "Correct" : "Incorrect";
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
        _ => 0
    };

    void HideAllUI()
    {
        if (questionText) questionText.gameObject.SetActive(false);
        if (progressSlider) progressSlider.gameObject.SetActive(false);
        if (distractorPanel) distractorPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);
        if (choiceButtons != null)
            foreach (var b in choiceButtons) b.gameObject.SetActive(false);
    }

    void GenerateTrials()
    {
        trials.Clear();
        for (int i = 0; i < numberOfTestTrials; i++)
        {
            trials.Add(objectTracker.CreateRandomTrial());
        }
    }

    private async void SaveFinalResults()
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            var config = ExperimentConfigLoader.Current;

            string studentId = "Anonymous";
            string groupId = "";

            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.studentId))
                    studentId = config.studentId;
                if (!string.IsNullOrEmpty(config.groupId))
                    groupId = config.groupId;
            }

            var trialData = new Dictionary<string, object>
            {
                { "studentId", studentId },
                { "groupId", groupId },
                { "experimentName", "Memory" },
                { "totalTrials", trials.Count },
                { "correctCount", correctCount },
                { "accuracy", trials.Count > 0 ? (float)correctCount / trials.Count : 0f },
                { "trials", trialResults },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "sessionControl", "completed" }
            };

            await db.Collection("experimentResult").AddAsync(trialData);
            Debug.Log($"[MemoryExperimentManager] Results saved (studentId={studentId}, correct={correctCount}/{trials.Count})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MemoryExperimentManager] Failed to save results: {ex.Message}");
        }
    }
}