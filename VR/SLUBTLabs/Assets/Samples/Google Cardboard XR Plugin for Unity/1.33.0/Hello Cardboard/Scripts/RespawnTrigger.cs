using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RespawnTrigger : MonoBehaviour
{
    [Header("Respawn Configuration")]
    [Tooltip("Must match the name of your VR Player root GameObject exactly.")]
    public string xrOriginName = "VR Player";
    public string spawnPointName = "Respawn";
    public float spawnHeightOffset = 0.1f;
    public float respawnCooldown = 1.5f;

    private float _lastRespawnTime = -999f;

    void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.isTrigger = true;

        GameObject spawn = GameObject.Find(spawnPointName);
        if (spawn != null)
            Debug.Log($"[RespawnTrigger] Respawn point found: '{spawnPointName}' at {spawn.transform.position}");
        else
            Debug.LogError($"[RespawnTrigger] Respawn point '{spawnPointName}' NOT found in scene!");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[RespawnTrigger] OnTriggerEnter fired by: '{other.gameObject.name}'");

        if (Time.time - _lastRespawnTime < respawnCooldown)
        {
            Debug.Log($"[RespawnTrigger] Cooldown active — {Time.time - _lastRespawnTime:F2}s since last respawn.");
            return;
        }

        _lastRespawnTime = Time.time;
        ExecuteRespawn();
    }

    private void ExecuteRespawn()
    {
        // Find the VR Player automatically — works across scenes since
        // VR Player persists via DontDestroyOnLoad.
        // FindAnyObjectByType searches all loaded scenes including DontDestroyOnLoad.
        GameObject camRig = GameObject.Find(xrOriginName);

        if (camRig == null)
        {
            Debug.LogError($"[RespawnTrigger] Could not find '{xrOriginName}' in any loaded scene! " +
                           $"Make sure the name matches exactly.");
            return;
        }

        GameObject spawn = GameObject.Find(spawnPointName);

        if (spawn == null)
        {
            Debug.LogError($"[RespawnTrigger] Could not locate '{spawnPointName}' in the scene!");
            return;
        }

        // Compensate for XR head tracking offset on all axes
        Vector3 cameraWorldPos = Camera.main.transform.position;
        Vector3 rigWorldPos = camRig.transform.position;
        Vector3 trackingOffset = cameraWorldPos - rigWorldPos;

        Vector3 spawnTarget = spawn.transform.position + Vector3.up * spawnHeightOffset;
        Vector3 targetRigPosition = spawnTarget - trackingOffset;

        Debug.Log($"[RespawnTrigger] Found rig: '{camRig.name}' | Tracking offset: {trackingOffset}");
        Debug.Log($"[RespawnTrigger] Moving rig to: {targetRigPosition} so camera lands at: {spawnTarget}");

        camRig.transform.position = targetRigPosition;
        camRig.transform.rotation = spawn.transform.rotation;

        if (Vector3.Distance(Camera.main.transform.position, spawnTarget) > 0.05f)
            Debug.LogWarning($"[RespawnTrigger] Camera not at spawn target — distance: {Vector3.Distance(Camera.main.transform.position, spawnTarget):F3}");
        else
            Debug.Log("[RespawnTrigger] Respawn successful!");
    }
}