using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [Header("Movement Settings")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;
    public float rotateSpeed = 45f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.localPosition;
    }

    private void Update()
    {
        // Smooth up-and-down bobbing motion
        float newY = _startPos.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.localPosition = new Vector3(_startPos.x, newY, _startPos.z);

        // Slow continuous rotation around local Y axis
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
    }
}