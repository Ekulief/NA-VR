using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance;

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public Image logoImage;                  // assign your logo
    public float fadeDuration = 0.6f;

    [Header("World Space Placement")]
    public float distanceFromCamera = 1.5f;  // how far in front of the player
    public float heightOffset = 0f;          // vertical offset

    [Header("First Load")]
    public bool isBootstrapScene = true;    // true only in the first scene that loads

    private Canvas _canvas;
    private Transform _cam;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (isBootstrapScene)
                DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _canvas = GetComponentInChildren<Canvas>();
        if (_canvas == null) _canvas = GetComponent<Canvas>();

        // Force World Space so it does NOT follow the head every frame
        if (_canvas != null)
        {
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 999;
        }

        // Critical: start fully opaque
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
        }
    }

    IEnumerator Start()
    {
        // Place the logo once in front of the player (stationary)
        PlaceInFrontOfPlayer();

        // Wait for XR to finish initializing
        yield return new WaitUntil(() =>
            XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.isInitializationComplete);

        yield return null;
        yield return new WaitForSeconds(0.15f);

        // Fade out
        yield return StartCoroutine(Fade(0f));
    }

    /// <summary>
    /// Places the loading screen once in world space and never moves it again.
    /// This removes the lag when turning your head.
    /// </summary>
    private void PlaceInFrontOfPlayer()
    {
        _cam = Camera.main != null ? Camera.main.transform : null;

        if (_cam == null)
        {
            var found = GameObject.Find("Main Camera") ?? GameObject.Find("CenterEyeAnchor");
            if (found != null) _cam = found.transform;
        }

        if (_cam == null || _canvas == null) return;

        // Position once – never update again
        Vector3 forward = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;

        Vector3 pos = _cam.position + forward * distanceFromCamera;
        pos.y = _cam.position.y + heightOffset;

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(pos - _cam.position);

        // Optional: scale so it looks good at that distance
        // transform.localScale = Vector3.one * 0.01f;   // adjust if needed
    }

    public IEnumerator Fade(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.01f;

        if (targetAlpha <= 0.01f)
            gameObject.SetActive(false);
    }

    public void ShowImmediate()
    {
        if (canvasGroup != null)
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Place it once again (in case the player moved)
            PlaceInFrontOfPlayer();
        }
    }
}