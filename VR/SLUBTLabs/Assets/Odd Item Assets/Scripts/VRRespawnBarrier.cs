using UnityEngine;
using Unity.XR.CoreUtils;

public class VRRespawnBarrier : MonoBehaviour
{
    [Header("Auto-Search Configuration")]
    [Tooltip("Fallback name to search if XROrigin component isn't found automatically.")]
    public string xrOriginName = "VR Player";

    [Tooltip("Name of the spawn point object in the scene.")]
    public string spawnPointName = "Respawn";

    [Header("Cooldown Settings")]
    public float respawnCooldown = 1.5f;

    private float _lastRespawnTime = -999f;
    private XROrigin _cachedXrOrigin;
    private Transform _cachedSpawnPoint;

    private void Start()
    {
        LocateReferences();
    }

    private void LocateReferences()
    {
        // 1. Find XROrigin by component, fall back to string search
        if (_cachedXrOrigin == null)
        {
            _cachedXrOrigin = FindFirstObjectByType<XROrigin>();
            if (_cachedXrOrigin == null)
            {
                GameObject camRig = GameObject.Find(xrOriginName);
                if (camRig != null)
                    _cachedXrOrigin = camRig.GetComponent<XROrigin>();
            }
        }

        // 2. Find Spawn Point by name search
        if (_cachedSpawnPoint == null)
        {
            GameObject spawnObj = GameObject.Find(spawnPointName);
            if (spawnObj != null)
                _cachedSpawnPoint = spawnObj.transform;
            else
                Debug.LogError($"[VRRespawnBarrier] Could not locate spawn point '{spawnPointName}' in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - _lastRespawnTime < respawnCooldown) return;

        // Auto-detect references if not already cached
        if (_cachedXrOrigin == null || _cachedSpawnPoint == null)
        {
            LocateReferences();
        }

        // Check if the collider belongs to MainCamera or the XR Rig
        if (other.CompareTag("MainCamera") || other.GetComponentInParent<XROrigin>() != null)
        {
            _lastRespawnTime = Time.time;
            TeleportPlayerToSpawn();
        }
    }

    public void TeleportPlayerToSpawn()
    {
        if (_cachedXrOrigin == null || _cachedSpawnPoint == null)
        {
            Debug.LogWarning("[VRRespawnBarrier] Missing XROrigin or RespawnPoint reference!");
            return;
        }

        Camera vrCam = _cachedXrOrigin.Camera ?? Camera.main;

        // 1. Flatten the target forward direction to avoid horizon tilt
        Vector3 forwardYOnly = Vector3.ProjectOnPlane(_cachedSpawnPoint.forward, Vector3.up).normalized;
        if (forwardYOnly == Vector3.zero)
            forwardYOnly = Vector3.forward;

        // 2. Match view rotation on the horizontal axis
        _cachedXrOrigin.MatchOriginUpCameraForward(Vector3.up, forwardYOnly);

        // 3. Keep the user's physical standing height relative to the spawn floor
        if (vrCam != null)
        {
            // Calculate height offset between camera eyes and XR Origin base floor
            float currentEyeHeightOffset = vrCam.transform.position.y - _cachedXrOrigin.transform.position.y;

            // Set camera location to spawn ground level + current physical eye height
            Vector3 targetCameraPosition = _cachedSpawnPoint.position + (Vector3.up * currentEyeHeightOffset);
            _cachedXrOrigin.MoveCameraToWorldLocation(targetCameraPosition);
        }
        else
        {
            _cachedXrOrigin.MoveCameraToWorldLocation(_cachedSpawnPoint.position);
        }

        Debug.Log("[VRRespawnBarrier] Respawn executed while maintaining physical height and horizontal orientation.");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}