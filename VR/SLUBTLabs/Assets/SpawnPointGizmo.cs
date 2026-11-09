using UnityEngine;

public class SpawnPointGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.cyan;
    public float radius = 0.3f;
    public string label = "Spawn Point";

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Draw sphere at ground level
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw forward direction arrow
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward * 0.8f
        );

        // Draw small sphere at arrow tip
        Gizmos.DrawSphere(
            transform.position + transform.forward * 0.8f,
            0.08f
        );

#if UNITY_EDITOR
        // Draw label
        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.4f,
            label
        );
#endif
    }
}