using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackDisplay : MonoBehaviour
{
    public static FeedbackDisplay Instance { get; private set; }

    [Header("Config")]
    public float displayDuration = 2f;
    public float fontSize = 72f;

    private GameObject _canvasObj;
    private TMP_Text _text;
    private Coroutine _hideCoroutine;
    private bool _initialised = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(BuildAfterFrame());
    }

    private IEnumerator BuildAfterFrame()
    {
        yield return null;
        BuildCanvas();
        _initialised = true;
    }

    private void BuildCanvas()
    {
        _canvasObj = new GameObject("FeedbackOverlayCanvas");
        DontDestroyOnLoad(_canvasObj);
        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 999;
        _canvasObj.AddComponent<CanvasScaler>();
        _canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("FeedbackPanel");
        panelObj.transform.SetParent(_canvasObj.transform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.40f);
        panelRt.anchorMax = new Vector2(0.9f, 0.65f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

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
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.color = Color.white;
        _text.text = "";

        _canvasObj.SetActive(false);
    }

    public void ShowFeedback(string message, Color textColor, float duration = -1f)
    {
        if (!_initialised || _text == null) return;

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _text.text = message;
        _text.color = textColor;
        if (_canvasObj != null) _canvasObj.SetActive(true);

        float dur = duration < 0 ? displayDuration : duration;
        _hideCoroutine = StartCoroutine(HideAfter(dur));
    }

    public void ShowSuccess(string message, float duration = -1f) => ShowFeedback(message, Color.green, duration);
    public void ShowError(string message, float duration = -1f) => ShowFeedback(message, new Color(1f, 0.3f, 0.3f), duration);

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
        try
        {
            yield return new WaitForSecondsRealtime(duration);
        }
        finally
        {
            if (_canvasObj != null)
                _canvasObj.SetActive(false);
        }
    }

    private void OnDisable()
    {
        HideFeedback();
    }
}