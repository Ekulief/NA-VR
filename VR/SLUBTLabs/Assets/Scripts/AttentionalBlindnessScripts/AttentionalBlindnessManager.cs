using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

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


    [Header("Audio Distraction (Radio)")]
    [Tooltip("AudioSource on the Radio object. Leave empty to auto-find.")]
    public AudioSource radioAudioSource;

    [Tooltip("List of possible sounds. One will be chosen at runtime.")]
    public List<AudioClip> audioClips = new();

    [Tooltip("Seconds after the participant presses 'Start Counting' before the sound begins.")]
    public float audioDelay = 5f;

    [Tooltip("If true, pick a random clip from the list. If false, use specificAudioClipIndex.")]
    public bool useRandomAudioClip = true;

    [Tooltip("Index into audioClips when useRandomAudioClip is false.")]
    public int specificAudioClipIndex = 0;

    [Tooltip("Whether the chosen clip should loop.")]
    public bool audioLoop = false;

    [Tooltip("Volume of the radio (0–1).")]
    [Range(0f, 1f)]
    public float audioVolume = 0.8f;

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

    [Header("Locomotion Lock")]
    [Tooltip("Optional: leave empty – runtime FindAnyObjectByType will find the XR Origin across scenes")]
    public GameObject xrOriginOverride;

    // Trial state
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

    // Audio state
    private AudioClip _chosenClip;


    private ContinuousMoveProvider _moveProvider;
    private ContinuousTurnProvider _turnProvider;
    private SnapTurnProvider _snapTurnProvider;
    private TeleportationProvider _teleportProvider;
    private CharacterController _characterController;
    private bool _locomotionLocked;

    private void Start()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (countInputPanel != null) countInputPanel.SetActive(false);
        if (awarenessPanel != null) awarenessPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        _actualCount = 18;

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

    private void OnDestroy()
    {
        UnlockLocomotion();
        StopRadioSound();
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

    // ─────────────────────────────────────────────
    // Locomotion lock / unlock
    // ─────────────────────────────────────────────
    private Vector3 _lockedPosition;
    private readonly List<Behaviour> _disabledLocomotion = new();

    private void LockLocomotion()
    {
        if (_locomotionLocked) return;

        // Find the VR Player (XROrigin)
        GameObject origin = xrOriginOverride;
        Unity.XR.CoreUtils.XROrigin xr = null;

        if (origin == null)
        {
            xr = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xr != null) origin = xr.gameObject;
        }
        else
        {
            xr = origin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        }

        if (origin == null)
        {
            Debug.LogWarning("[AttentionalBlindness] VR Player / XROrigin not found.");
            return;
        }

        Debug.Log($"[AttentionalBlindness] Found VR Player: '{origin.name}' " +
                  $"(pos={origin.transform.position}, children={origin.transform.childCount})");

        DisableIfFound<ContinuousMoveProvider>(origin, "Move (thumbstick walk)");
        DisableIfFound<TeleportationProvider>(origin, "Teleportation");

        DisableIfFound<ContinuousTurnProvider>(origin, "ContinuousTurn (stick)");
        DisableIfFound<SnapTurnProvider>(origin, "SnapTurn (stick)");

        _characterController = origin.GetComponent<CharacterController>();
        if (_characterController != null && _characterController.enabled)
        {
            _characterController.enabled = false;
            Debug.Log("[AttentionalBlindness] Disabled: CharacterController on VR Player");
        }

        _lockedPosition = origin.transform.position;
        _locomotionLocked = true;

        Debug.Log($"[AttentionalBlindness] Locomotion locked. " +
                  $"Disabled {_disabledLocomotion.Count} providers. " +
                  $"Position frozen at {_lockedPosition}. Head look + UI trigger still enabled.");
    }

    private void DisableIfFound<T>(GameObject root, string label) where T : Behaviour
    {
        var comps = root.GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            _disabledLocomotion.Add(c);
            Debug.Log($"[AttentionalBlindness] Disabled: {typeof(T).Name} on '{c.gameObject.name}' ({label})");
        }
    }

    private void UnlockLocomotion()
    {
        if (!_locomotionLocked) return;

        foreach (var c in _disabledLocomotion)
        {
            if (c != null) c.enabled = true;
        }
        _disabledLocomotion.Clear();

        if (_characterController != null)
            _characterController.enabled = true;

        _locomotionLocked = false;
        Debug.Log("[AttentionalBlindness] Locomotion unlocked.");
    }

    private void LateUpdate()
    {
        if (!_locomotionLocked) return;

        GameObject origin = xrOriginOverride;
        if (origin == null)
        {
            var xr = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xr != null) origin = xr.gameObject;
        }
        if (origin == null) return;

        origin.transform.position = _lockedPosition;
    }


    private static GameObject FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent.gameObject;

        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;

            GameObject found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private IEnumerator BeginExperiment()
    {
        Debug.Log("[AttentionalBlindness] Waiting for Firestore config to be ready...");
        yield return new WaitUntil(() => ExperimentConfigLoader.IsReady);

        if (config == null)
            config = ExperimentConfigLoader.Current;

        float delay = config != null ? config.globalInstructionDelay : 1.5f;
        yield return new WaitForSeconds(delay);

        // ── Apply remote config overrides ────────────────────────────────
        if (config != null)
        {
            useRandomFadeTarget = config.ab_UseRandomFadeTarget;
            if (config.ab_FadeDelaySeconds > 0f) fadeDelay = config.ab_FadeDelaySeconds;
            if (config.ab_FadeDurationSeconds > 0f) fadeDuration = config.ab_FadeDurationSeconds;

            // Audio config
            if (config.ab_AudioDelaySeconds > 0f)
                audioDelay = config.ab_AudioDelaySeconds;
            useRandomAudioClip = config.ab_UseRandomAudioClip;
            specificAudioClipIndex = config.ab_SpecificAudioClipIndex;
            audioLoop = config.ab_AudioLoop;
            if (config.ab_AudioVolume > 0f)
                audioVolume = Mathf.Clamp01(config.ab_AudioVolume);

            Debug.Log($"[AttentionalBlindness] Config from Firestore → " +
                      $"ab_UseRandomFadeTarget={config.ab_UseRandomFadeTarget}, " +
                      $"ab_SpecificFadeTarget='{config.ab_SpecificFadeTarget}', " +
                      $"ab_FadeDelaySeconds={config.ab_FadeDelaySeconds}, " +
                      $"ab_FadeDurationSeconds={config.ab_FadeDurationSeconds}, " +
                      $"ab_AudioDelaySeconds={config.ab_AudioDelaySeconds}, " +
                      $"ab_UseRandomAudioClip={config.ab_UseRandomAudioClip}");
        }
        LocomotionLocker.Lock(LocomotionLocker.LockMode.Full, xrOriginOverride);       
        if (radioAudioSource == null)
        {
            var radio = GameObject.Find("Radio");
            if (radio != null)
                radioAudioSource = radio.GetComponent<AudioSource>();
        }

        if (radioAudioSource == null)
            Debug.LogWarning("[AttentionalBlindness] No AudioSource found on Radio.");

        // Choose which clip will play
        _chosenClip = null;

        if (audioClips != null && audioClips.Count > 0)
        {
            if (useRandomAudioClip)
            {
                _chosenClip = audioClips[UnityEngine.Random.Range(0, audioClips.Count)];
            }
            else
            {
                // 1. Prefer selection by name (from Firestore / web app)
                string targetName = config != null ? config.ab_SpecificAudioClipName : null;

                if (!string.IsNullOrEmpty(targetName))
                {
                    targetName = targetName.Trim();

                    // Exact match (case-insensitive)
                    _chosenClip = audioClips.Find(c =>
                        c != null && string.Equals(c.name, targetName, StringComparison.OrdinalIgnoreCase));

                    // Partial match fallback
                    if (_chosenClip == null)
                    {
                        _chosenClip = audioClips.Find(c =>
                            c != null && c.name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    if (_chosenClip != null)
                    {
                        Debug.Log($"[AttentionalBlindness] Audio clip selected by name → '{_chosenClip.name}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[AttentionalBlindness] Could not find clip named '{targetName}'. " +
                                         $"Available clips: {string.Join(", ", audioClips.ConvertAll(c => c != null ? c.name : "null"))}");
                    }
                }

                // 2. Fallback to index if name was not found or not provided
                if (_chosenClip == null)
                {
                    int idx = Mathf.Clamp(specificAudioClipIndex, 0, audioClips.Count - 1);
                    _chosenClip = audioClips[idx];
                    Debug.Log($"[AttentionalBlindness] Falling back to index {idx} → '{_chosenClip?.name}'");
                }
            }
        }

        if (_chosenClip != null)
            Debug.Log($"[AttentionalBlindness] Final audio clip: '{_chosenClip.name}' (delay={audioDelay}s, random={useRandomAudioClip})");
        else
            Debug.LogWarning("[AttentionalBlindness] No audio clips available.");

        // Build furniture list (recursive search so nested objects are found)
        if (config != null && config.ab_FurnitureList != null && config.ab_FurnitureList.Count > 0)
        {
            furnitureItems.Clear();
            Transform parent = GameObject.Find("Vanishing Furniture")?.transform;

            foreach (string name in config.ab_FurnitureList)
            {
                GameObject go = null;
                if (parent != null)
                    go = FindChildRecursive(parent, name);
                if (go == null)
                    go = GameObject.Find(name);

                if (go != null)
                {
                    furnitureItems.Add(go);
                    Debug.Log($"[AttentionalBlindness] Found furniture: '{go.name}'");
                }
                else
                {
                    Debug.LogWarning($"[AttentionalBlindness] Could not find furniture: '{name}'");
                }
            }
        }

        _actualCount = furnitureItems.Count;
        Debug.Log($"[AttentionalBlindness] Furniture list loaded ({_actualCount}): " +
                  string.Join(", ", furnitureItems.ConvertAll(f => f.name)));

        // Select fade target
        _fadeTarget = null;
        if (useRandomFadeTarget)
        {
            if (furnitureItems.Count > 0)
                _fadeTarget = furnitureItems[UnityEngine.Random.Range(0, furnitureItems.Count)];
        }
        else
        {
            string targetName = config?.ab_SpecificFadeTarget;
            Debug.Log($"[AttentionalBlindness] Looking for specific fade target from Firestore: '{targetName}'");

            if (!string.IsNullOrEmpty(targetName))
            {
                // exact
                _fadeTarget = furnitureItems.Find(f => f.name == targetName);

                // tolerant
                if (_fadeTarget == null)
                {
                    _fadeTarget = furnitureItems.Find(f =>
                        f.name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (_fadeTarget == null)
                {
                    Debug.LogError($"[AttentionalBlindness] '{targetName}' not in loaded list. " +
                                   $"Available: {string.Join(", ", furnitureItems.ConvertAll(f => f.name))}");
                }
                else
                {
                    Debug.Log($"[AttentionalBlindness] Matched specific target → '{_fadeTarget.name}'");
                }
            }

            if (_fadeTarget == null)
                _fadeTarget = specificFadeTarget;

            if (_fadeTarget == null && furnitureItems.Count > 0)
            {
                _fadeTarget = furnitureItems[0];
                Debug.LogWarning($"[AttentionalBlindness] Falling back to first item: '{_fadeTarget.name}'");
            }
        }

        if (_fadeTarget == null)
        {
            Debug.LogError("[AttentionalBlindness] No fade target available at all.");
            ExperimentLogger.Log("error", "No fade target assigned or found");
        }
        else
        {
            Debug.Log($"[AttentionalBlindness] Fade target selected: '{_fadeTarget.name}' (random={useRandomFadeTarget})");
            ExperimentLogger.Log("fade_target_selected",
                $"Fade target selected: {_fadeTarget.name}",
                new Dictionary<string, object>
                {
                    { "itemName", _fadeTarget.name },
                    { "useRandom", useRandomFadeTarget }
                });
        }

        if (instructionText != null)
        {
            instructionText.text = (config != null && !string.IsNullOrEmpty(config.ab_InstructionText))
                ? config.ab_InstructionText
                : "<b>Count the furniture</b>\n\nLook around the scene and count how many furniture items you can see.\n\nPress <b>Start Counting</b> when you are ready.";
        }

        if (instructionPanel != null) instructionPanel.SetActive(true);
        LockLocomotion();
        ExperimentLogger.Log("instructions_shown", "Attentional Blindness instructions displayed");
    }

    private void OnStartCounting()
    {
        if (IsSessionPaused()) return;

        if (instructionPanel != null) instructionPanel.SetActive(false);

        _trialStartTime = Time.time;
        _experimentStartRealtime = Time.realtimeSinceStartup;
        _startedAtUtc = DateTime.UtcNow;

        ExperimentLogger.Log("trial_started", "Student began counting furniture");

        ShowCountInput();
        StartCoroutine(FadeOutAfterDelay());
        StartCoroutine(PlayAudioAfterDelay());          
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
        if (_fadeTarget == null) yield break;

        Debug.Log($"[AttentionalBlindness] Fading out '{_fadeTarget.name}'...");
        ExperimentLogger.Log("fade_started", $"Fading out '{_fadeTarget.name}'");

        yield return StartCoroutine(FadeOut());

        Debug.Log($"[AttentionalBlindness] '{_fadeTarget.name}' fully faded.");
        ExperimentLogger.Log("fade_completed", $"'{_fadeTarget.name}' fully faded");
    }

    // ─────────────────────────────────────────────
    // NEW: Audio playback
    // ─────────────────────────────────────────────
    private IEnumerator PlayAudioAfterDelay()
    {
        if (_chosenClip == null || radioAudioSource == null) yield break;

        float waited = 0f;
        while (waited < audioDelay)
        {
            yield return WaitWhilePaused();
            waited += Time.deltaTime;
            yield return null;
        }

        if (_trialComplete) yield break;

        PlayRadioSound();
    }

    private void PlayRadioSound()
    {
        if (radioAudioSource == null || _chosenClip == null) return;

        radioAudioSource.clip = _chosenClip;
        radioAudioSource.loop = audioLoop;
        radioAudioSource.volume = audioVolume;
        radioAudioSource.Play();


        ExperimentLogger.Log("audio_started",
            $"Radio started playing '{_chosenClip.name}' after {audioDelay:F1}s",
            new Dictionary<string, object>
            {
                { "clipName", _chosenClip.name },
                { "delaySeconds", audioDelay },
                { "loop", audioLoop },
                { "volume", audioVolume }
            });
    }

    private void StopRadioSound()
    {
        if (radioAudioSource != null && radioAudioSource.isPlaying)
        {
            radioAudioSource.Stop();
        }
    }

    private void OnSubmitCount()
    {
        if (IsSessionPaused()) return;

        _countSubmitTime = Time.time - _trialStartTime;

        if (countInputPanel != null) countInputPanel.SetActive(false);

        ExperimentLogger.Log("count_submitted",
            $"Student submitted count: {_participantCount} (actual: {_actualCount}) after {_countSubmitTime:F1}s",
            new Dictionary<string, object>
            {
                { "participantCount", _participantCount },
                { "actualCount", _actualCount },
                { "countDifference", Mathf.Abs(_participantCount - _actualCount) },
                { "countSubmitTimeSeconds", _countSubmitTime }
            });

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

        // Stop the radio when the trial ends
        StopRadioSound();

        if (awarenessPanel != null) awarenessPanel.SetActive(false);

        ExperimentLogger.Log("awareness_response",
            noticed ? "Student reported noticing the anomaly" : "Student did not notice the anomaly",
            new Dictionary<string, object>
            {
                { "noticedAnomaly", noticed },
                { "fadedItemName", _fadeTarget != null ? _fadeTarget.name : "" }
            });

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

        string fadedName = _fadeTarget != null ? _fadeTarget.name : "(none)";

        if (resultsSummaryText != null)
        {
            resultsSummaryText.text =
                $"Trial Complete\n\n" +
                $"Your count:       {_participantCount}\n" +
                $"Actual count:     {_actualCount}\n" +
                $"Accuracy:         {countAccuracy}\n\n" +
                $"Noticed anomaly:  {(_noticedAnomaly ? "Yes" : "No")}\n" +
                $"Faded item:       {fadedName}\n\n" +
                (_noticedAnomaly
                    ? "You noticed the furniture item fading —\nyour attention was broadly distributed."
                    : "You did not notice the fading item.\nThis is the inattentional blindness effect.");
        }

        if (resultsPanel != null) resultsPanel.SetActive(true);
        SaveResultsToFirestore();
    }

    // ─────────────────────────────────────────────
    // Fade (no black shadow spot)
    // ─────────────────────────────────────────────
    private void CacheRenderers(GameObject target)
    {
        _fadeTargetRenderers.Clear();
        _originalAlphas.Clear();

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            _fadeTargetRenderers.Add(new Renderer[] { r });

            float[] alphas = new float[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                Material mat = r.materials[i];
                SetMaterialTransparent(mat);
                alphas[i] = mat.HasProperty("_BaseColor")
                    ? mat.GetColor("_BaseColor").a
                    : mat.color.a;
            }
            _originalAlphas.Add(alphas);
        }

        foreach (var col in target.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }

    private IEnumerator FadeOut()
    {
        if (_fadeTarget == null) yield break;

        CacheRenderers(_fadeTarget);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            yield return WaitWhilePaused();
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            for (int i = 0; i < _fadeTargetRenderers.Count; i++)
            {
                Renderer r = _fadeTargetRenderers[i][0];
                if (r == null) continue;

                for (int j = 0; j < r.materials.Length; j++)
                {
                    Material mat = r.materials[j];
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor");
                        c.a = alpha;
                        mat.SetColor("_BaseColor", c);
                    }
                    else
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }
            yield return null;
        }

        if (_fadeTarget != null)
        {
            foreach (var group in _fadeTargetRenderers)
            {
                if (group != null && group[0] != null)
                    group[0].enabled = false;
            }
            _fadeTarget.SetActive(false);
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0);

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // ─────────────────────────────────────────────
    // Count helpers
    // ─────────────────────────────────────────────
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

    // ─────────────────────────────────────────────
    // Firestore
    // ─────────────────────────────────────────────
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
                    { "awarenessQuestionText", config != null ? config.ab_AwarenessQuestionText ?? "" : "" },

                    // NEW audio fields
                    { "audioDelaySeconds", audioDelay },
                    { "useRandomAudioClip", useRandomAudioClip },
                    { "specificAudioClipIndex", specificAudioClipIndex },
                    { "audioLoop", audioLoop },
                    { "audioVolume", audioVolume },
                    { "chosenClipName", _chosenClip != null ? _chosenClip.name : "" }
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

            ExperimentLogger.Log("experiment_completed",
                $"Attentional Blindness completed. Count={_participantCount}/{_actualCount}, Noticed={_noticedAnomaly}",
                new Dictionary<string, object>
                {
                    { "participantCount", _participantCount },
                    { "actualCount", _actualCount },
                    { "noticedAnomaly", _noticedAnomaly },
                    { "fadedItemName", _fadeTarget != null ? _fadeTarget.name : "" },
                    { "durationSeconds", durationSeconds }
                });

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
            ExperimentLogger.Log("error", $"Failed to save results: {ex.Message}");
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