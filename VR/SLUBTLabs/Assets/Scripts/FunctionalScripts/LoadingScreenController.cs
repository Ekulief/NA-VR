using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance;

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public Image logoImage;          // assign your logo
    public float fadeDuration = 0.6f;

    [Header("First Load")]
    public bool isBootstrapScene = true;   // true only in the first scene that loads

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

        // Critical: start fully opaque so the user never sees the blue/green
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
        }
    }

    IEnumerator Start()
    {
        // Wait for XR to finish initializing (very important in VR)
        yield return new WaitUntil(() =>
            XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.isInitializationComplete);

        // Small extra delay so the first frame of geometry is ready
        yield return null;
        yield return new WaitForSeconds(0.15f);

        // Fade out the loading screen
        yield return StartCoroutine(Fade(0f));
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
    }

    public void ShowImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
        }
    }
}