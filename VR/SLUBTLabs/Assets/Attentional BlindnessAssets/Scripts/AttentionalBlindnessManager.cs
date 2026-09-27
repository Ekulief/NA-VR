using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SLUBT Labs — Attentional Blindness Manager
/// Flow: 
/// 1. Waits for Firestore config via ExperimentConfigLoader.IsReady
/// 2. Instruction Panel visible first (uses ExperimentConfig text)
/// 3. Click "Start Counting" -> Shows Count Input Panel
/// 4. Submit Count -> Shows Awareness Panel
/// 5. Answer Awareness Question -> Shows Results Panel
/// </summary>
public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("Furniture Items")]
    [Tooltip("Drag all furniture GameObjects in the scene here.")]
    public List<GameObject> furnitureItems = new();

    [Header("Fade Target")]
    [Tooltip("If true, a random furniture item fades each trial. If false, uses specificFadeTarget.")]
    public bool useRandomFadeTarget = true;

    [Tooltip("The specific furniture item to fade — only used when useRandomFadeTarget is false.")]
    public GameObject specificFadeTarget;

    [Header("Fade Settings")]
    [Tooltip("Seconds after trial starts before the item begins fading.")]
    public float fadeDelay = 8f;

    [Tooltip("How long the fade takes to complete in seconds.")]
    public float fadeDuration = 3f;

    [Header("UI — Instruction Panel (Visible First)")]
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

    // ── State ─────────────────────────────────────────────────────────────────
    private float _trialStartTime;
    private float _countSubmitTime;
    private int _participantCount = 0;
    private int _actualCount;
    private bool _noticedAnomaly;
    private bool _trialComplete = false;
    private GameObject _fadeTarget;
    private List<Renderer[]> _fadeTargetRenderers = new();
    private List<float[]> _originalAlphas = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        // Hide all sub-panels initially
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (countInputPanel != null) countInputPanel.SetActive(false);
        if (awarenessPanel != null) awarenessPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        _actualCount = furnitureItems.Count;

        // Wire UI Listeners
        if (startCountingButton != null) startCountingButton.onClick.AddListener(OnStartCounting);
        if (incrementCountButton != null) incrementCountButton.onClick.AddListener(() => SetCount(_participantCount + 1));
        if (decrementCountButton != null) decrementCountButton.onClick.AddListener(() => SetCount(_participantCount - 1));
        if (submitCountButton != null) submitCountButton.onClick.AddListener(OnSubmitCount);
        if (yesButton != null) yesButton.onClick.AddListener(() => OnAwarenessResponse(true));
        if (noButton != null) noButton.onClick.AddListener(() => OnAwarenessResponse(false));

        StartCoroutine(BeginExperiment());
    }

    // ── Experiment flow ───────────────────────────────────────────────────────
    private IEnumerator BeginExperiment()
    {
        Debug.Log("[AttentionalBlindness] Waiting for Firestore config to be ready...");
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        // Assign active config from loader if missing in inspector
        if (config == null)
        {
            config = ExperimentConfigLoader.Current;
        }

        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        // Apply config settings if present
        if (config != null)
        {
            fadeDelay = config.ab_FadeDelaySeconds;
            fadeDuration = config.ab_FadeDurationSeconds;
            useRandomFadeTarget = config.ab_UseRandomFadeTarget;
            Debug.Log("[AttentionalBlindness] Applied parameters from Firestore config.");
        }

        // Select fade target
        if (useRandomFadeTarget)
        {
            if (furnitureItems.Count == 0)
            {
                Debug.LogError("[AttentionalBlindness] furnitureItems list is empty!");
                yield break;
            }
            _fadeTarget = furnitureItems[Random.Range(0, furnitureItems.Count)];
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

        // Cache renderers for transparency modifications
        CacheRenderers(_fadeTarget);

        // Show instruction text from config
        if (instructionText != null)
        {
            instructionText.text = (config != null && !string.IsNullOrEmpty(config.ab_InstructionText))
                ? config.ab_InstructionText
                : "<b>Count the furniture</b>\n\nWalk around the scene and count how many furniture items you can see.\n\nPress <b>Start Counting</b> when you are ready.";
        }

        // Show Instruction Panel FIRST
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    private void OnStartCounting()
    {
        // Hide instructions & start counting phase
        if (instructionPanel != null) instructionPanel.SetActive(false);

        _trialStartTime = Time.time;

        // Show Count Panel
        ShowCountInput();

        // Trigger item fade sequence after delay
        StartCoroutine(FadeOutAfterDelay());
    }

    private void ShowCountInput()
    {
        _participantCount = 0;
        UpdateCountDisplay();

        if (countPromptText != null)
        {
            countPromptText.text = "How many furniture items do you count?";
        }

        if (countInputPanel != null) countInputPanel.SetActive(true);
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(fadeDelay);

        if (_trialComplete) yield break;

        Debug.Log($"[AttentionalBlindness] Fading out '{_fadeTarget.name}'...");
        yield return StartCoroutine(FadeOut());
        Debug.Log($"[AttentionalBlindness] '{_fadeTarget.name}' fully faded.");
    }

    private void OnSubmitCount()
    {
        _countSubmitTime = Time.time - _trialStartTime;

        // Hide Count Panel immediately upon submission
        if (countInputPanel != null) countInputPanel.SetActive(false);

        // Show Awareness Question Panel
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
        _noticedAnomaly = noticed;
        _trialComplete = true;

        // Hide Awareness Panel immediately
        if (awarenessPanel != null) awarenessPanel.SetActive(false);

        // Show Results Panel
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
    }

    // ── Fade logic ────────────────────────────────────────────────────────────

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

    // ── Count display ─────────────────────────────────────────────────────────
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
}