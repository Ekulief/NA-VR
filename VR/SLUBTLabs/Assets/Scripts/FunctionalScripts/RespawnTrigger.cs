using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RespawnTrigger : MonoBehaviour
{
    [Header("Respawn Configuration")]
    [Tooltip("Must match the name of your VR Player root GameObject exactly.")]
    public string xrOriginName = "VR Player";
    public string spawnPointName = "Respawn";
    [Tooltip("Only used when no floor is found under the spawn point (old behaviour: raises the HEAD above the spawn point).")]
    public float spawnHeightOffset = 0.0f;
    public float respawnCooldown = 1.5f;

    [Header("Floor Snapping")]
    [Tooltip("Put the rig ON the floor under the spawn point instead of putting the player's head at the spawn point's height.")]
    public bool snapToFloor = true;
    [Tooltip("How far below the spawn point to look for the floor.")]
    public float floorSearchDistance = 4f;

    private float _lastRespawnTime = -999f;

    void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.isTrigger = true;

        Transform spawn = FindSpawn();
        if (spawn != null)
            Debug.Log($"[RespawnTrigger] Respawn point found: '{spawnPointName}' at {spawn.position}");
        else
            Debug.LogError($"[RespawnTrigger] Respawn point '{spawnPointName}' NOT found in scene!");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the player should trigger a respawn (falling furniture used to trigger it too).
        if (!IsPlayer(other)) return;

        if (Time.time - _lastRespawnTime < respawnCooldown) return;

        _lastRespawnTime = Time.time;
        ExecuteRespawn();
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player")) return true;

        GameObject rig = GameObject.Find(xrOriginName);
        return rig != null && other.transform.IsChildOf(rig.transform);
    }

    // Prefer a spawn point in the same scene as this trigger, so a same-named object in another
    // loaded scene (e.g. the hub) can't be picked by mistake.
    private Transform FindSpawn()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name == spawnPointName) return root.transform;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == spawnPointName) return t;
        }

        GameObject any = GameObject.Find(spawnPointName);
        return any != null ? any.transform : null;
    }

    private void ExecuteRespawn()
    {
        GameObject camRig = GameObject.Find(xrOriginName);
        if (camRig == null)
        {
            Debug.LogError($"[RespawnTrigger] Could not find '{xrOriginName}' in any loaded scene!");
            return;
        }

        Transform spawn = FindSpawn();
        if (spawn == null)
        {
            Debug.LogError($"[RespawnTrigger] Could not locate '{spawnPointName}' in the scene!");
            return;
        }

        // Same pattern as MemoryExperimentManager.TeleportPlayer: freeze physics, move, wait a
        // physics step, restore. Without this the falling velocity carried over and the player
        // tunnelled straight back through the floor after respawning.
        CharacterController cc = camRig.GetComponentInChildren<CharacterController>();
        Rigidbody rb = camRig.GetComponentInChildren<Rigidbody>();

        bool controllerWasEnabled = cc != null && cc.enabled;
        bool wasKinematic = rb != null && rb.isKinematic;
        bool usedGravity = rb != null && rb.useGravity;

        if (cc != null) cc.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Camera vrCam = Camera.main;
        if (vrCam == null) vrCam = camRig.GetComponentInChildren<Camera>();

        if (vrCam == null)
        {
            camRig.transform.position = spawn.position;
            camRig.transform.rotation = spawn.rotation;
        }
        else
        {
            // 1. Rotate rig first so camera yaw matches spawn rotation
            float rotationDelta = spawn.eulerAngles.y - vrCam.transform.eulerAngles.y;
            camRig.transform.Rotate(0f, rotationDelta, 0f, Space.World);

            // 2. Horizontal tracking offset after rotation
            Vector3 trackingOffset = vrCam.transform.position - camRig.transform.position;

            Vector3 targetPos;
            if (snapToFloor && TryFindFloorY(spawn.position, camRig.transform, out float floorY))
            {
                // Rig origin ON the floor; the headset adds the player's real height on top.
                targetPos = new Vector3(
                    spawn.position.x - trackingOffset.x,
                    floorY,
                    spawn.position.z - trackingOffset.z);
            }
            else
            {
                // Fallback: old behaviour (head at spawn point + offset)
                targetPos = (spawn.position + Vector3.up * spawnHeightOffset) - trackingOffset;
            }

            camRig.transform.position = targetPos;
        }

        Debug.Log($"[RespawnTrigger] Respawned at '{spawn.name}'. Spawn={spawn.position}, " +
                  $"Origin={camRig.transform.position}, Camera={(vrCam != null ? vrCam.transform.position.ToString() : "none")}");

        StartCoroutine(RestorePlayer(cc, controllerWasEnabled, rb, wasKinematic, usedGravity));
    }

    private bool TryFindFloorY(Vector3 from, Transform rig, out float floorY)
    {
        floorY = from.y;

        RaycastHit[] hits = Physics.RaycastAll(from + Vector3.up * 0.5f, Vector3.down,
            floorSearchDistance + 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(rig)) continue;
            floorY = hit.point.y;
            return true;
        }

        Debug.LogWarning("[RespawnTrigger] No floor collider found under the spawn point; using the spawn point's height.");
        return false;
    }

    private IEnumerator RestorePlayer(CharacterController cc, bool controllerWasEnabled,
                                      Rigidbody rb, bool wasKinematic, bool usedGravity)
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        if (cc != null) cc.enabled = controllerWasEnabled;

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = usedGravity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}