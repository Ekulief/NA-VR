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

        if (_cachedXrOrigin == null || _cachedSpawnPoint == null)
        {
            LocateReferences();
        }

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

        Vector3 forwardYOnly = Vector3.ProjectOnPlane(_cachedSpawnPoint.forward, Vector3.up).normalized;
        if (forwardYOnly == Vector3.zero)
            forwardYOnly = Vector3.forward;

        _cachedXrOrigin.MatchOriginUpCameraForward(Vector3.up, forwardYOnly);

        if (vrCam != null)
        {
            float currentEyeHeightOffset = vrCam.transform.position.y - _cachedXrOrigin.transform.position.y;

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