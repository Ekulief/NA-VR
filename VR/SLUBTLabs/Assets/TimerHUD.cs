using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TimerHUD : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR REFERENCES
    // ─────────────────────────────────────────
    [Header("UI References")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roomCounterText;
    public Slider timerSlider;
    public Image sliderFill;
    public GameObject hudPanel;

    [Header("Color Settings")]
    public Color normalColor = new Color(0.2f, 0.8f, 0.2f);   // green
    public Color warningColor = new Color(1f, 0.6f, 0f);       // orange
    public Color urgentColor = new Color(1f, 0.1f, 0.1f);      // red

    [Header("Warning Thresholds")]
    public float warningTime = 30f;   // turns orange below this
    public float urgentTime = 10f;    // turns red and pulses below this

    // ─────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────
    private float totalRoomTime = 60f;
    private bool isPulsing = false;
    private Coroutine pulseCoroutine;

    // ─────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────
    private void OnEnable()
    {
        // Subscribe to ExperimentManager events
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnRoomStarted += OnRoomStarted;
            ExperimentManager.Instance.OnRoomEnded += OnRoomEnded;
            ExperimentManager.Instance.OnTimerTick += OnTimerTick;
            ExperimentManager.Instance.OnAllRoomsExplored += OnAllRoomsExplored;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnRoomStarted -= OnRoomStarted;
            ExperimentManager.Instance.OnRoomEnded -= OnRoomEnded;
            ExperimentManager.Instance.OnTimerTick -= OnTimerTick;
            ExperimentManager.Instance.OnAllRoomsExplored -= OnAllRoomsExplored;
        }
    }

    private void Start()
    {
        // Hide HUD at start
        HideHUD();
    }

    // ─────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────
    private void OnRoomStarted(string roomName, float duration)
    {
        totalRoomTime = duration;

        // Update room name
        roomNameText.text = FormatRoomName(roomName);

        // Update room counter
        if (ExperimentManager.Instance != null)
        {
            int current = ExperimentManager.Instance.CurrentRoomIndex + 1;
            int total = ExperimentManager.Instance.TotalRooms;
            roomCounterText.text = $"Room {current} of {total}";
        }

        // Reset slider
        if (timerSlider != null)
        {
            timerSlider.maxValue = duration;
            timerSlider.value = duration;
        }

        // Reset color
        SetSliderColor(normalColor);

        // Show HUD
        ShowHUD();
    }

    private void OnRoomEnded(string roomName)
    {
        // Stop pulsing
        StopPulse();
        HideHUD();
    }

    private void OnTimerTick(float timeRemaining)
    {
        // Update timer text
        timerText.text = FormatTime(timeRemaining);

        // Update slider
        if (timerSlider != null)
            timerSlider.value = timeRemaining;

        // Update color based on time remaining
        if (timeRemaining <= urgentTime)
        {
            SetSliderColor(urgentColor);
            if (!isPulsing)
                pulseCoroutine = StartCoroutine(PulseHUD());
        }
        else if (timeRemaining <= warningTime)
        {
            SetSliderColor(warningColor);
            StopPulse();
        }
        else
        {
            SetSliderColor(normalColor);
            StopPulse();
        }
    }

    private void OnAllRoomsExplored()
    {
        HideHUD();
    }

    // ─────────────────────────────────────────
    // HUD VISIBILITY
    // ─────────────────────────────────────────
    private void ShowHUD()
    {
        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    private void HideHUD()
    {
        if (hudPanel != null)
            hudPanel.SetActive(false);

        StopPulse();
    }

    // ─────────────────────────────────────────
    // PULSE EFFECT (urgent warning)
    // ─────────────────────────────────────────
    private IEnumerator PulseHUD()
    {
        isPulsing = true;

        while (true)
        {
            // Fade to semi-transparent
            yield return StartCoroutine(
                FadePanel(1f, 0.3f, 0.3f)
            );
            // Fade back to full
            yield return StartCoroutine(
                FadePanel(0.3f, 1f, 0.3f)
            );
        }
    }

    private IEnumerator FadePanel(float startAlpha,
                                   float endAlpha,
                                   float duration)
    {
        CanvasGroup cg = hudPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = hudPanel.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        isPulsing = false;

        // Reset alpha
        if (hudPanel != null)
        {
            CanvasGroup cg = hudPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins}:{secs:00}";
    }

    private string FormatRoomName(string roomName)
    {
        // Converts "kitchen" → "Kitchen"
        // Converts "living_room" → "Living Room"
        return System.Globalization.CultureInfo
               .CurrentCulture
               .TextInfo
               .ToTitleCase(roomName.Replace("_", " "));
    }

    private void SetSliderColor(Color color)
    {
        if (sliderFill != null)
            sliderFill.color = color;

        if (timerText != null)
            timerText.color = color;
    }
}
