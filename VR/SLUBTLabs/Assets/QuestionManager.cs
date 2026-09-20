using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────
    public static QuestionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ─────────────────────────────────────────
    // INSPECTOR REFERENCES
    // ─────────────────────────────────────────
    [Header("Database")]
    public MemoryTargetDatabase[] databases;

    [Header("Settings")]
    public bool randomizeQuestions = true;

    // ─────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────
    private List<QuestionEntry> allQuestions = new List<QuestionEntry>();
    private int currentQuestionIndex = 0;

    // Events
    public System.Action<QuestionEntry, int, int> OnQuestionReady;
    public System.Action<QuestionEntry, string, bool, float> OnAnswerSubmitted;
    public System.Action OnAllQuestionsFinished;

    // ─────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────
    private void OnEnable()
    {
        // Keep trying to find ExperimentManager 
        // in case it comes from DontDestroyOnLoad
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait until ExperimentManager exists
        while (ExperimentManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Avoid double-subscription if it's already bound
        ExperimentManager.Instance.OnRecallStarted -= OnRecallStarted;
        ExperimentManager.Instance.OnRecallStarted += OnRecallStarted;
        Debug.Log("[QuestionManager] Subscribed to ExperimentManager.");
    }

    private void OnDisable()
    {
        if (ExperimentManager.Instance != null)
            ExperimentManager.Instance.OnRecallStarted -= OnRecallStarted;
    }

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────
    private void OnRecallStarted()
    {
        BuildQuestionList();
        currentQuestionIndex = 0;
        ShowNextQuestion();
    }

    private void BuildQuestionList()
    {
        allQuestions.Clear();

        foreach (MemoryTargetDatabase db in databases)
        {
            if (db == null) continue;

            foreach (MemoryTargetEntry entry in db.entries)
            {
                foreach (QuestionData q in entry.questions)
                {
                    allQuestions.Add(new QuestionEntry
                    {
                        objectID = entry.gameID,
                        objectName = entry.objectName,
                        room = entry.room,
                        questionText = q.questionText,
                        questionType = q.questionType,
                        correctAnswer = q.correctAnswer,
                        choices = q.multipleChoiceOptions
                    });
                }
            }
        }

        // Randomize if enabled
        if (randomizeQuestions)
            Shuffle(allQuestions);

        Debug.Log($"[QuestionManager] Built {allQuestions.Count} questions.");
    }

    // ─────────────────────────────────────────
    // QUESTION FLOW
    // ─────────────────────────────────────────
    public void ShowNextQuestion()
    {
        if (currentQuestionIndex >= allQuestions.Count)
        {
            Debug.Log("[QuestionManager] All questions answered.");
            OnAllQuestionsFinished?.Invoke();
            ExperimentManager.Instance?.OnExperimentComplete();
            return;
        }

        QuestionEntry current = allQuestions[currentQuestionIndex];

        // Fire event — QuestionPanelUI listens to this
        OnQuestionReady?.Invoke(
            current,
            currentQuestionIndex + 1,
            allQuestions.Count
        );

        Debug.Log($"[QuestionManager] Showing Q{currentQuestionIndex + 1}: " +
                  $"{current.questionText}");
    }

    /// <summary>
    /// Called by QuestionPanelUI when participant selects an answer
    /// </summary>
    public void SubmitAnswer(string selectedAnswer, float reactionTimeMs)
    {
        if (currentQuestionIndex >= allQuestions.Count) return;

        QuestionEntry current = allQuestions[currentQuestionIndex];
        bool correct = selectedAnswer.ToLower().Trim() ==
                       current.correctAnswer.ToLower().Trim();

        Debug.Log($"[QuestionManager] Q{currentQuestionIndex + 1} " +
                  $"Answer: {selectedAnswer} | " +
                  $"Correct: {current.correctAnswer} | " +
                  $"Result: {(correct ? "✓" : "✗")} | " +
                  $"RT: {reactionTimeMs:F0}ms");

        // Fire event — FirebaseLogger listens to this
        OnAnswerSubmitted?.Invoke(
            current,
            selectedAnswer,
            correct,
            reactionTimeMs
        );

        currentQuestionIndex++;

        // Small delay before next question
        StartCoroutine(DelayThenNext(0.5f));
    }

    private IEnumerator DelayThenNext(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowNextQuestion();
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    private void Shuffle(List<QuestionEntry> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            QuestionEntry temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}

// ─────────────────────────────────────────
// DATA CLASS — one question instance
// ─────────────────────────────────────────
[System.Serializable]
public class QuestionEntry
{
    public string objectID;
    public string objectName;
    public string room;
    public string questionText;
    public QuestionType questionType;
    public string correctAnswer;
    public string[] choices;
}