using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SLUBT Labs —  Feedback Display
/// Creates a Screen Space - Camera canvas overlay at runtime.
/// Always appears in front of the player's view in VR — no positioning needed.
/// Attach to the same GameObject as AttentionalBlindnessManager.
/// </summary>
public class FeedbackDisplay : MonoBehaviour
{
    [Header("Config")]
    public float displayDuration = 2f;
    public float fontSize = 72f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private GameObject _canvasObj;
    private TMP_Text _text;
    private Coroutine _hideCoroutine;
    private bool _initialised = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(BuildAfterFrame());
    }

    private IEnumerator BuildAfterFrame()
    {
        yield return null;
        BuildCanvas();
        _initialised = true;
        Debug.Log("[FeedbackDisplay] Overlay canvas built and ready.");
    }

    // ── Canvas builder ────────────────────────────────────────────────────────
    private void BuildCanvas()
    {
        // Canvas
        _canvasObj = new GameObject("FeedbackOverlayCanvas");
        DontDestroyOnLoad(_canvasObj);

        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 999;

        _canvasObj.AddComponent<CanvasScaler>();
        _canvasObj.AddComponent<GraphicRaycaster>();

        // Top of screen panel - no background
        GameObject panelObj = new GameObject("FeedbackPanel");
        panelObj.transform.SetParent(_canvasObj.transform, false);

        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.2f, 0.78f);
        panelRt.anchorMax = new Vector2(0.8f, 0.95f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // No background image

        // Text
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.SetParent(panelObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(24f, 16f);
        textRt.offsetMax = new Vector2(-24f, -16f);

        _text = textObj.AddComponent<TextMeshProUGUI>();
        _text.fontSize = fontSize;
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.color = Color.white;
        _text.text = "";

        // Start hidden
        _canvasObj.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void ShowFeedback(string message, Color textColor, float duration = -1f)
    {
        if (!_initialised || _text == null)
        {
            Debug.LogWarning("[FeedbackDisplay] Not initialised yet.");
            return;
        }

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _text.text = message;
        _text.color = textColor;
        _canvasObj.SetActive(true);

        float dur = duration < 0 ? displayDuration : duration;
        _hideCoroutine = StartCoroutine(HideAfter(dur));
    }

    public void ShowSuccess(string message, float duration = -1f)
    {
        ShowFeedback(message, Color.green, duration);
    }

    public void ShowError(string message, float duration = -1f)
    {
        ShowFeedback(message, new Color(1f, 0.3f, 0.3f), duration);
    }

    public void HideFeedback()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        if (_canvasObj != null)
            _canvasObj.SetActive(false);
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (_canvasObj != null)
            _canvasObj.SetActive(false);
    }
}