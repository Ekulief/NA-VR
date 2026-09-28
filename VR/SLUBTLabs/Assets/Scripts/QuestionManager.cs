using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
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

    [Header("Database (Fallback)")]
    public MemoryTargetDatabase[] databases;

    [Header("Settings")]
    public bool randomizeQuestions = true;

    private List<QuestionEntry> allQuestions = new List<QuestionEntry>();
    private int currentQuestionIndex = 0;

    public System.Action<QuestionEntry, int, int> OnQuestionReady;
    public System.Action<QuestionEntry, string, bool, float> OnAnswerSubmitted;
    public System.Action OnAllQuestionsFinished;

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (ExperimentManager.Instance == null)
            yield return new WaitForSeconds(0.1f);

        ExperimentManager.Instance.OnRecallStarted -= OnRecallStarted;
        ExperimentManager.Instance.OnRecallStarted += OnRecallStarted;
        Debug.Log("[QuestionManager] Subscribed to ExperimentManager.");
    }

    private void OnDisable()
    {
        if (ExperimentManager.Instance != null)
            ExperimentManager.Instance.OnRecallStarted -= OnRecallStarted;
    }

    private void OnRecallStarted()
    {
        // Prefer remote config if available
        if (ExperimentConfigLoader.Current != null)
        {
            randomizeQuestions = ExperimentConfigLoader.Current.memory_RandomizeQuestions;
        }

        BuildQuestionList();
        currentQuestionIndex = 0;
        ShowNextQuestion();
    }

    private void BuildQuestionList()
    {
        allQuestions.Clear();

        // 1. Prefer remote targets from Firestore
        if (ExperimentConfigLoader.Current != null &&
            ExperimentConfigLoader.Current.memory_Targets != null &&
            ExperimentConfigLoader.Current.memory_Targets.Count > 0)
        {
            Debug.Log("[QuestionManager] Using remote targets from Firestore.");

            foreach (MemoryTargetEntry entry in ExperimentConfigLoader.Current.memory_Targets)
            {
                AddEntryQuestions(entry);
            }
        }
        else
        {
            // 2. Fallback to ScriptableObject databases
            Debug.Log("[QuestionManager] Using local ScriptableObject databases.");

            foreach (MemoryTargetDatabase db in databases)
            {
                if (db == null) continue;

                foreach (MemoryTargetEntry entry in db.entries)
                {
                    AddEntryQuestions(entry);
                }
            }
        }

        if (randomizeQuestions)
            Shuffle(allQuestions);

        Debug.Log($"[QuestionManager] Built {allQuestions.Count} questions.");
    }

    private void AddEntryQuestions(MemoryTargetEntry entry)
    {
        if (entry.questions == null) return;

        foreach (QuestionData q in entry.questions)
        {
            if (!q.enabled) continue;

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

        OnQuestionReady?.Invoke(current, currentQuestionIndex + 1, allQuestions.Count);

        Debug.Log($"[QuestionManager] Showing Q{currentQuestionIndex + 1}: {current.questionText}");
    }

    public void SubmitAnswer(string selectedAnswer, float reactionTimeMs)
    {
        if (currentQuestionIndex >= allQuestions.Count) return;

        QuestionEntry current = allQuestions[currentQuestionIndex];
        bool correct = selectedAnswer.ToLower().Trim() == current.correctAnswer.ToLower().Trim();

        Debug.Log($"[QuestionManager] Q{currentQuestionIndex + 1} " +
                  $"Answer: {selectedAnswer} | Correct: {current.correctAnswer} | " +
                  $"Result: {(correct ? "✓" : "✗")} | RT: {reactionTimeMs:F0}ms");

        OnAnswerSubmitted?.Invoke(current, selectedAnswer, correct, reactionTimeMs);

        currentQuestionIndex++;
        StartCoroutine(DelayThenNext(0.5f));
    }

    private IEnumerator DelayThenNext(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowNextQuestion();
    }

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