using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class TeleportationActivator : MonoBehaviour
{
    public XRRayInteractor teleportInteractor;
    public XRRayInteractor rayInteractor;
    public InputActionProperty teleportActivatorAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void DisableTeleportRay()
    {
        teleportInteractor.gameObject.SetActive(false);
    }
    void Start()
    {
        teleportInteractor.gameObject.SetActive(false);

        teleportActivatorAction.action.performed += Action_performed;
        rayInteractor.uiHoverEntered.AddListener(x => DisableTeleportRay());
    }

    private void Action_performed(InputAction.CallbackContext obj)
    {
        if (rayInteractor && rayInteractor.IsOverUIGameObject())
        {
            
                return;
         
           
        }
        teleportInteractor.gameObject.SetActive(true);
    }
    // Update is called once per frame
    void Update()
                {
        if (teleportActivatorAction.action.WasReleasedThisFrame())
        {
            teleportInteractor.gameObject.SetActive(false);
        }
                }
        
   


}