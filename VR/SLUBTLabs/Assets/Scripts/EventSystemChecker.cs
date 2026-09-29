using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemChecker : MonoBehaviour
{
    private void Awake()
    {
        // Find all EventSystems in the scene
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (eventSystems.Length > 1)
        {
            Debug.Log("[EventSystemChecker] Duplicate EventSystem found. Destroying this one.");
            Destroy(gameObject); // destroys this EventSystem
        }
    }
}