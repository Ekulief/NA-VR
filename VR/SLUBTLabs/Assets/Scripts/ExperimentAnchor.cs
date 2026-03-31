using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[RequireComponent(typeof(TeleportationAnchor))]
public class ExperimentAnchor : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<TeleportationAnchor>().selectExited.AddListener(OnAnchorSelected);
    }

    private void OnAnchorSelected(SelectExitEventArgs args)
    {
        GetComponent<TeleportationAnchor>().selectExited.RemoveListener(OnAnchorSelected);
        FindAnyObjectByType<ExperimentLoader>()?.LoadScene();
    }
}