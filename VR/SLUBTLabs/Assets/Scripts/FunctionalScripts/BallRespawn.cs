using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Puts a grabbable ball back at its starting spot if it falls out of the world
/// or is left lying too far away. Never resets it while someone is holding it.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallRespawn : MonoBehaviour
{
    [Tooltip("Respawn if the ball drops this far below its start height (metres).")]
    public float fallLimit = 5f;
    [Tooltip("Respawn if the ball ends up farther than this from its start (metres).")]
    public float maxDistance = 15f;
    [Tooltip("Return the ball after it has been left alone for this long (0 = never).")]
    public float returnAfterIdleSeconds = 20f;
    [Tooltip("Only return it if it is farther than this from its start (metres).")]
    public float idleReturnMinDistance = 1.5f;

    private Rigidbody _rb;
    private XRGrabInteractable _grab;
    private Vector3 _startPos;
    private Quaternion _startRot;
    private float _idleTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grab = GetComponent<XRGrabInteractable>();
        _startPos = transform.position;
        _startRot = transform.rotation;
    }

    private void Update()
    {
        // Never interfere while the ball is in someone's hand
        if (_grab != null && _grab.isSelected)
        {
            _idleTimer = 0f;
            return;
        }

        Vector3 offset = transform.position - _startPos;

        if (transform.position.y < _startPos.y - fallLimit || offset.magnitude > maxDistance)
        {
            Respawn();
            return;
        }

        if (returnAfterIdleSeconds > 0f && offset.magnitude > idleReturnMinDistance)
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= returnAfterIdleSeconds) Respawn();
        }
        else
        {
            _idleTimer = 0f;
        }
    }

    private void Respawn()
    {
        _idleTimer = 0f;

        if (!_rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector3.zero;
#else
            _rb.velocity = Vector3.zero;
#endif
            _rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(_startPos, _startRot);
    }
}