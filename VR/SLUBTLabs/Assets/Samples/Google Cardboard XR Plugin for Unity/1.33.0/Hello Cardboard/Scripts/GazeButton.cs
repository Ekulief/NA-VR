using UnityEngine;

// This forces a Box Collider onto the object to capture the VR Gaze flawlessly
[RequireComponent(typeof(BoxCollider))]
public class GazeCloseUI : MonoBehaviour
{
    public GameObject uiPanelToClose;
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
        // 1. Configure the Box Collider area
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true; // Makes sure it detects gaze without physically blocking objects
        }

        // 2. Safely find and cache TextMeshPro
        if (targetTextObject != null)
        {
            tmpComponent = targetTextObject.GetComponent("TextMeshProUGUI");
            if (tmpComponent != null)
            {
                // Cache the text starting color
                startColor = (Color)tmpComponent.GetType().GetProperty("color").GetValue(tmpComponent, null);

                // FORCE the text mesh to stop raycasting so it doesn't fight the BoxCollider
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

    public void OnPointerClick() => CloseUI();

    void Update()
    {
        if (isHovering)
        {
            timer += Time.deltaTime;

            // Smoothly blend text color to red as the player gazes at the area
            Color lerpedColor = Color.Lerp(startColor, Color.red, timer / gazeTimeLimit);
            SetTextColor(lerpedColor);

            if (timer >= gazeTimeLimit)
            {
                CloseUI();
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

    void CloseUI()
    {
        if (uiPanelToClose != null) uiPanelToClose.SetActive(false);
        isHovering = false;
        timer = 0f;
    }
}