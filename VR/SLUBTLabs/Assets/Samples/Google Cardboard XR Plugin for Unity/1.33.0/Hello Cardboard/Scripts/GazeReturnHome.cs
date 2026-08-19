using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class GazeReturnHome : MonoBehaviour
{
    [Header("Return Configuration")]
    public string targetScene = "CardboardTest";
    public string spawnPointName = "Respawn";
    public float gazeTimeLimit = 1.5f;

    [Tooltip("Must match the name of your VR Player root GameObject exactly.")]
    public string xrOriginName = "VR Player";

    [Header("Visuals")]
    [Tooltip("Drag your TextMeshPro GameObject here")]
    [SerializeField] private GameObject targetTextObject;

    private float timer = 0f;
    private bool isHovering = false;
    private Color startColor;
    private Component tmpComponent;

    void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.isTrigger = true;

        if (targetTextObject != null)
        {
            tmpComponent = targetTextObject.GetComponent("TextMeshProUGUI");
            if (tmpComponent != null)
            {
                startColor = (Color)tmpComponent.GetType().GetProperty("color").GetValue(tmpComponent, null);
                tmpComponent.GetType().GetProperty("raycastTarget").SetValue(tmpComponent, false, null);
            }
        }
    }

    public void OnPointerEnter() => isHovering = true;

    public void OnPointerExit()
    {
        isHovering = false;
        timer = 0f;
        SetTextColor(startColor);
    }

    public void OnPointerClick() => ExecuteTeleport();

    void Update()
    {
        if (isHovering)
        {
            timer += Time.deltaTime;

            Color lerpedColor = Color.Lerp(startColor, Color.red, timer / gazeTimeLimit);
            SetTextColor(lerpedColor);

            if (timer >= gazeTimeLimit)
                ExecuteTeleport();
        }
    }

    private void SetTextColor(Color targetColor)
    {
        if (tmpComponent != null)
            tmpComponent.GetType().GetProperty("color").SetValue(tmpComponent, targetColor, null);
    }

    void ExecuteTeleport()
    {
        isHovering = false;
        timer = 0f;

        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>();
        if (reticle != null)
            reticle.gameObject.SetActive(false);

        GameObject camRig = GameObject.Find(xrOriginName);
        if (camRig == null)
        {
            Debug.LogError($"[SLUBT Labs] Could not find '{xrOriginName}' — check the name matches exactly.");
            return;
        }

        DontDestroyOnLoad(camRig);

        SceneManager.LoadScene(targetScene);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Re-enable reticle first
        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>(FindObjectsInactive.Include);
        if (reticle != null)
        {
            reticle.gameObject.SetActive(true);
            reticle.SendMessage("OnPointerExit", null, SendMessageOptions.DontRequireReceiver);
        }

        // Wait one frame before repositioning so XR tracking has settled
        StartCoroutine(RepositionAfterFrame());
    }

    private IEnumerator RepositionAfterFrame()
    {
        yield return null; // wait one frame

        GameObject spawn = GameObject.Find(spawnPointName);
        GameObject camRig = GameObject.Find(xrOriginName);

        if (spawn == null)
        {
            Debug.LogError($"[SLUBT Labs] Could not locate '{spawnPointName}' in '{targetScene}'!");
            yield break;
        }

        if (camRig == null)
        {
            Debug.LogError($"[SLUBT Labs] Could not locate '{xrOriginName}' after scene load!");
            yield break;
        }

        Cardboard6DoF dynamicTracker = camRig.GetComponent<Cardboard6DoF>();
        if (dynamicTracker != null)
            dynamicTracker.Recalibrate();

        // Wait another frame after recalibrate so tracking drift clears
        yield return null;

        // Now calculate offset with settled tracking values
        Vector3 cameraWorldPos = Camera.main.transform.position;
        Vector3 rigWorldPos = camRig.transform.position;
        Vector3 trackingOffset = cameraWorldPos - rigWorldPos;

        Vector3 spawnTarget = spawn.transform.position;
        Vector3 targetRigPosition = spawnTarget - trackingOffset;

        camRig.transform.position = targetRigPosition;
        camRig.transform.rotation = spawn.transform.rotation;

        Debug.Log($"[SLUBT Labs] Repositioned after frame settle. " +
                  $"Tracking offset: {trackingOffset} | " +
                  $"Camera now at: {Camera.main.transform.position} | " +
                  $"Spawn target was: {spawnTarget}");
    }
}