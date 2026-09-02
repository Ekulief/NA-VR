using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Shelf Spawner
/// Attach to an invisible cube (Box Collider, no Mesh Renderer) placed on top of your shelf.
/// Resize the Box Collider to cover the shelf surface — items spawn evenly across its top face.
///
/// MULTI-SHELF SETUP:
///   Place this script on each shelf. Leave targetPrefab empty on all shelves.
///   AttentionalBlindnessManager calls InjectTarget() on one randomly chosen shelf
///   to place the unique target item among the distractors.
///
/// SETUP:
///   a) Create an empty GameObject, name it "ShelfSpawner".
///   b) Add a Box Collider. Resize it to match your shelf surface. Check Is Trigger.
///   c) Attach this script.
///   d) Assign distractor prefabs in the Inspector.
///   e) Leave targetPrefab empty.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ShelfSpawner : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int columns = 5;
    public int rows = 2;
    public float randomPositionOffset = 0.04f;
    public float randomRotationOffset = 15f;

    [Header("Items")]
    [Tooltip("Drag all your distractor prefabs here.")]
    public List<GameObject> distractorPrefabs = new();

    [Tooltip("Optional — leave empty. Set via InjectTarget() from AttentionalBlindnessManager.")]
    public GameObject targetPrefab;

    [Header("Experiment")]
    public bool logTargetPosition = true;

    // ── Internal state ────────────────────────────────────────────────────────
    private List<GameObject> _spawnedItems = new();
    private int _targetSlotIndex = -1;
    private BoxCollider _bounds;
    private GameObject _itemHolder;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _bounds = GetComponent<BoxCollider>();
        _bounds.isTrigger = true;

        // All spawned items are parented under this holder so they get
        // destroyed automatically when the scene unloads
        _itemHolder = new GameObject($"{gameObject.name}_Items");

        SpawnShelf();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void RespawnShelf()
    {
        ClearShelf();
        SpawnShelf();
    }

    public bool HasTarget() => targetPrefab != null;

    public Vector3 GetTargetPosition()
    {
        if (_targetSlotIndex >= 0 && _targetSlotIndex < _spawnedItems.Count)
            return _spawnedItems[_targetSlotIndex].transform.position;
        return Vector3.zero;
    }

    /// <summary>
    /// Injects a unique target prefab into a random slot on this shelf.
    /// Destroys whatever distractor was in that slot and replaces it with the target.
    /// Returns the spawned target instance — AttentionalBlindnessManager holds this reference.
    /// </summary>
    public GameObject InjectTarget(GameObject targetPrefabToInject)
    {
        if (_spawnedItems.Count == 0)
        {
            Debug.LogWarning($"[ShelfSpawner] '{gameObject.name}' has no spawned items to inject into.");
            return null;
        }

        // Pick a random slot to replace with the target
        int slot = Random.Range(0, _spawnedItems.Count);

        // Destroy the distractor that was there
        if (_spawnedItems[slot] != null)
            Destroy(_spawnedItems[slot]);

        // Spawn target at the same position parented under the holder
        Vector3 position = GetSlotPosition(slot);
        Quaternion rotation = Quaternion.Euler(
            0f,
            Random.Range(-randomRotationOffset, randomRotationOffset),
            0f
        );

        GameObject target = Instantiate(targetPrefabToInject, position, rotation, _itemHolder.transform);
        target.tag = "SpawnedItem";
        _spawnedItems[slot] = target;
        _targetSlotIndex = slot;

        if (logTargetPosition)
            Debug.Log($"[ShelfSpawner] Target '{targetPrefabToInject.name}' injected at slot {slot} " +
                      $"on '{gameObject.name}' — world pos: {position}");

        return target;
    }

    /// <summary>Returns all currently spawned items on this shelf.</summary>
    public List<GameObject> GetSpawnedItems() => _spawnedItems;

    // ── Spawning ──────────────────────────────────────────────────────────────
    private void SpawnShelf()
    {
        if (distractorPrefabs == null || distractorPrefabs.Count == 0)
        {
            Debug.LogError($"[ShelfSpawner] '{gameObject.name}' has no distractor prefabs assigned!");
            return;
        }

        Bounds bounds = _bounds.bounds;
        float topY = bounds.max.y;

        float spacingX = bounds.size.x / Mathf.Max(columns, 1);
        float spacingZ = bounds.size.z / Mathf.Max(rows, 1);
        float startX = bounds.min.x + spacingX * 0.5f;
        float startZ = bounds.min.z + spacingZ * 0.5f;

        int totalSlots = columns * rows;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 basePosition = new Vector3(
                    startX + col * spacingX,
                    topY,
                    startZ + row * spacingZ
                );

                Vector3 randomOffset = new Vector3(
                    Random.Range(-randomPositionOffset, randomPositionOffset),
                    0f,
                    Random.Range(-randomPositionOffset, randomPositionOffset)
                );

                Quaternion spawnRotation = Quaternion.Euler(
                    0f,
                    Random.Range(-randomRotationOffset, randomRotationOffset),
                    0f
                );

                GameObject prefab = distractorPrefabs[Random.Range(0, distractorPrefabs.Count)];

                // Parent under _itemHolder so items get destroyed with the scene
                GameObject spawned = Instantiate(prefab, basePosition + randomOffset, spawnRotation, _itemHolder.transform);
                spawned.tag = "SpawnedItem";
                _spawnedItems.Add(spawned);
            }
        }

        if (logTargetPosition)
            Debug.Log($"[ShelfSpawner] '{gameObject.name}' spawned {totalSlots} distractor items.");
    }

    private Vector3 GetSlotPosition(int slot)
    {
        Bounds bounds = _bounds.bounds;
        float topY = bounds.max.y;

        float spacingX = bounds.size.x / Mathf.Max(columns, 1);
        float spacingZ = bounds.size.z / Mathf.Max(rows, 1);
        float startX = bounds.min.x + spacingX * 0.5f;
        float startZ = bounds.min.z + spacingZ * 0.5f;

        int col = slot % columns;
        int row = slot / columns;

        return new Vector3(
            startX + col * spacingX + Random.Range(-randomPositionOffset, randomPositionOffset),
            topY,
            startZ + row * spacingZ + Random.Range(-randomPositionOffset, randomPositionOffset)
        );
    }

    private void ClearShelf()
    {
        foreach (GameObject item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();
        _targetSlotIndex = -1;
    }

    // ── Editor helper ─────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Bounds bounds = box.bounds;
        float topY = bounds.max.y;

        float spacingX = bounds.size.x / Mathf.Max(columns, 1);
        float spacingZ = bounds.size.z / Mathf.Max(rows, 1);
        float startX = bounds.min.x + spacingX * 0.5f;
        float startZ = bounds.min.z + spacingZ * 0.5f;

        Gizmos.color = Color.cyan;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * spacingX,
                    topY,
                    startZ + row * spacingZ
                );
                Gizmos.DrawWireCube(pos, new Vector3(spacingX * 0.8f, 0.02f, spacingZ * 0.8f));
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}