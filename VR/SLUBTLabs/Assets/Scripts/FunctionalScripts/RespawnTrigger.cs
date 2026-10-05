using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RespawnTrigger : MonoBehaviour
{
    [Header("Respawn Configuration")]
    [Tooltip("Must match the name of your VR Player root GameObject exactly.")]
    public string xrOriginName = "VR Player";
    public string spawnPointName = "Respawn";
    public float spawnHeightOffset = 0.0f;
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
        if (Time.time - _lastRespawnTime < respawnCooldown) return;

        _lastRespawnTime = Time.time;
        ExecuteRespawn();
    }

    private void ExecuteRespawn()
    {
        GameObject camRig = GameObject.Find(xrOriginName);
        if (camRig == null)
        {
            Debug.LogError($"[RespawnTrigger] Could not find '{xrOriginName}' in any loaded scene!");
            return;
        }

        GameObject spawn = GameObject.Find(spawnPointName);
        if (spawn == null)
        {
            Debug.LogError($"[RespawnTrigger] Could not locate '{spawnPointName}' in the scene!");
            return;
        }

        Camera vrCam = Camera.main ?? camRig.GetComponentInChildren<Camera>();
        if (vrCam == null)
        {
            camRig.transform.position = spawn.transform.position;
            camRig.transform.rotation = spawn.transform.rotation;
            return;
        }

        // 1. Rotate rig first so camera yaw matches spawn rotation     
        float currentCamYAngle = vrCam.transform.eulerAngles.y;
        float targetYAngle = spawn.transform.eulerAngles.y;
        float rotationDelta = targetYAngle - currentCamYAngle;
        camRig.transform.Rotate(0f, rotationDelta, 0f, Space.World);

        // 2. Calculate offset post-rotation
        Vector3 cameraWorldPos = vrCam.transform.position;
        Vector3 rigWorldPos = camRig.transform.position;
        Vector3 trackingOffset = cameraWorldPos - rigWorldPos;

        // Keep local Y height offset constant
        Vector3 targetPos = (spawn.transform.position + Vector3.up * spawnHeightOffset) - trackingOffset;

        camRig.transform.position = targetPos;
        Debug.Log("[RespawnTrigger] Respawn executed cleanly with tracking offset compensation.");
    }
}