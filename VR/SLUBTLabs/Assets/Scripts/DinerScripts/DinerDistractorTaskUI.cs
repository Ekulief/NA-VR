using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DinerDistractorTaskUI : MonoBehaviour
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
    public Color progressColor = new Color(0.2f, 0.6f, 1f);
    public Color urgentColor = new Color(1f, 0.4f, 0.1f);

    private Coroutine sequenceCoroutine;
    private Coroutine countdownCoroutine;

    private void OnEnable() => StartCoroutine(SubscribeWhenReady());

    private IEnumerator SubscribeWhenReady()
    {
        while (ExperimentManager.Instance == null)
            yield return new WaitForSeconds(0.1f);

        ExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
        ExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
        ExperimentManager.Instance.OnDistractorStarted += OnDistractorStarted;
        ExperimentManager.Instance.OnDistractorEnded += OnDistractorEnded;
        Debug.Log("[DinerDistractorTaskUI] Subscribed.");
    }

    private void OnDisable()
    {
        if (ExperimentManager.Instance == null) return;
        ExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
        ExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
    }

    private void Start() => HidePanel();

    private void OnDistractorStarted(float duration)
    {
        // Ensure question UI is not covering distractor
        ShowPanel(duration);
    }

    private void OnDistractorEnded() => HidePanel();

    private void ShowPanel(float duration)
    {
        if (distractorPanel != null)
            distractorPanel.SetActive(true);

        if (headerText != null)
            headerText.text = "Before We Begin...";

        if (instructionText != null)
        {
            if (ExperimentConfigLoader.Current != null &&
                !string.IsNullOrEmpty(ExperimentConfigLoader.Current.memory_DistractorInstructionText))
            {
                instructionText.text = ExperimentConfigLoader.Current.memory_DistractorInstructionText;
            }
            else
            {
                instructionText.text = "Count backwards from 100 by 3s.\n\nSay each number <b>aloud</b>.";
            }
        }

        if (progressSlider != null)
        {
            progressSlider.maxValue = duration;
            progressSlider.value = duration;
        }
        if (progressFill != null)
            progressFill.color = progressColor;

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);

        countdownCoroutine = StartCoroutine(RunCountdown(duration));
        sequenceCoroutine = StartCoroutine(ShowCountingSequence());
    }

    private void HidePanel()
    {
        if (distractorPanel != null)
            distractorPanel.SetActive(false);

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private IEnumerator RunCountdown(float duration)
    {
        float timeRemaining = duration;
        while (timeRemaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(timeRemaining)} seconds remaining";
            if (progressSlider != null)
                progressSlider.value = timeRemaining;
            if (progressFill != null)
                progressFill.color = timeRemaining <= 10f ? urgentColor : progressColor;

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null) countdownText.text = "0 seconds remaining";
        if (progressSlider != null) progressSlider.value = 0f;
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
                if (current - 3 > 0)
                    display += "  →  <color=#555555>?</color>";
                countingSequenceText.text = display;
            }
            yield return new WaitForSeconds(3f);
            current -= 3;
        }
    }
}