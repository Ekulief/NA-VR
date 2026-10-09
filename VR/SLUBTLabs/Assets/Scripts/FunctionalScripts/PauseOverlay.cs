using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR-friendly World-Space "Paused" overlay that sits in front of the headset.
/// Call Show() / Hide() from SessionController.
/// </summary>
public class PauseOverlay : MonoBehaviour
{
    public static PauseOverlay Instance { get; private set; }

    [Header("Look")]
    public float fontSize = 0.08f;               // world-space size
    public string pausedMessage = "Session Paused";
    public string subtitleMessage = "Waiting for instructor to resume";
    public Color dimColor = new Color(0f, 0f, 0f, 0.75f);
    public Color titleColor = Color.white;
    public Color subtitleColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Placement")]
    public float distanceFromCamera = 1.2f;      // meters in front of eyes
    public Vector2 panelSize = new Vector2(1.6f, 0.7f);

    private GameObject _canvasObj;
    private Canvas _canvas;
    private bool _ready;
    private Transform _followTarget;             // usually Camera.main / XR camera

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
        yield return null;                       // wait one frame so Camera.main exists
        BuildCanvas();
        _ready = true;
        Hide();
        Debug.Log("[PauseOverlay] Ready (World Space).");
    }

    private void BuildCanvas()
    {
        _canvasObj = new GameObject("PauseOverlayCanvas");
        DontDestroyOnLoad(_canvasObj);

        _canvas = _canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 1000;

        // Make it a reasonable world size
        RectTransform canvasRt = _canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = panelSize * 100f;   // TMP works better with larger units

        // Add scaler + raycaster (optional)
        var scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        _canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dim (covers the whole panel)
        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(_canvasObj.transform, false);
        RectTransform dimRt = dim.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dim.AddComponent<Image>();
        dimImg.color = dimColor;
        dimImg.raycastTarget = true;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_canvasObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.45f);
        titleRt.anchorMax = new Vector2(0.95f, 0.9f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        TMP_Text title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = pausedMessage;
        title.fontSize = 48f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = 60f;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(_canvasObj.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.05f, 0.1f);
        subRt.anchorMax = new Vector2(0.95f, 0.45f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        TMP_Text sub = subObj.AddComponent<TextMeshProUGUI>();
        sub.text = subtitleMessage;
        sub.fontSize = 28f;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = subtitleColor;
        sub.enableAutoSizing = true;
        sub.fontSizeMin = 16f;
        sub.fontSizeMax = 36f;

        // Scale the whole canvas down to comfortable world size
        _canvasObj.transform.localScale = Vector3.one * 0.01f;
    }

    private void LateUpdate()
    {
        if (!_ready || _canvasObj == null || !_canvasObj.activeSelf) return;

        // Keep following the main camera / XR camera
        if (_followTarget == null)
            _followTarget = Camera.main != null ? Camera.main.transform : null;

        if (_followTarget == null) return;

        // Place in front of the user, always facing them
        Vector3 targetPos = _followTarget.position + _followTarget.forward * distanceFromCamera;
        _canvasObj.transform.position = targetPos;
        _canvasObj.transform.rotation = Quaternion.LookRotation(
            _canvasObj.transform.position - _followTarget.position);
    }

    public void Show()
    {
        if (!_ready || _canvasObj == null)
        {
            Debug.LogWarning("[PauseOverlay] Show called before ready.");
            return;
        }

        // Force re-find camera in case scene changed
        _followTarget = Camera.main != null ? Camera.main.transform : null;

        _canvasObj.SetActive(true);
        Debug.Log("[PauseOverlay] Shown");
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