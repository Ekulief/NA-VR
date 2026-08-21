using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// SLUBT Labs — Runtime Feedback Display
/// Creates a world space feedback canvas at runtime parented to Camera Offset.
/// Attach to AttentionalBlindnessManager or any experiment manager that needs
/// face-following feedback text.
///
/// SETUP:
///   a) Attach to your AttentionalBlindnessManager GameObject.
///   b) Call ShowFeedback("message", color, duration) from any script.
///   c) No Inspector wiring needed — finds Camera Offset automatically.
/// </summary>
public class FeedbackDisplay : MonoBehaviour
{
    [Header("Canvas Config")]
    public float distanceFromCamera = 1.5f;
    public float canvasWidth = 800f;
    public float canvasHeight = 200f;
    public float fontSize = 52f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private TMP_Text _feedbackText;
    private Coroutine _hideCoroutine;
    private bool _initialised = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(InitialiseAfterFrame());
    }

    private IEnumerator InitialiseAfterFrame()
    {
        // Wait one frame so VR Player is fully loaded into the scene
        yield return null;

        // Find Camera Offset inside VR Player
        Transform cameraOffset = FindCameraOffset();

        if (cameraOffset == null)
        {
            Debug.LogWarning("[FeedbackDisplay] Could not find Camera Offset — " +
                             "feedback canvas will not follow player.");
            yield break;
        }

        BuildFeedbackCanvas(cameraOffset);
        _initialised = true;

        Debug.Log($"[FeedbackDisplay] Feedback canvas created on '{cameraOffset.name}'.");
    }

    // ── Canvas builder ────────────────────────────────────────────────────────
    private void BuildFeedbackCanvas(Transform parent)
    {
        // Canvas GameObject
        GameObject canvasObj = new GameObject("FeedbackCanvas");
        canvasObj.transform.SetParent(parent, false);
        canvasObj.transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Canvas component
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Size
        RectTransform rt = canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(canvasWidth, canvasHeight);

        // TMP Text
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.SetParent(canvasObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        _feedbackText = textObj.AddComponent<TextMeshProUGUI>();
        _feedbackText.fontSize = fontSize;
        _feedbackText.alignment = TextAlignmentOptions.Center;
        _feedbackText.text = "";
        _feedbackText.color = Color.white;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show a feedback message for a set duration then hide it.</summary>
    public void ShowFeedback(string message, Color color, float duration = 2f)
    {
        if (!_initialised || _feedbackText == null)
        {
            Debug.LogWarning("[FeedbackDisplay] Not yet initialised.");
            return;
        }

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _feedbackText.text = message;
        _feedbackText.color = color;
        _hideCoroutine = StartCoroutine(HideAfter(duration));
    }

    /// <summary>Hide feedback immediately.</summary>
    public void HideFeedback()
    {
        if (_feedbackText != null)
            _feedbackText.text = "";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (_feedbackText != null)
            _feedbackText.text = "";
    }

    private Transform FindCameraOffset()
    {
        // Search by common names
        string[] names = { "Camera Offset", "CameraOffset", "VR Player" };
        foreach (string n in names)
        {
            GameObject found = GameObject.Find(n);
            if (found != null)
                return found.transform;
        }

        // Fallback — find Main Camera and use its parent
        Camera main = Camera.main;
        if (main != null && main.transform.parent != null)
            return main.transform.parent;

        return null;
    }
}