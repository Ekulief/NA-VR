using UnityEngine;
using UnityEngine.UI;

public class GazeCloseUI : MonoBehaviour
{
    public GameObject uiPanelToClose;
    public float gazeTimeLimit = 1.5f;

    private float timer = 0f;
    private bool isHovering = false;
    private Image img;
    private Color startColor;

    void Start()
    {
        img = GetComponent<Image>();
        if (img != null) startColor = img.color;
    }

    // These names match CardboardReticlePointer exactly
    public void OnPointerEnter() => isHovering = true;

    public void OnPointerExit()
    {
        isHovering = false;
        timer = 0f;
        if (img != null) img.color = startColor;
    }

    // Triggered if the player clicks the headset button
    public void OnPointerClick() => CloseUI();

    void Update()
    {
        if (isHovering)
        {
            timer += Time.deltaTime;

            // Visual feedback: Change color as the gaze "charges"
            if (img != null)
                img.color = Color.Lerp(startColor, Color.red, timer / gazeTimeLimit);

            if (timer >= gazeTimeLimit)
            {
                CloseUI();
            }
        }
    }

    void CloseUI()
    {
        if (uiPanelToClose != null) uiPanelToClose.SetActive(false);
        isHovering = false;
    }
}