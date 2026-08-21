using UnityEngine;
using UnityEngine.UI;

public class AttentionalBlindnessUI : MonoBehaviour
{


    [Header("UI References")]

    public Button returnHomeButton;        
    public GameObject confirmationPanel;    


    private ExperimentLoader _experimentLoader;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _experimentLoader = FindAnyObjectByType<ExperimentLoader>();

        returnHomeButton.onClick.AddListener(OnReturnHome);

 

        confirmationPanel.SetActive(false);

    }



    private void OnReturnHome()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (_experimentLoader != null)
        {
            // Loader now handles matching and teleporting directly to "Respawn"
            _experimentLoader.ReturnToHub();
        }
        else
        {
            Debug.LogWarning("[SLUBT Labs] ExperimentLoader not found in any loaded scene.");
        }
    }



}