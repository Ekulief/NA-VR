using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-view "Paused" overlay for VR (Screen Space - Camera).
/// Does not auto-hide; call Show() / Hide() from SessionController.
/// </summary>
public class PauseOverlay : MonoBehaviour
{
    public static PauseOverlay Instance { get; private set; }

    [Header("Look")]
    public float fontSize = 64f;
    public string pausedMessage = "Session Paused";
    public string subtitleMessage = "Waiting for instructor to resume";
    public Color dimColor = new Color(0f, 0f, 0f, 0.65f);
    public Color titleColor = Color.white;
    public Color subtitleColor = new Color(0.8f, 0.8f, 0.8f);

    private GameObject _canvasObj;
    private bool _ready;

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
        _ready = true;
        Hide();
        Debug.Log("[PauseOverlay] Ready.");
    }

    private void BuildCanvas()
    {
        _canvasObj = new GameObject("PauseOverlayCanvas");
        DontDestroyOnLoad(_canvasObj);

        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.4f;
        canvas.sortingOrder = 1000; // above feedback if needed

        _canvasObj.AddComponent<CanvasScaler>();
        _canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dim
        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(_canvasObj.transform, false);
        RectTransform dimRt = dim.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dim.AddComponent<Image>();
        dimImg.color = dimColor;
        dimImg.raycastTarget = true; // block clicks through overlay

        // Center panel (no need for heavy chrome)
        GameObject panel = new GameObject("MessagePanel");
        panel.transform.SetParent(_canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.15f, 0.4f);
        panelRt.anchorMax = new Vector2(0.85f, 0.65f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.45f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        TMP_Text title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = pausedMessage;
        title.fontSize = fontSize;
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(panel.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.45f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;
        TMP_Text sub = subObj.AddComponent<TextMeshProUGUI>();
        sub.text = subtitleMessage;
        sub.fontSize = fontSize * 0.45f;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = subtitleColor;
    }

    public void Show()
    {
        if (!_ready || _canvasObj == null) return;

        // Re-bind camera if scene changed
        Canvas c = _canvasObj.GetComponent<Canvas>();
        if (c != null && Camera.main != null)
            c.worldCamera = Camera.main;

        _canvasObj.SetActive(true);
    }

    public void Hide()
    {
        if (_canvasObj != null)
            _canvasObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_canvasObj != null)
            Destroy(_canvasObj);
    }
}