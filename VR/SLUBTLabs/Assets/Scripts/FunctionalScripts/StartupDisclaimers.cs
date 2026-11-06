using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

/// <summary>
/// Shows informed-consent / disclaimer pages one after another at app start.
/// Each page fades in, stays long enough to read, then fades out automatically.
/// Holding a button for a couple of seconds skips the remaining pages.
///
/// SETUP
///  - Put this component on the ExperimentLoader object (NOT on the UI group itself).
///  - Create a child of the loading canvas with a CanvasGroup, a title TMP and a body TMP.
///  - Set that child and all its children to the LoadingUI layer.
///  - Assign the references below, then assign this component on ExperimentLoader.
///  - Assign Canvas Root (the world-space canvas) and Head (Main Camera) so the
///    pages are placed in front of the player every time.
/// </summary>
public class StartupDisclaimers : MonoBehaviour
{
    [Serializable]
    public class Page
    {
        public string title;
        [TextArea(3, 12)] public string body;
        [Tooltip("0 = calculate automatically from the text length.")]
        public float secondsOverride = 0f;
    }

    [Header("UI (all on the LoadingUI layer)")]
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    [Tooltip("Optional, shows e.g. 1 / 4")]
    public TMP_Text pageIndicatorText;

    [Header("Placement (world-space canvas)")]
    [Tooltip("The root of the world-space canvas that holds the group. Leave empty to skip placement.")]
    public Transform canvasRoot;
    [Tooltip("The XR camera (Main Camera). Falls back to Camera.main if empty.")]
    public Transform head;
    public float distance = 1.5f;
    [Tooltip("Max seconds to wait for the headset to be tracked/worn before showing anyway.")]
    public float headsetWaitTimeout = 10f;
    [Tooltip("Extra delay after the headset is ready so recentering can settle.")]
    public float headsetSettleSeconds = 0.5f;

    [Header("Timing")]
    public float fadeInSeconds = 1f;
    public float fadeOutSeconds = 1f;
    public float gapBetweenPages = 0.4f;
    public float minSecondsPerPage = 5f;
    [Tooltip("Reading speed used to work out how long each page stays on screen.")]
    public float charactersPerSecond = 14f;

    [Header("Behaviour")]
    [Tooltip("If true, the pages are only shown the first time the app is launched on this headset.")]
    public bool showOnlyOnFirstLaunch = false;
    public string prefsKey = "startup_disclaimers_seen";
    [Tooltip("Optional. If assigned, the LAST page waits for this button instead of fading out automatically.")]
    public Button acceptButton;

    [Header("Hold To Skip")]
    public bool allowSkip = true;
    [Tooltip("If true, skipping only works once the pages have been fully viewed on this headset before.")]
    public bool skipOnlyAfterFirstView = true;
    [Tooltip("If true, holding ANY button on either controller skips (trigger, grip, A/B/X/Y, menu, stick click).")]
    public bool anyControllerButton = true;
    [Tooltip("Optional extra action that also counts, e.g. a specific button. Not needed when Any Controller Button is on.")]
    public InputActionReference skipAction;
    public float holdSeconds = 2f;
    [Tooltip("Optional. An Image set to Filled (Radial 360) that shows the hold progress.")]
    public Image skipProgressImage;
    [Tooltip("Optional hint, e.g. 'Hold A to skip'. Only shown when skipping is allowed.")]
    public TMP_Text skipHintText;

    [Header("Pages (edit the wording to match your ethics approval)")]
    public Page[] pages = new Page[]
    {
        new Page
        {
            title = "Welcome",
            body = "We will now run short psychology experiments in virtual reality as part of a class activity."
        },
        new Page
        {
            title = "What is recorded",
            body = "Your responses, timings and actions during each experiment are recorded." +
                   "The instructor and experimenter can watch a live view of what you see during the session."
        },
        new Page
        {
            title = "Your safety",
            body = "Participation is voluntary. You may remove the headset and stop at any time without penalty. " +
                   "Tell your experimenter if you feel dizzy, nauseous or uncomfortable. " +
                   "VR is not recommended if you have epilepsy, photosensitivity or are prone to motion sickness."
        },
        new Page
        {
            title = "Consent",
            body = "By continuing to use this application you confirm that you have read and understood this information " +
                   "and agree to take part."
        }
    };

    public bool IsPlaying { get; private set; }

    private bool _skipRequested;
    private bool _waitingForAccept;

    private bool HasSeenAll => PlayerPrefs.GetInt(prefsKey, 0) == 1;

    /// <summary>True if Play() would actually show something right now.</summary>
    public bool WillShow =>
        group != null && titleText != null && bodyText != null &&
        pages != null && pages.Length > 0 &&
        !(showOnlyOnFirstLaunch && HasSeenAll);

    private bool CanSkip => allowSkip && (!skipOnlyAfterFirstView || HasSeenAll);

    private void Awake()
    {
        if (group != null) group.alpha = 0f;
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        _buttonsDirty = true;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    // Controllers can appear, disappear or be swapped (hand tracking <-> controllers,
    // sleep/wake) at any time. Any such change invalidates the cached buttons.
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is XRController) _buttonsDirty = true;
    }

    public IEnumerator Play()
    {
        if (!WillShow) yield break;

        IsPlaying = true;
        _skipRequested = false;
        _waitingForAccept = false;

        group.gameObject.SetActive(true);
        group.alpha = 0f;
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);

        // Don't start until the headset is tracked/worn, otherwise the panel
        // is placed relative to a head that is not where the player ends up.
        yield return StartCoroutine(WaitForHeadset());

        bool canSkip = CanSkip;
        if (skipHintText != null) skipHintText.gameObject.SetActive(canSkip);
        if (skipProgressImage != null)
        {
            skipProgressImage.fillAmount = 0f;
            skipProgressImage.gameObject.SetActive(canSkip);
        }

        Coroutine holdRoutine = null;
        if (canSkip)
        {
            if (skipAction != null && skipAction.action != null && !skipAction.action.enabled)
                skipAction.action.Enable();

            SnapshotPressedButtons();
            holdRoutine = StartCoroutine(HoldToSkipRoutine());
        }

        bool finishedAll = true;

        for (int i = 0; i < pages.Length; i++)
        {
            Page p = pages[i];
            titleText.text = p.title ?? "";
            bodyText.text = p.body ?? "";
            if (pageIndicatorText != null)
                pageIndicatorText.text = $"{i + 1} / {pages.Length}";

            // Re-place in front of the player for every page.
            PlaceInFrontOfHead();

            yield return StartCoroutine(FadeGroup(0f, 1f, fadeInSeconds));

            bool last = i == pages.Length - 1;
            if (last && acceptButton != null)
            {
                // Explicit consent: skipping never bypasses this button.
                _waitingForAccept = true;
                _skipRequested = false;

                bool accepted = false;
                UnityAction onClick = () => accepted = true;
                acceptButton.onClick.AddListener(onClick);
                acceptButton.gameObject.SetActive(true);

                while (!accepted) yield return null;

                acceptButton.onClick.RemoveListener(onClick);
                acceptButton.gameObject.SetActive(false);
                _waitingForAccept = false;
            }
            else
            {
                int length = (p.title != null ? p.title.Length : 0) + (p.body != null ? p.body.Length : 0);
                float duration = p.secondsOverride > 0f
                    ? p.secondsOverride
                    : Mathf.Max(minSecondsPerPage, length / Mathf.Max(1f, charactersPerSecond));

                yield return StartCoroutine(WaitReading(duration));
            }

            yield return StartCoroutine(FadeGroup(1f, 0f, fadeOutSeconds));

            if (_skipRequested)
            {
                _skipRequested = false;

                if (acceptButton != null && i < pages.Length - 2)
                {
                    // Jump straight to the last page so consent is still required.
                    i = pages.Length - 2;
                    continue;
                }
                if (acceptButton == null && i < pages.Length - 1)
                {
                    finishedAll = false;
                    break;
                }
            }

            yield return new WaitForSecondsRealtime(gapBetweenPages);
        }

        if (holdRoutine != null) StopCoroutine(holdRoutine);
        if (skipProgressImage != null) skipProgressImage.fillAmount = 0f;

        // Only a full viewing counts as "seen".
        if (finishedAll)
        {
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
        }

        IsPlaying = false;
        group.alpha = 0f;
        group.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Headset readiness + placement
    // ------------------------------------------------------------------

    private Transform Head
    {
        get
        {
            if (head != null) return head;
            Camera cam = Camera.main;
            return cam != null ? cam.transform : null;
        }
    }

    private bool IsHeadReady()
    {
        // Not running in XR (e.g. editor without a headset): nothing to wait for.
        if (!UnityEngine.XR.XRSettings.isDeviceActive) return true;

        UnityEngine.XR.InputDevice hmd =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
        if (!hmd.isValid || Head == null) return false;

        if (hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out bool worn) && !worn)
            return false;

        return true;
    }

    private IEnumerator WaitForHeadset()
    {
        float t = 0f;
        while (!IsHeadReady() && t < headsetWaitTimeout)
        {
            t += Dt;
            yield return null;
        }

        // Let tracking / recenter settle.
        if (headsetSettleSeconds > 0f)
            yield return new WaitForSecondsRealtime(headsetSettleSeconds);
    }

    private void PlaceInFrontOfHead()
    {
        Transform h = Head;
        Transform root = canvasRoot;

        // Fallback: use the root canvas that contains the disclaimer group
        // (FadeCanvas in your scene), so it works even if Canvas Root is empty.
        if (root == null && group != null)
        {
            Canvas c = group.GetComponentInParent<Canvas>();
            if (c != null) root = c.rootCanvas.transform;
        }

        if (root == null || h == null) return;

        Vector3 fwd = Vector3.ProjectOnPlane(h.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.01f) fwd = h.forward;
        fwd.Normalize();

        root.position = h.position + fwd * distance;
        root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    // ------------------------------------------------------------------
    // Hold to skip
    // ------------------------------------------------------------------

    private IEnumerator HoldToSkipRoutine()
    {
        float held = 0f;

        while (true)
        {
            bool down = IsSkipHeld() && !_waitingForAccept;
            held = down
                ? held + Dt
                : Mathf.Max(0f, held - Dt * 2f);   // drains quickly when released

            if (skipProgressImage != null)
                skipProgressImage.fillAmount = Mathf.Clamp01(held / Mathf.Max(0.1f, holdSeconds));

            if (held >= holdSeconds)
            {
                Debug.Log($"[StartupDisclaimers] Skip triggered by holding: {_heldSource}");
                _skipRequested = true;
                held = 0f;
            }

            yield return null;
        }
    }

    // Only these named controller buttons can trigger a skip
    private static readonly HashSet<string> SkipButtonNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "triggerPressed", "gripPressed", "primaryButton", "secondaryButton",
            "thumbstickClicked", "joystickClicked", "menu",
            "trackpadClicked", "touchpadClicked", "primary2DAxisClicked", "secondary2DAxisClicked"
        };

    private readonly List<ButtonControl> _controllerButtons = new();
    private readonly HashSet<ButtonControl> _ignoreUntilReleased = new();
    private bool _buttonsDirty = true;
    private string _heldSource = "";

    private void RebuildButtonCache()
    {
        _controllerButtons.Clear();
        _buttonsDirty = false;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is not XRController) continue;

            foreach (InputControl control in device.allControls)
            {
                if (control is ButtonControl button && SkipButtonNames.Contains(control.name))
                    _controllerButtons.Add(button);
            }
        }

        // Forget buttons that belong to devices that no longer exist.
        _ignoreUntilReleased.RemoveWhere(b => !_controllerButtons.Contains(b));

        Debug.Log($"[StartupDisclaimers] Skip buttons found: {_controllerButtons.Count}");
    }

    // Anything already pressed (or stuck pressed) when the pages start is ignored
    // until it has been released once.
    private void SnapshotPressedButtons()
    {
        _ignoreUntilReleased.Clear();
        if (_buttonsDirty)
            RebuildButtonCache();

        foreach (ButtonControl b in _controllerButtons)
            if (b.isPressed) _ignoreUntilReleased.Add(b);
    }

    private bool IsSkipHeld()
    {
        if (anyControllerButton)
        {
            if (_buttonsDirty)
                RebuildButtonCache();

            for (int i = 0; i < _controllerButtons.Count; i++)
            {
                ButtonControl b = _controllerButtons[i];

                if (!b.isPressed)
                {
                    _ignoreUntilReleased.Remove(b);
                    continue;
                }
                if (_ignoreUntilReleased.Contains(b)) continue;

                _heldSource = b.path;
                return true;
            }
        }

        if (skipAction != null && skipAction.action != null && skipAction.action.IsPressed())
        {
            _heldSource = "skipAction";
            return true;
        }

#if UNITY_EDITOR
        // Editor-only: hold Space to skip.
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            _heldSource = "Space key";
            return true;
        }
#endif
        return false;
    }

    // ------------------------------------------------------------------
    // Timing helpers
    // ------------------------------------------------------------------

    // A loading hitch must never count as hold time or reading time.
    private static float Dt => Mathf.Min(Time.unscaledDeltaTime, 0.1f);

    private IEnumerator WaitReading(float seconds)
    {
        float t = 0f;
        while (t < seconds && !_skipRequested)
        {
            t += Dt;
            yield return null;
        }
    }

    private IEnumerator FadeGroup(float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Dt;
            group.alpha = Mathf.SmoothStep(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        group.alpha = to;
    }
}