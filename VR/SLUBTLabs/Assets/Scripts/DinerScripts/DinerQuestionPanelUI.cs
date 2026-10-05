using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DinerQuestionPanelUI : MonoBehaviour
{
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

    [Header("Highlight")]
    public Color normalBgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color highlightBgColor = Color.white;
    public Color normalTextColor = Color.white;
    public Color highlightTextColor = Color.black;

    public bool showFeedback = true;

    private float questionStartTime;
    private bool waitingForAnswer;

    private void OnEnable() => StartCoroutine(SubscribeWhenReady());

    private IEnumerator SubscribeWhenReady()
    {
        while (DinerQuestionManager.Instance == null)
            yield return new WaitForSeconds(0.1f);

        DinerQuestionManager.Instance.OnQuestionReady -= OnQuestionReady;
        DinerQuestionManager.Instance.OnAllQuestionsFinished -= OnAllQuestionsFinished;
        DinerQuestionManager.Instance.OnQuestionReady += OnQuestionReady;
        DinerQuestionManager.Instance.OnAllQuestionsFinished += OnAllQuestionsFinished;
    }

    private void OnDisable()
    {
        if (DinerQuestionManager.Instance == null) return;
        DinerQuestionManager.Instance.OnQuestionReady -= OnQuestionReady;
        DinerQuestionManager.Instance.OnAllQuestionsFinished -= OnAllQuestionsFinished;
    }

    private void Start()
    {
        HidePanel();
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(() => OnAnswerSelected("true"));
            WireHighlight(yesButton, yesButton.GetComponentInChildren<TextMeshProUGUI>());
        }
        if (noButton != null)
        {
            noButton.onClick.AddListener(() => OnAnswerSelected("false"));
            WireHighlight(noButton, noButton.GetComponentInChildren<TextMeshProUGUI>());
        }
    }

    private void OnQuestionReady(QuestionEntry entry, int current, int total)
    {
        ShowPanel();

        if (questionCounterText) questionCounterText.text = $"Question {current} of {total}";
        if (roomLabelText) roomLabelText.text = FormatRoomName(entry.room);
        if (questionText) questionText.text = entry.questionText;

        if (progressSlider)
        {
            progressSlider.maxValue = total;
            progressSlider.value = current - 1;
        }
        if (progressText) progressText.text = $"{current - 1} / {total}";

        SetupAnswerLayout(entry);
        questionStartTime = Time.realtimeSinceStartup;
        waitingForAnswer = true;
    }

    private void SetupAnswerLayout(QuestionEntry entry)
    {
        if (yesNoGroup != null) yesNoGroup.SetActive(false);
        if (multipleChoiceGroup != null) multipleChoiceGroup.SetActive(false);

        if (entry.questionType == QuestionType.YesNo)
        {
            if (yesNoGroup != null) yesNoGroup.SetActive(true);
            return;
        }

        SetupMultipleChoice(entry.choices);
    }
    private void SetupMultipleChoice(string[] choices)
    {
        if (multipleChoiceGroup) multipleChoiceGroup.SetActive(true);
        if (choiceButtons == null) return;

        foreach (var btn in choiceButtons)
            if (btn) btn.gameObject.SetActive(false);

        if (choices == null || choices.Length == 0) return;

        for (int i = 0; i < choiceButtons.Count; i++)
            if (choiceButtons[i]) choiceButtons[i].onClick.RemoveAllListeners();

        for (int i = 0; i < choices.Length && i < choiceButtons.Count; i++)
        {
            choiceButtons[i].gameObject.SetActive(true);
            if (choiceTexts != null && i < choiceTexts.Count && choiceTexts[i])
                choiceTexts[i].text = choices[i];

            string answer = choices[i];
            choiceButtons[i].onClick.AddListener(() => OnAnswerSelected(answer));

            var label = (choiceTexts != null && i < choiceTexts.Count)
                ? choiceTexts[i]
                : choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            WireHighlight(choiceButtons[i], label);
            ApplyButtonColors(choiceButtons[i], label, false);
        }
    }

    private void OnAnswerSelected(string answer)
    {
        if (!waitingForAnswer) return;
        waitingForAnswer = false;

        float rt = (Time.realtimeSinceStartup - questionStartTime) * 1000f;
        DinerQuestionManager.Instance?.SubmitAnswer(answer, rt);

        if (showFeedback) StartCoroutine(ShowFeedback());
    }

    private IEnumerator ShowFeedback()
    {
        if (feedbackPanel == null) yield break;
        feedbackPanel.SetActive(true);
        if (feedbackText)
        {
            feedbackText.text = "Response recorded!";
            feedbackText.color = Color.white;
        }
        yield return new WaitForSeconds(0.4f);
        feedbackPanel.SetActive(false);
    }

    private void OnAllQuestionsFinished() => HidePanel();

    private void ShowPanel()
    {
        if (questionPanel) questionPanel.SetActive(true);
    }

    private void HidePanel()
    {
        if (questionPanel) questionPanel.SetActive(false);
    }

    private void ApplyButtonColors(Button btn, TextMeshProUGUI label, bool on)
    {
        if (!btn) return;
        var img = btn.GetComponent<Image>();
        if (img) img.color = on ? highlightBgColor : normalBgColor;
        if (label) label.color = on ? highlightTextColor : normalTextColor;
    }

    private void WireHighlight(Button btn, TextMeshProUGUI label)
    {
        if (!btn) return;
        ApplyButtonColors(btn, label, false);

        var trigger = btn.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        void Add(EventTriggerType type, bool on)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(_ => ApplyButtonColors(btn, label, on));
            trigger.triggers.Add(e);
        }

        Add(EventTriggerType.PointerEnter, true);
        Add(EventTriggerType.PointerExit, false);
        Add(EventTriggerType.PointerDown, true);
        Add(EventTriggerType.PointerUp, false);
    }

    private string FormatRoomName(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return "Diner";
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(roomName.Replace("_", " "));
    }
}