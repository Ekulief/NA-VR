using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System;

public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("Furniture Items")]
    public List<GameObject> furnitureItems = new();

    [Header("Fade Target")]
    public bool useRandomFadeTarget = true;
    public GameObject specificFadeTarget;

    [Header("Fade Settings")]
    public float fadeDelay = 8f;
    public float fadeDuration = 3f;

    [Header("UI — Instruction Panel")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;
    public Button startCountingButton;

    [Header("UI — Count Input Panel")]
    public GameObject countInputPanel;
    public TMP_Text countPromptText;
    public Button incrementCountButton;
    public Button decrementCountButton;
    public TMP_Text countDisplayText;
    public Button submitCountButton;

    [Header("UI — Awareness Panel")]
    public GameObject awarenessPanel;
    public TMP_Text awarenessQuestionText;
    public Button yesButton;
    public Button noButton;

    [Header("UI — Results Panel")]
    public GameObject resultsPanel;
    public TMP_Text resultsSummaryText;

    private float _trialStartTime;
    private float _countSubmitTime;
    private int _participantCount = 0;
    private int _actualCount;
    private bool _noticedAnomaly;
    private bool _trialComplete = false;
    private GameObject _fadeTarget;
    private List<Renderer[]> _fadeTargetRenderers = new();
    private List<float[]> _originalAlphas = new();
    private float _experimentStartRealtime;
    private DateTime _startedAtUtc;

    private void Start()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (countInputPanel != null) countInputPanel.SetActive(false);
        if (awarenessPanel != null) awarenessPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        _actualCount = furnitureItems.Count;

        if (startCountingButton != null) startCountingButton.onClick.AddListener(OnStartCounting);
        if (incrementCountButton != null) incrementCountButton.onClick.AddListener(() =>
        {
            if (IsSessionPaused()) return;
            SetCount(_participantCount + 1);
        });
        if (decrementCountButton != null) decrementCountButton.onClick.AddListener(() =>
        {
            if (IsSessionPaused()) return;
            SetCount(_participantCount - 1);
        });
        if (submitCountButton != null) submitCountButton.onClick.AddListener(OnSubmitCount);
        if (yesButton != null) yesButton.onClick.AddListener(() => OnAwarenessResponse(true));
        if (noButton != null) noButton.onClick.AddListener(() => OnAwarenessResponse(false));

        StartCoroutine(BeginExperiment());
    }

    private bool IsSessionPaused()
    {
        return SessionController.Instance != null && SessionController.Instance.IsPaused;
    }

    private IEnumerator WaitWhilePaused()
    {
        while (IsSessionPaused())
            yield return null;
    }

    private IEnumerator BeginExperiment()
    {
        Debug.Log("[AttentionalBlindness] Waiting for Firestore config to be ready...");
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        if (config == null)
            config = ExperimentConfigLoader.Current;

        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        if (config != null)
        {
            fadeDelay = config.ab_FadeDelaySeconds;
            fadeDuration = config.ab_FadeDurationSeconds;
            useRandomFadeTarget = config.ab_UseRandomFadeTarget;
            Debug.Log("[AttentionalBlindness] Applied parameters from Firestore config.");
        }

        if (useRandomFadeTarget)
        {
            if (furnitureItems.Count == 0)
            {
                Debug.LogError("[AttentionalBlindness] furnitureItems list is empty!");
                yield break;
            }
            _fadeTarget = furnitureItems[UnityEngine.Random.Range(0, furnitureItems.Count)];
        }
        else
        {
            _fadeTarget = specificFadeTarget;
        }

        if (_fadeTarget == null)
        {
            Debug.LogError("[AttentionalBlindness] No fade target assigned or found!");
            yield break;
        }

        Debug.Log($"[AttentionalBlindness] Fade target selected: '{_fadeTarget.name}'");
        CacheRenderers(_fadeTarget);

        if (instructionText != null)
        {
            instructionText.text = (config != null && !string.IsNullOrEmpty(config.ab_InstructionText))
                ? config.ab_InstructionText
                : "<b>Count the furniture</b>\n\nWalk around the scene and count how many furniture items you can see.\n\nPress <b>Start Counting</b> when you are ready.";
        }

        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    private void OnStartCounting()
    {
        if (IsSessionPaused()) return;

        if (instructionPanel != null) instructionPanel.SetActive(false);

        _trialStartTime = Time.time;
        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;

        ShowCountInput();
        StartCoroutine(FadeOutAfterDelay());
    }

    private void ShowCountInput()
    {
        _participantCount = 0;
        UpdateCountDisplay();
        if (countPromptText != null)
            countPromptText.text = "How many furniture items do you count?";
        if (countInputPanel != null) countInputPanel.SetActive(true);
    }

    private IEnumerator FadeOutAfterDelay()
    {
        float waited = 0f;
        while (waited < fadeDelay)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }

        if (_trialComplete) yield break;

        Debug.Log($"[AttentionalBlindness] Fading out '{_fadeTarget.name}'...");
        yield return StartCoroutine(FadeOut());
        Debug.Log($"[AttentionalBlindness] '{_fadeTarget.name}' fully faded.");
    }

    private void OnSubmitCount()
    {
        if (IsSessionPaused()) return;

        _countSubmitTime = Time.time - _trialStartTime;

        if (countInputPanel != null) countInputPanel.SetActive(false);

        if (awarenessQuestionText != null)
        {
            awarenessQuestionText.text = (config != null && !string.IsNullOrEmpty(config.ab_AwarenessQuestionText))
                ? config.ab_AwarenessQuestionText
                : "While counting the furniture,\ndid you notice anything unusual\nhappening in the scene?";
        }

        if (awarenessPanel != null) awarenessPanel.SetActive(true);
        Debug.Log($"[AttentionalBlindness] Count submitted: {_participantCount} (actual: {_actualCount}) after {_countSubmitTime:F1}s");
    }

    private void OnAwarenessResponse(bool noticed)
    {
        if (IsSessionPaused()) return;

        _noticedAnomaly = noticed;
        _trialComplete = true;

        if (awarenessPanel != null) awarenessPanel.SetActive(false);
        ShowResults();
    }

    private void ShowResults()
    {
        int countDifference = Mathf.Abs(_participantCount - _actualCount);
        string countAccuracy = countDifference == 0
            ? "Correct!"
            : countDifference == 1
                ? $"Off by 1 (actual: {_actualCount})"
                : $"Off by {countDifference} (actual: {_actualCount})";

        if (resultsSummaryText != null)
        {
            resultsSummaryText.text =
                $"Trial Complete\n\n" +
                $"Your count:       {_participantCount}\n" +
                $"Actual count:     {_actualCount}\n" +
                $"Accuracy:         {countAccuracy}\n\n" +
                $"Noticed anomaly:  {(_noticedAnomaly ? "Yes" : "No")}\n" +
                $"Faded item:       {_fadeTarget.name}\n\n" +
                (_noticedAnomaly
                    ? "You noticed the furniture item fading —\nyour attention was broadly distributed."
                    : "You did not notice the fading item.\nThis is the inattentional blindness effect.");
        }

        if (resultsPanel != null) resultsPanel.SetActive(true);
        SaveResultsToFirestore();
    }

    private void CacheRenderers(GameObject target)
    {
        _fadeTargetRenderers.Clear();
        _originalAlphas.Clear();
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            _fadeTargetRenderers.Add(new Renderer[] { r });
            float[] alphas = new float[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                Material mat = r.materials[i];
                SetMaterialTransparent(mat);
                alphas[i] = mat.color.a;
            }
            _originalAlphas.Add(alphas);
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            yield return WaitWhilePaused();

            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            for (int i = 0; i < _fadeTargetRenderers.Count; i++)
            {
                Renderer r = _fadeTargetRenderers[i][0];
                for (int j = 0; j < r.materials.Length; j++)
                {
                    Color c = r.materials[j].color;
                    c.a = alpha;
                    r.materials[j].color = c;
                }
            }
            yield return null;
        }

        if (_fadeTarget != null)
            _fadeTarget.SetActive(false);
    }

    private void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetCount(int value)
    {
        _participantCount = Mathf.Max(0, value);
        UpdateCountDisplay();
    }

    private void UpdateCountDisplay()
    {
        if (countDisplayText != null) countDisplayText.text = _participantCount.ToString();
        if (decrementCountButton != null) decrementCountButton.interactable = _participantCount > 0;
    }

    private async void SaveResultsToFirestore()
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            string studentId = GetConfigString("studentId", "Anonymous");
            string groupId = GetConfigString("groupId", "");
            string blockId = GetConfigString("blockId", "");
            string experimentId = GetConfigString("experimentId", "");
            string vrId = GetConfigString("vrId", "");
            string progressId = GetConfigString("progressId", "");

            if (string.IsNullOrEmpty(progressId) && SessionController.Instance != null)
                progressId = SessionController.Instance.progressDocumentId ?? "";

            float durationSeconds = 0f;
            if (_experimentStartRealtime > 0f)
                durationSeconds = Time.realtimeSinceStartup - _experimentStartRealtime;
            else if (_trialStartTime > 0f)
                durationSeconds = Time.time - _trialStartTime;

            string durationDisplay = FormatDuration(durationSeconds);
            DateTime completedAtUtc = DateTime.UtcNow;

            var configurations = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "fadeDelaySeconds", fadeDelay },
                    { "fadeDurationSeconds", fadeDuration },
                    { "useRandomFadeTarget", useRandomFadeTarget },
                    { "actualFurnitureCount", _actualCount },
                    { "fadedItemName", _fadeTarget != null ? _fadeTarget.name : "" },
                    { "instructionText", config != null ? config.ab_InstructionText ?? "" : "" },
                    { "awarenessQuestionText", config != null ? config.ab_AwarenessQuestionText ?? "" : "" }
                }
            };

            var experimentalResults = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "trialIndex", 0 },
                    { "participantCount", _participantCount },
                    { "actualCount", _actualCount },
                    { "countDifference", Mathf.Abs(_participantCount - _actualCount) },
                    { "countCorrect", _participantCount == _actualCount },
                    { "noticedAnomaly", _noticedAnomaly },
                    { "fadedItemName", _fadeTarget != null ? _fadeTarget.name : "" },
                    { "countSubmitTimeSeconds", _countSubmitTime },
                    { "reactionTimeMs", (int)(_countSubmitTime * 1000f) }
                }
            };

            var doc = new Dictionary<string, object>
            {
                { "blockId", blockId },
                { "experimentId", experimentId },
                { "groupId", groupId },
                { "studentId", studentId },
                { "vrId", vrId },
                { "progressId", progressId },
                { "experimentName", "Attentional_Blindness" },
                { "moduleName", "Attentional_Blindness" },
                { "completionStatus", "Completed" },
                { "duration", durationDisplay },
                { "durationSeconds", durationSeconds },
                { "startedAt", _startedAtUtc.ToString("o") },
                { "completedAt", completedAtUtc.ToString("o") },
                { "timestamp", completedAtUtc.ToString("o") },
                { "configurations", configurations },
                { "experimentalResults", experimentalResults }
            };

            await db.Collection("experimentResults").AddAsync(doc);
            Debug.Log($"[AttentionalBlindness] Saved (duration={durationDisplay}, progressId={progressId})");

            if (!string.IsNullOrEmpty(progressId))
            {
                await db.Collection("experimentProgress").Document(progressId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "sessionControl", "ended" },
                        { "completionStatus", "Completed" },
                        { "completionAt", completedAtUtc.ToString("o") }
                    });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AttentionalBlindness] Failed to save results: {ex.Message}");
        }
    }

    private string GetConfigString(string fieldName, string fallback)
    {
        if (config == null) return fallback;
        switch (fieldName)
        {
            case "studentId":
                return !string.IsNullOrEmpty(config.studentId) ? config.studentId : fallback;
            case "groupId":
                return !string.IsNullOrEmpty(config.groupId) ? config.groupId : fallback;
            case "blockId":
                return !string.IsNullOrEmpty(config.blockId) ? config.blockId : fallback;
            case "experimentId":
                return !string.IsNullOrEmpty(config.experimentId) ? config.experimentId : fallback;
            case "vrId":
                return !string.IsNullOrEmpty(config.vrId) ? config.vrId : fallback;
            case "progressId":
                return !string.IsNullOrEmpty(config.progressId) ? config.progressId : fallback;
            default:
                return fallback;
        }
    }

    private static string FormatDuration(float totalSeconds)
    {
        if (totalSeconds < 0f) totalSeconds = 0f;
        int t = Mathf.FloorToInt(totalSeconds);
        int m = t / 60;
        int s = t % 60;
        return $"{m}:{s:D2}";
    }
}