using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[RequireComponent(typeof(TeleportationAnchor))]
public class ExperimentAnchor : MonoBehaviour
{
    [Header("Dynamic Target")]
    [Tooltip("Type the EXACT scene name you want this specific anchor pad to load.")]
    public string sceneToLoad = "Depth Perception Scene";

    private void Awake()
    {
        GetComponent<TeleportationAnchor>().selectExited.AddListener(OnAnchorSelected);
    } 

    private void OnAnchorSelected(SelectExitEventArgs args)
    {
        // Unsubscribe instantly so it doesn't double-trigger
        GetComponent<TeleportationAnchor>().selectExited.RemoveListener(OnAnchorSelected);

        // Find the main manager and pass our unique, custom scene name right into it!
        ExperimentLoader loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader != null)
        {
            loader.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"[SLUBT Labs] Could not find ExperimentLoader in the scene to load: {sceneToLoad}");
        }
    }
}