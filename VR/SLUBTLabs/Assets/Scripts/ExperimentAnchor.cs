using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[RequireComponent(typeof(TeleportationAnchor))]
public class ExperimentAnchor : MonoBehaviour
{
    [Header("Dynamic Target")]
    [Tooltip("Type the EXACT scene name you want this specific anchor pad to load.")]
    public string sceneToLoad = "Depth Perception Scene";

    private bool _hasTriggered = false;

    private void OnEnable()
    {
        _hasTriggered = false;
        GetComponent<TeleportationAnchor>().selectExited.AddListener(OnAnchorSelected);
    }

    private void OnDisable()
    {
        GetComponent<TeleportationAnchor>().selectExited.RemoveListener(OnAnchorSelected);
    }

    private void OnAnchorSelected(SelectExitEventArgs args)
    {
        if (_hasTriggered) return;
        _hasTriggered = true;

        ExperimentLoader loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader != null)
            loader.LoadScene(sceneToLoad);
        else
            Debug.LogError($"[SLUBT Labs] Could not find ExperimentLoader to load: {sceneToLoad}");
    }
}