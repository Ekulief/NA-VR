using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Timer HUD for Memory Dinner. Hooks MemoryExperimentManager (and ExperimentManager if present).
/// </summary>
public class TimerHUD : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roomCounterText;
    public Slider timerSlider;
    public Image sliderFill;
    public GameObject hudPanel;

    [Header("Color Settings")]
    public Color normalColor = new Color(0.2f, 0.8f, 0.2f);
    public Color warningColor = new Color(1f, 0.6f, 0f);
    public Color urgentColor = new Color(1f, 0.1f, 0.1f);

    [Header("Warning Thresholds")]
    public float warningTime = 30f;
    public float urgentTime = 10f;

    private float totalRoomTime = 60f;
    private bool isPulsing = false;
    private Coroutine pulseCoroutine;
    private bool _subscribed;

    private void Start()
    {
        HideHUD();
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait a few frames for managers to Awake
        for (int i = 0; i < 30 && !_subscribed; i++)
        {
            TrySubscribe();
            if (_subscribed) yield break;
            yield return null;
        }
        // Keep trying a bit longer in case manager loads late
        float t = 0f;
        while (!_subscribed && t < 10f)
        {
            TrySubscribe();
            if (_subscribed) yield break;
            t += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void TrySubscribe()
    {
        if (MemoryExperimentManager.Instance != null)
        {
            MemoryExperimentManager.Instance.OnRoomStarted += OnRoomStarted;
            MemoryExperimentManager.Instance.OnRoomEnded += OnRoomEnded;
            MemoryExperimentManager.Instance.OnTimerTick += OnTimerTick;
            MemoryExperimentManager.Instance.OnAllRoomsExplored += OnAllRoomsExplored;
            _subscribed = true;
            Debug.Log("[TimerHUD] Subscribed to MemoryExperimentManager.");
            return;
        }

        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnRoomStarted += OnRoomStarted;
            ExperimentManager.Instance.OnRoomEnded += OnRoomEnded;
            ExperimentManager.Instance.OnTimerTick += OnTimerTick;
            ExperimentManager.Instance.OnAllRoomsExplored += OnAllRoomsExplored;
            _subscribed = true;
            Debug.Log("[TimerHUD] Subscribed to ExperimentManager.");
        }
    }

    private void OnDestroy()
    {
        if (MemoryExperimentManager.Instance != null)
        {
            MemoryExperimentManager.Instance.OnRoomStarted -= OnRoomStarted;
            MemoryExperimentManager.Instance.OnRoomEnded -= OnRoomEnded;
            MemoryExperimentManager.Instance.OnTimerTick -= OnTimerTick;
            MemoryExperimentManager.Instance.OnAllRoomsExplored -= OnAllRoomsExplored;
        }
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnRoomStarted -= OnRoomStarted;
            ExperimentManager.Instance.OnRoomEnded -= OnRoomEnded;
            ExperimentManager.Instance.OnTimerTick -= OnTimerTick;
            ExperimentManager.Instance.OnAllRoomsExplored -= OnAllRoomsExplored;
        }
    }

    private void OnRoomStarted(string roomName, float duration)
    {
        totalRoomTime = duration;
        if (roomNameText != null)
            roomNameText.text = FormatRoomName(roomName);

        if (roomCounterText != null)
        {
            if (MemoryExperimentManager.Instance != null)
            {
                int current = MemoryExperimentManager.Instance.CurrentRoomIndex + 1;
                int total = MemoryExperimentManager.Instance.TotalRooms;
                roomCounterText.text = $"Room {current} of {total}";
            }
            else if (ExperimentManager.Instance != null)
            {
                int current = ExperimentManager.Instance.CurrentRoomIndex + 1;
                int total = ExperimentManager.Instance.TotalRooms;
                roomCounterText.text = $"Room {current} of {total}";
            }
        }

        if (timerSlider != null)
        {
            timerSlider.maxValue = duration;
            timerSlider.value = duration;
        }

        SetSliderColor(normalColor);
        ShowHUD();
    }

    private void OnRoomEnded(string roomName)
    {
        StopPulse();
        HideHUD();
    }

    private void OnTimerTick(float timeRemaining)
    {
        if (timerText != null)
            timerText.text = FormatTime(timeRemaining);

        if (timerSlider != null)
            timerSlider.value = timeRemaining;

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

    private IEnumerator PulseHUD()
    {
        isPulsing = true;
        while (true)
        {
            yield return StartCoroutine(FadePanel(1f, 0.3f, 0.3f));
            yield return StartCoroutine(FadePanel(0.3f, 1f, 0.3f));
        }
    }

    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
        if (hudPanel == null) yield break;
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
        if (hudPanel != null)
        {
            CanvasGroup cg = hudPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins}:{secs:00}";
    }

    private string FormatRoomName(string roomName)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase((roomName ?? "").Replace("_", " "));
    }

    private void SetSliderColor(Color color)
    {
        if (sliderFill != null)
            sliderFill.color = color;
        if (timerText != null)
            timerText.color = color;
    }
}