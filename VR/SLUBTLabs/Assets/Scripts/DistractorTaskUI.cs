using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DistractorTaskUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR REFERENCES
    // ─────────────────────────────────────────
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

    // ─────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────
    private float totalDuration;
    private Coroutine sequenceCoroutine;

    // ─────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────
    private void OnEnable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnDistractorStarted += OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded += OnDistractorEnded;
        }
    }

    private void OnDisable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
        }
    }

    private void Start()
    {
        HidePanel();
    }

    // ─────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────
    private void OnDistractorStarted(float duration)
    {
        totalDuration = duration;
        ShowPanel(duration);
    }

    private void OnDistractorEnded()
    {
        HidePanel();
    }

    // ─────────────────────────────────────────
    // PANEL CONTROL
    // ─────────────────────────────────────────
    private void ShowPanel(float duration)
    {
        if (distractorPanel != null)
            distractorPanel.SetActive(true);

        // Set static text
        if (headerText != null)
            headerText.text = "Before We Begin...";

        if (instructionText != null)
            instructionText.text =
                "Count backwards from 100 by 3s.\n\nSay each number <b>aloud</b>.";

        // Reset slider
        if (progressSlider != null)
        {
            progressSlider.maxValue = duration;
            progressSlider.value = duration;
        }

        if (progressFill != null)
            progressFill.color = progressColor;

        // Start the countdown display
        StartCoroutine(RunCountdown(duration));

        // Start the counting sequence display
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
    }

    // ─────────────────────────────────────────
    // COUNTDOWN TIMER DISPLAY
    // ─────────────────────────────────────────
    private IEnumerator RunCountdown(float duration)
    {
        float timeRemaining = duration;

        while (timeRemaining > 0f)
        {
            // Update countdown text
            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(timeRemaining)} seconds remaining";

            // Update slider
            if (progressSlider != null)
                progressSlider.value = timeRemaining;

            // Change color in last 10 seconds
            if (progressFill != null)
            {
                progressFill.color = timeRemaining <= 10f
                    ? urgentColor
                    : progressColor;
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        // Ensure it hits exactly 0
        if (countdownText != null)
            countdownText.text = "0 seconds remaining";

        if (progressSlider != null)
            progressSlider.value = 0f;
    }

    // ─────────────────────────────────────────
    // COUNTING SEQUENCE DISPLAY
    // Cycles through numbers to prompt participant
    // ─────────────────────────────────────────
    private IEnumerator ShowCountingSequence()
    {
        int current = 100;

        while (current > 0)
        {
            if (countingSequenceText != null)
            {
                // Show current number prominently
                // with faded previous numbers
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

            // Wait 3 seconds before showing next number
            yield return new WaitForSeconds(3f);

            current -= 3;
        }
    }
}