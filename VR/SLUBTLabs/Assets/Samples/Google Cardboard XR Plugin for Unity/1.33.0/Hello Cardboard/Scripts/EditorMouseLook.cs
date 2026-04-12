using UnityEngine;
using UnityEngine.InputSystem;

public class EditorMouseLook : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    #if UNITY_EDITOR
        // Check if Right Mouse Button is held using the New Input System
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            // Get mouse movement delta
            Vector2 delta = Mouse.current.delta.ReadValue();

            float sensitivity = 0.1f;
            float mouseX = delta.x * sensitivity;
            float mouseY = delta.y * sensitivity;

            // Apply rotation
            transform.localRotation *= Quaternion.Euler(-mouseY, mouseX, 0);

            // Keep the camera upright
            Vector3 euler = transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(euler.x, euler.y, 0);
        }
    #endif
    
}
}
