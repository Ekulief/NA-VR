using UnityEngine;
using UnityEngine.SceneManagement;

// This forces a Box Collider onto the object to capture the VR Gaze flawlessly
[RequireComponent(typeof(BoxCollider))]
public class GazeReturnHome : MonoBehaviour
{
    [Header("Return Configuration")]
    public string targetScene = "CardboardTest";
    public string spawnPointName = "Respawn";
    public float gazeTimeLimit = 1.5f;

    [Header("Visuals")]
    [Tooltip("Drag your TextMeshPro GameObject here")]
    [SerializeField] private GameObject targetTextObject;

    private float timer = 0f;
    private bool isHovering = false;
    private Color startColor;
    private Component tmpComponent;

    void Start()
    {
        // 1. Configure the Box Collider area automatically
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true; // Detects gaze without physically blocking elements
        }

        // 2. Safely find and cache TextMeshPro using reflection (avoids missing dependency errors)
        if (targetTextObject != null)
        {
            tmpComponent = targetTextObject.GetComponent("TextMeshProUGUI");
            if (tmpComponent != null)
            {
                // Cache the text starting color
                startColor = (Color)tmpComponent.GetType().GetProperty("color").GetValue(tmpComponent, null);

                // FORCE the text mesh to stop raycasting so it doesn't fight our BoxCollider
                tmpComponent.GetType().GetProperty("raycastTarget").SetValue(tmpComponent, false, null);
            }
        }
    }

    // These names match CardboardReticlePointer's SendMessage triggers exactly
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

            // Smoothly blend text color to red as the player gazes at the button
            Color lerpedColor = Color.Lerp(startColor, Color.red, timer / gazeTimeLimit);
            SetTextColor(lerpedColor);

            if (timer >= gazeTimeLimit)
            {
                ExecuteTeleport();
            }
        }
    }

    private void SetTextColor(Color targetColor)
    {
        if (tmpComponent != null)
        {
            tmpComponent.GetType().GetProperty("color").SetValue(tmpComponent, targetColor, null);
        }
    }

    void ExecuteTeleport()
    {
        isHovering = false;
        timer = 0f;

        // Temporarily disable the reticle during scene transition
        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>();
        if (reticle != null)
        {
            reticle.gameObject.SetActive(false);
        }

        // Locate your persistent Player VR Camera Rig
        GameObject camRig = Camera.main.transform.parent != null ?
                           Camera.main.transform.parent.gameObject : Camera.main.gameObject;

        DontDestroyOnLoad(camRig);

        // Transition to your Hub scene
        SceneManager.LoadScene(targetScene);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawn = GameObject.Find(spawnPointName);
        GameObject camRig = Camera.main.transform.parent != null ?
                           Camera.main.transform.parent.gameObject : Camera.main.gameObject;

        if (spawn != null && camRig != null)
        {
            // Clear baseline tracking drift history
            Cardboard6DoF dynamicTracker = camRig.GetComponent<Cardboard6DoF>();
            if (dynamicTracker != null)
            {
                dynamicTracker.Recalibrate();
            }

            // Snap the player rig directly on top of your "Respawn" object
            camRig.transform.position = spawn.transform.position;
            camRig.transform.rotation = spawn.transform.rotation;
        }
        else
        {
            if (spawn == null)
            {
                Debug.LogError($"[SLUBT Labs] Could not locate a GameObject named '{spawnPointName}' in '{targetScene}'!");
            }
        }

        // Re-enable and reset the gaze pointer visual state
        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>(FindObjectsInactive.Include);
        if (reticle != null)
        {
            reticle.gameObject.SetActive(true);
            reticle.SendMessage("OnPointerExit", null, SendMessageOptions.DontRequireReceiver);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}