using UnityEngine;

public class RoomBoundary : MonoBehaviour
{
    [Header("Room Settings")]
    public string roomName = "kitchen";

    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[RoomBoundary] Player entered: {roomName}");
            ExperimentManager.Instance.OnPlayerEnterRoom(roomName);
            // ^ We'll uncomment this in Phase 2 when ExperimentManager exists
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[RoomBoundary] Player exited: {roomName}");
            ExperimentManager.Instance.OnPlayerExitRoom(roomName);
        }
    }

    // Shows the boundary as a green box in Scene view
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}