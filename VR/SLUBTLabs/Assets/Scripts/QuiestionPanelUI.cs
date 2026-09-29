using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuestionPanelUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR REFERENCES
    // ─────────────────────────────────────────
    [Header("Panel")]
    public GameObject questionPanel;

    [Header("Header UI")]
    public TextMeshProUGUI questionCounterText;
    public TextMeshProUGUI roomLabelText;

    [Header("Question UI")]
    public TextMeshProUGUI questionText;

    [Header("Answer Buttons")]
    public GameObject yesNoGroup;
    public Button yesButton;
    public Button noButton;

    public GameObject multipleChoiceGroup;
    public List<Button> choiceButtons;
    public List<TextMeshProUGUI> choiceTexts;

    [Header("Progress")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    [Header("Feedback")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public Color correctColor = new Color(0.2f, 0.8f, 0.2f);
    public Color incorrectColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Settings")]
    public bool showFeedback = true;

    // ─────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────
    private float questionStartTime;
    private bool waitingForAnswer = false;

    // ─────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────
    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait until QuestionManager exists
        while (QuestionManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Unsubscribe first to avoid duplicate bindings
        QuestionManager.Instance.OnQuestionReady -= OnQuestionReady;
        QuestionManager.Instance.OnAllQuestionsFinished -= OnAllQuestionsFinished;

        // Subscribe to events
        QuestionManager.Instance.OnQuestionReady += OnQuestionReady;
        QuestionManager.Instance.OnAllQuestionsFinished += OnAllQuestionsFinished;

        Debug.Log("[QuestionPanelUI] Subscribed to QuestionManager.");
    }

    private void OnDisable()
    {
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.OnQuestionReady -= OnQuestionReady;
            QuestionManager.Instance.OnAllQuestionsFinished -= OnAllQuestionsFinished;
        }
    }

    private void Start()
    {
        HidePanel();
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        // Hook up YES/NO buttons
        if (yesButton != null)
            yesButton.onClick.AddListener(() => OnAnswerSelected("true"));
        if (noButton != null)
            noButton.onClick.AddListener(() => OnAnswerSelected("false"));
    }

    // ─────────────────────────────────────────
    // QUESTION DISPLAY
    // ─────────────────────────────────────────
    private void OnQuestionReady(QuestionEntry entry,
                                  int current,
                                  int total)
    {
        ShowPanel();

        // Update counter and room label
        if (questionCounterText != null)
            questionCounterText.text = $"Question {current} of {total}";

        if (roomLabelText != null)
            roomLabelText.text = $"📍 {FormatRoomName(entry.room)}";

        // Update question text
        if (questionText != null)
            questionText.text = entry.questionText;

        // Update progress bar
        if (progressSlider != null)
        {
            progressSlider.maxValue = total;
            progressSlider.value = current - 1;
        }

        if (progressText != null)
            progressText.text = $"{current - 1} / {total}";

        // Show correct answer layout
        SetupAnswerLayout(entry);

        // Start RT timer
        questionStartTime = Time.realtimeSinceStartup;
        waitingForAnswer = true;
    }

    private void SetupAnswerLayout(QuestionEntry entry)
    {
        // Hide both groups first
        if (yesNoGroup != null) yesNoGroup.SetActive(false);
        if (multipleChoiceGroup != null) multipleChoiceGroup.SetActive(false);

        switch (entry.questionType)
        {
            case QuestionType.YesNo:
                SetupYesNo();
                break;

            case QuestionType.Count:
            case QuestionType.MultipleChoice:
            case QuestionType.Color:
            case QuestionType.Detail:
                SetupMultipleChoice(entry.choices);
                break;
        }
    }

    private void SetupYesNo()
    {
        if (yesNoGroup != null)
            yesNoGroup.SetActive(true);
    }

    private void SetupMultipleChoice(string[] choices)
    {
        if (multipleChoiceGroup != null)
            multipleChoiceGroup.SetActive(true);

        if (choices == null || choices.Length == 0) return;

        // Hide all buttons first
        foreach (Button btn in choiceButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        // Remove old listeners
        for (int i = 0; i < choiceButtons.Count; i++)
            if (choiceButtons[i] != null)
                choiceButtons[i].onClick.RemoveAllListeners();

        // Set up only the buttons we need
        for (int i = 0; i < choices.Length && i < choiceButtons.Count; i++)
        {
            int index = i; // capture for lambda

            choiceButtons[i].gameObject.SetActive(true);

            if (choiceTexts[i] != null)
                choiceTexts[i].text = choices[i];

            string answer = choices[i];
            choiceButtons[i].onClick.AddListener(
                () => OnAnswerSelected(answer)
            );
        }
    }

    // ─────────────────────────────────────────
    // ANSWER HANDLING
    // ─────────────────────────────────────────
    private void OnAnswerSelected(string answer)
    {
        if (!waitingForAnswer) return;
        waitingForAnswer = false;

        // Calculate RT
        float rt = (Time.realtimeSinceStartup - questionStartTime) * 1000f;

        // Submit to QuestionManager
        QuestionManager.Instance?.SubmitAnswer(answer, rt);

        // Show feedback if enabled
        if (showFeedback)
            StartCoroutine(ShowFeedback(answer));
    }

    // ─────────────────────────────────────────
    // FEEDBACK
    // ─────────────────────────────────────────
    private IEnumerator ShowFeedback(string selectedAnswer)
    {
        if (feedbackPanel == null) yield break;

        feedbackPanel.SetActive(true);

        if (feedbackText != null)
        {
            feedbackText.text = "Response recorded!";
            feedbackText.color = Color.white;
        }

        yield return new WaitForSeconds(0.4f);

        feedbackPanel.SetActive(false);
    }

    // ─────────────────────────────────────────
    // FINISH
    // ─────────────────────────────────────────
    private void OnAllQuestionsFinished()
    {
        HidePanel();
    }

    // ─────────────────────────────────────────
    // PANEL VISIBILITY
    // ─────────────────────────────────────────
    private void ShowPanel()
    {
        if (questionPanel != null)
            questionPanel.SetActive(true);
    }

    private void HidePanel()
    {
        if (questionPanel != null)
            questionPanel.SetActive(false);
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    private string FormatRoomName(string roomName)
    {
        return System.Globalization.CultureInfo
               .CurrentCulture.TextInfo
               .ToTitleCase(roomName.Replace("_", " "));
    }
}