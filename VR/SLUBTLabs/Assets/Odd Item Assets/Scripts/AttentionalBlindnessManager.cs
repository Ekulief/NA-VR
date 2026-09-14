using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SLUBT Labs — Attentional Blindness Manager
/// Primary task: participant counts furniture items in the scene.
/// Anomaly: one furniture item fades out completely during counting.
/// After submitting count, participant is asked if they noticed anything unusual.
///
/// SETUP:
///   a) Attach to an empty GameObject called "AttentionalBlindnessManager"
///   b) Assign all furniture GameObjects to the furnitureItems list
///   c) If useRandomFadeTarget = true, one is picked randomly each trial
///   d) If false, assign specificFadeTarget manually
///   e) Wire all UI references in Inspector
///
/// FLOW:
///   1. Instruction panel → "Count the furniture"
///   2. After fadeDelay seconds, chosen item fades to invisible
///   3. Participant presses Done Counting → number input appears
///   4. Participant submits count → awareness question appears
///   5. Results shown with count accuracy and awareness result
/// </summary>
public class AttentionalBlindnessManager : MonoBehaviour
{
    [Header("Config")]
    public ExperimentConfig config;

    [Header("Furniture Items")]
    [Tooltip("Drag all furniture GameObjects in the scene here.")]
    public List<GameObject> furnitureItems = new();

    [Header("Fade Target")]
    [Tooltip("If true, a random furniture item fades each trial. " +
             "If false, uses specificFadeTarget.")]
    public bool useRandomFadeTarget = true;

    [Tooltip("The specific furniture item to fade — only used when useRandomFadeTarget is false.")]
    public GameObject specificFadeTarget;

    [Header("Fade Settings")]
    [Tooltip("Seconds after trial starts before the item begins fading.")]
    public float fadeDelay = 8f;

    [Tooltip("How long the fade takes to complete in seconds.")]
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
    private FeedbackDisplay _feedbackDisplay;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        instructionPanel.SetActive(false);
        countInputPanel.SetActive(false);
        awarenessPanel.SetActive(false);
        resultsPanel.SetActive(false);

        _feedbackDisplay = GetComponent<FeedbackDisplay>();
        _actualCount = furnitureItems.Count;

        startCountingButton.onClick.AddListener(OnStartCounting);
        incrementCountButton.onClick.AddListener(() => SetCount(_participantCount + 1));
        decrementCountButton.onClick.AddListener(() => SetCount(_participantCount - 1));
        submitCountButton.onClick.AddListener(OnSubmitCount);
        yesButton.onClick.AddListener(() => OnAwarenessResponse(true));
        noButton.onClick.AddListener(() => OnAwarenessResponse(false));

        StartCoroutine(BeginExperiment());
    }

    // ── Experiment flow ───────────────────────────────────────────────────────
    private IEnumerator BeginExperiment()
    {
        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        // Apply config settings if present
        if (config != null)
        {
            fadeDelay = config.ab_FadeDelaySeconds;
            fadeDuration = config.ab_FadeDurationSeconds;
            useRandomFadeTarget = config.ab_UseRandomFadeTarget;
        }

        // Pick fade target
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

        Debug.Log($"[AttentionalBlindness] Fade target: '{_fadeTarget.name}'");

        // Cache all renderers and their original alpha values
        CacheRenderers(_fadeTarget);

        // Show instruction from config or default fallback
        instructionText.text = config != null ? config.ab_InstructionText :
            $"<b>Count the furniture</b>\n\n" +
            $"Walk around the scene and count how many\n" +
            $"furniture items you can see.\n\n" +
            $"Press <b>Start Counting</b> when you are ready.";

        instructionPanel.SetActive(true);
    }

    private void OnStartCounting()
    {
        instructionPanel.SetActive(false);
        _trialStartTime = Time.time;

        // Start fade after delay
        StartCoroutine(FadeOutAfterDelay());

        Debug.Log($"[AttentionalBlindness] Counting started. " +
                  $"Fade will begin in {fadeDelay}s.");
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(fadeDelay);

        if (_trialComplete) yield break;

        Debug.Log($"[AttentionalBlindness] Fading out '{_fadeTarget.name}'...");
        yield return StartCoroutine(FadeOut());
        Debug.Log($"[AttentionalBlindness] '{_fadeTarget.name}' fully faded.");

        // Wait configured duration before showing input
        float pause = config != null ? config.ab_PostFadePauseSeconds : 2f;
        yield return new WaitForSeconds(pause);

        if (!_trialComplete)
            ShowCountInput();
    }

    private void ShowCountInput()
    {
        _participantCount = 0;
        UpdateCountDisplay();
        countPromptText.text = "How many furniture items did you count?";
        countInputPanel.SetActive(true);
    }

    private void OnSubmitCount()
    {
        _countSubmitTime = Time.time - _trialStartTime;
        countInputPanel.SetActive(false);

        awarenessQuestionText.text = config != null ? config.ab_AwarenessQuestionText :
            "While counting the furniture,\n" +
            "did you notice anything unusual\n" +
            "happening in the scene?";

        awarenessPanel.SetActive(true);

        Debug.Log($"[AttentionalBlindness] Count submitted: {_participantCount} " +
                  $"(actual: {_actualCount}) after {_countSubmitTime:F1}s");
    }

    private void OnAwarenessResponse(bool noticed)
    {
        _noticedAnomaly = noticed;
        awarenessPanel.SetActive(false);
        _trialComplete = true;

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

        resultsPanel.SetActive(true);

        Debug.Log($"[AttentionalBlindness] Result — " +
                  $"Count: {_participantCount}/{_actualCount} | " +
                  $"Noticed: {_noticedAnomaly} | " +
                  $"Fade target: {_fadeTarget.name} | " +
                  $"Time: {_countSubmitTime:F1}s");

        // TODO: SessionDataManager.Instance.RecordAttentionalBlindnessTrial(
        //   _participantCount, _actualCount, _noticedAnomaly, _fadeTarget.name, _countSubmitTime);
    }

    // ── Fade logic ────────────────────────────────────────────────────────────

    private void CacheRenderers(GameObject target)
    {
        _fadeTargetRenderers.Clear();
        _originalAlphas.Clear();

        // Get all renderers including children
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            _fadeTargetRenderers.Add(new Renderer[] { r });

            // Cache original alpha per material
            float[] alphas = new float[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                Material mat = r.materials[i];
                // Enable transparency on the material
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

        // Make completely invisible
        _fadeTarget.SetActive(false);
    }

    private void SetMaterialTransparent(Material mat)
    {
        // URP transparent mode
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
        countDisplayText.text = _participantCount.ToString();
        decrementCountButton.interactable = _participantCount > 0;
    }
}