using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Distractor UI for Memory Dinner. Listens to MemoryExperimentManager
/// (and ExperimentManager if present).
/// </summary>
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
    private bool _subscribed;

    private void Start()
    {
        HidePanel();
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        float t = 0f;
        while (!_subscribed && t < 15f)
        {
            TrySubscribe();
            if (_subscribed) yield break;
            t += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        if (!_subscribed)
            Debug.LogWarning("[DinerDistractorTaskUI] No manager found to subscribe to.");
    }

    private void TrySubscribe()
    {
        if (MemoryExperimentManager.Instance != null)
        {
            MemoryExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
            MemoryExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
            MemoryExperimentManager.Instance.OnDistractorStarted += OnDistractorStarted;
            MemoryExperimentManager.Instance.OnDistractorEnded += OnDistractorEnded;
            _subscribed = true;
            Debug.Log("[DinerDistractorTaskUI] Subscribed to MemoryExperimentManager.");
            return;
        }

        // Fallback if someone uses ExperimentManager in this scene
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
            ExperimentManager.Instance.OnDistractorStarted += OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded += OnDistractorEnded;
            _subscribed = true;
            Debug.Log("[DinerDistractorTaskUI] Subscribed to ExperimentManager.");
        }
    }

    private void OnDestroy()
    {
        if (MemoryExperimentManager.Instance != null)
        {
            MemoryExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
            MemoryExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
        }
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnDistractorStarted -= OnDistractorStarted;
            ExperimentManager.Instance.OnDistractorEnded -= OnDistractorEnded;
        }
    }

    private void OnDistractorStarted(float duration)
    {
        Debug.Log($"[DinerDistractorTaskUI] Distractor started ({duration}s).");
        ShowPanel(duration);
    }

    private void OnDistractorEnded()
    {
        Debug.Log("[DinerDistractorTaskUI] Distractor ended.");
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
        // Match MemoryExperimentManager: realtime so pause cannot desync UI vs manager
        float endAt = Time.realtimeSinceStartup + duration;
        while (Time.realtimeSinceStartup < endAt)
        {
            float timeRemaining = endAt - Time.realtimeSinceStartup;

            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(timeRemaining)} seconds remaining";

            if (progressSlider != null)
                progressSlider.value = timeRemaining;

            if (progressFill != null)
                progressFill.color = timeRemaining <= 10f ? urgentColor : progressColor;

            yield return null;
        }

        if (countdownText != null) countdownText.text = "0 seconds remaining";
        if (progressSlider != null) progressSlider.value = 0f;
        // Panel stays until OnDistractorEnded from the manager (should fire immediately after)
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