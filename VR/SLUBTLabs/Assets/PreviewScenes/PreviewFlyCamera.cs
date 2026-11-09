using UnityEngine;
using UnityEngine.InputSystem;

public class PreviewFlyCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float fastMultiplier = 2.5f;
    public float smoothTime = 0.1f;

    [Header("Look")]
    public float lookSensitivity = 0.02f; 
    public float maxPitch = 85f;

    [Header("Options")]
    public bool invertY = false;
    public bool lockCursorOnStart = true;

    private float yaw;
    private float pitch;
    private Vector3 currentVelocity;
    private bool cursorLocked = true;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        if (lockCursorOnStart)
        {
            LockCursor(true);
        }
    }

    void Update()
    {
        HandleCursor();
        HandleLook();
        HandleMovement();
    }

    void HandleCursor()
    {
        // Press Escape to unlock cursor, Left Click to lock again
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !cursorLocked)
        {
            LockCursor(true);
        }
    }

    void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleLook()
    {
        if (!cursorLocked || Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * lookSensitivity;

        yaw += mouseDelta.x;
        pitch += invertY ? mouseDelta.y : -mouseDelta.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float speed = moveSpeed;

        // Hold Shift to move faster
        if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
        {
            speed *= fastMultiplier;
        }

        Vector3 input = Vector3.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.z += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.z -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;

        // Q / E for up / down
        if (Keyboard.current.eKey.isPressed) input.y += 1f;
        if (Keyboard.current.qKey.isPressed) input.y -= 1f;

        Vector3 targetVelocity = transform.TransformDirection(input.normalized) * speed;

        // Smooth movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            transform.position + targetVelocity * Time.deltaTime * 10f,
            ref currentVelocity,
            smoothTime
        );
    }
}