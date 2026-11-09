using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DistractorTaskUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject distractorPanel;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI countingSequenceText;
    public Slider progressSlider;
    public Image progressFill;

    [Header("Settings")]
    public Color progressColor = new Color(0.2f, 0.6f, 1f);  // blue
    public Color urgentColor = new Color(1f, 0.4f, 0.1f);    // orange

    private Coroutine sequenceCoroutine;

    private void OnEnable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnDistractorStarted += OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded += OnDistractorEnded;
            ExperimentManager.Instance.OnTimerTick += OnTimerTick;
        }
    }

    private void OnDisable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
            ExperimentManager.Instance.OnTimerTick -= OnTimerTick;
        }
    }

    private void Start()
    {
        HidePanel();
    }

    private void OnDistractorStarted(float duration)
    {
        ShowPanel(duration);
    }

    private void OnDistractorEnded()
    {
        HidePanel();
    }

    private void ShowPanel(float duration)
    {
        if (distractorPanel != null)
            distractorPanel.SetActive(true);

        if (headerText != null)
            headerText.text = "Before We Begin...";

        if (instructionText != null)
        {
            if (ExperimentConfigLoader.Current != null &&
                !string.IsNullOrEmpty(ExperimentConfigLoader.Current.memory2_DistractorInstructionText))
            {
                instructionText.text = ExperimentConfigLoader.Current.memory2_DistractorInstructionText;
            }
            else
            {
                instructionText.text = "Count backwards from 100 by 3s.\n\nSay each number <b>aloud</b>.";
            }
        }

        if (countdownText != null)
            countdownText.text = $"{Mathf.CeilToInt(duration)} seconds remaining";

        if (progressSlider != null)
        {
            progressSlider.maxValue = duration;
            progressSlider.value = duration;
        }
        if (progressFill != null)
            progressFill.color = progressColor;

        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(ShowCountingSequence());
    }

    private void HidePanel()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (distractorPanel != null)
            distractorPanel.SetActive(false);
    }

    private void OnTimerTick(float timeRemaining)
    {
        if (ExperimentManager.Instance != null && ExperimentManager.Instance.CurrentState == ExperimentManager.ExperimentState.DistractorTask)
        {
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(timeRemaining)} seconds remaining";

            if (progressSlider != null)
                progressSlider.value = timeRemaining;

            if (progressFill != null)
            {
                progressFill.color = timeRemaining <= 10f ? urgentColor : progressColor;
            }
        }
    }

    private IEnumerator ShowCountingSequence()
    {
        int current = 100;

        while (current > 0)
        {
            if (countingSequenceText != null)
            {
                int prev1 = current + 3;
                int prev2 = current + 6;

                string display = "";

                if (prev2 > 0 && prev2 <= 100)
                    display += $"<color=#555555>{prev2}</color>  →  ";

                if (prev1 > 0 && prev1 <= 100)
                    display += $"<color=#999999>{prev1}</color>  →  ";

                display += $"<color=#FFFFFF><b>{current}</b></color>";

                int next = current - 3;
                if (next > 0)
                    display += $"  →  <color=#555555>?</color>";

                countingSequenceText.text = display;
            }

            yield return new WaitForSeconds(3f);
            current -= 3;
        }
    }
}