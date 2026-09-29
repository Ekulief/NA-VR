using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class XRMaterialHover : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The MeshRenderer whose material will change. Defaults to this object if left empty.")]
    public Renderer targetRenderer;

    [Header("Hover Material")]
    [Tooltip("The material to swap to when hovered (e.g., a gray material).")]
    public Material grayHoverMaterial;

    private Material _originalMaterial;
    private XRSimpleInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();

        // Auto-get renderer on this object or child if not assigned
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        // Save original material safely using sharedMaterial
        if (targetRenderer != null)
            _originalMaterial = new Material(targetRenderer.sharedMaterial);
    }

    private void OnEnable()
    {
        if (_interactable != null)
        {
            _interactable.hoverEntered.AddListener(OnHoverEntered);
            _interactable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDisable()
    {
        if (_interactable != null)
        {
            _interactable.hoverEntered.RemoveListener(OnHoverEntered);
            _interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (targetRenderer != null && grayHoverMaterial != null)
        {
            targetRenderer.material = grayHoverMaterial;
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (targetRenderer != null && _originalMaterial != null)
        {
            targetRenderer.material = _originalMaterial;
        }
    }
}