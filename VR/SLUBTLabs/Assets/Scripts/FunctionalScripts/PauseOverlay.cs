using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseOverlay : MonoBehaviour
{
    public static PauseOverlay Instance { get; private set; }

    [Header("Look")]
    public string pausedMessage = "Session Paused";
    public Color dimColor = new Color(0f, 0f, 0f, 0.85f);
    public Color titleColor = Color.white;

    private const float DISTANCE = 1.2f;
    private static readonly Vector2 PANEL_SIZE = new Vector2(2.6f, 1.8f);

    private GameObject _canvasObj;
    private Canvas _canvas;
    private bool _ready;
    private Transform _followTarget;
    private Transform _cameraTransform;
    private bool _isPaused = false;
    private Vector3 _lockedPosition;
    private Quaternion _lockedRotation;

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
        Debug.Log("[PauseOverlay] Ready");
    }

    private void BuildCanvas()
    {
        _canvasObj = new GameObject("PauseOverlayCanvas");
        DontDestroyOnLoad(_canvasObj);

        _canvas = _canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 1000;

        RectTransform canvasRt = _canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = PANEL_SIZE * 100f;

        var scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        _canvasObj.AddComponent<GraphicRaycaster>();

        // Background
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
        titleRt.anchorMin = new Vector2(0.05f, 0.35f);
        titleRt.anchorMax = new Vector2(0.95f, 0.65f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        TMP_Text title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = pausedMessage;
        title.fontSize = 60f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;
        title.enableAutoSizing = true;
        title.fontSizeMin = 36f;
        title.fontSizeMax = 80f;
        title.fontStyle = FontStyles.Bold;
        title.outlineWidth = 0.3f;
        title.outlineColor = new Color(0f, 0f, 0f, 0.9f);

        _canvasObj.transform.localScale = Vector3.one * 0.01f;
    }

    private void LateUpdate()
    {
        // If paused, forcefully lock the camera position so head movement cannot wander
        if (_isPaused && _cameraTransform != null)
        {
            _cameraTransform.position = _lockedPosition;
            _cameraTransform.rotation = _lockedRotation;
        }

        if (!_ready || _canvasObj == null || !_canvasObj.activeSelf) return;

        if (_followTarget == null)
        {
            var xrCam = GameObject.Find("Main Camera") ?? GameObject.Find("CenterEyeAnchor");
            if (xrCam != null)
            {
                _followTarget = xrCam.transform;
                _cameraTransform = xrCam.transform;
            }
            else if (Camera.main != null)
            {
                _followTarget = Camera.main.transform;
                _cameraTransform = Camera.main.transform;
            }
        }

        if (_followTarget == null) return;

        Vector3 targetPos = _followTarget.position + _followTarget.forward * DISTANCE;
        _canvasObj.transform.position = targetPos;
        _canvasObj.transform.rotation = Quaternion.LookRotation(
            _canvasObj.transform.position - _followTarget.position);
    }

    public void Show()
    {
        if (!_ready || _canvasObj == null) return;

        var xrCam = GameObject.Find("Main Camera") ?? GameObject.Find("CenterEyeAnchor");
        if (xrCam != null) _cameraTransform = xrCam.transform;
        else if (Camera.main != null) _cameraTransform = Camera.main.transform;

        if (_cameraTransform != null)
        {
            // Lock the exact pose right when paused
            _lockedPosition = _cameraTransform.position;
            _lockedRotation = _cameraTransform.rotation;
        }

        _isPaused = true;
        _followTarget = null;
        _canvasObj.SetActive(true);
        Debug.Log("[PauseOverlay] Paused & Head Locked");
    }

    public void Hide()
    {
        _isPaused = false;
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