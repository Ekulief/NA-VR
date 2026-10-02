using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

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
    public float studyDuration = 90f;        // free exploration
    public float distractorDuration = 30f;
    public int numberOfTestTrials = 12;

    [Header("References")]
    public Transform spawnPoint;
    public MemoryObjectTracker objectTracker;
    public FirebaseLogger firebaseLogger;   // shared service

    Phase currentPhase = Phase.Idle;
    float phaseTimer;
    int currentTrial;
    List<MemoryTrial> trials = new();
    string sessionId;

    void Start()
    {
        sessionId = Guid.NewGuid().ToString("N");
        HideAllUI();
        if (roomNameText) roomNameText.text = "Memory Lab – Spatial Recognition";
    }

    public void StartExperiment()
    {
        // Called from the Main VR Scene experiment launcher
        objectTracker.CaptureOriginalLayout();
        GenerateTrials();
        currentTrial = 0;
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
                // XR locomotion enabled
                break;

            case Phase.Distractor:
                distractorPanel.SetActive(true);
                questionText.text = "Count backwards from 100 by 3s.";
                questionText.gameObject.SetActive(true);
                // Optional simple secondary task UI
                break;

            case Phase.Test:
                objectTracker.ApplyTrialChanges(trials[currentTrial]);
                ShowQuestion(trials[currentTrial]);
                break;

            case Phase.Complete:
                endPanel.SetActive(true);
                questionText.text = "Experiment complete. Data uploaded.";
                firebaseLogger.FinalizeSession(sessionId);
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

        // Log exactly like the other modules (Jamovi-ready)
        firebaseLogger.LogTrial(sessionId, new Dictionary<string, object>
        {
            { "module", "Memory" },
            { "trial", currentTrial },
            { "questionType", t.type.ToString() },
            { "correct", correct },
            { "reactionTimeMs", (int)(rt * 1000) },
            { "choice", choiceIndex },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        });

        // Brief feedback then next
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
        questionText.gameObject.SetActive(false);
        progressSlider.gameObject.SetActive(false);
        distractorPanel.SetActive(false);
        endPanel?.SetActive(false);
        foreach (var b in choiceButtons) b.gameObject.SetActive(false);
    }

    void GenerateTrials()
    {
        trials.Clear();
        // Example: change-detection + location questions (matches dense prop scene)
        for (int i = 0; i < numberOfTestTrials; i++)
        {
            trials.Add(objectTracker.CreateRandomTrial());
        }
    }
}