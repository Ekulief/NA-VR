using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Shelf Spawner
/// Attach to an invisible cube (Box Collider, no Mesh Renderer) placed on top of your shelf.
/// Resize the Box Collider to cover the shelf surface — items spawn evenly across its top face.
///
/// MULTI-SHELF SETUP:
///   Place this script on each shelf. Leave targetPrefab empty on all shelves.
///   From a separate ShelfManager script, call SetTarget(prefab) on whichever
///   shelf you want to hide the target in, then call RespawnShelf() to regenerate.
///
/// SETUP:
///   a) Create an empty GameObject, name it "ShelfSpawner".
///   b) Add a Box Collider. Resize it to match your shelf surface. Check Is Trigger.
///   c) Attach this script.
///   d) Assign distractor prefabs in the Inspector.
///   e) Leave targetPrefab empty — assign it later via SetTarget() from your manager.
///   f) Press Play — shelves fill with distractors only until a target is assigned.
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

    [Tooltip("Optional — leave empty for distractor-only shelves. " +
             "Assign via SetTarget() from your ShelfManager script.")]
    public GameObject targetPrefab;

    [Header("Experiment")]
    public bool logTargetPosition = true;

    // ── Internal state ────────────────────────────────────────────────────────
    private List<GameObject> _spawnedItems = new();
    private int _targetSlotIndex = -1;
    private BoxCollider _bounds;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _bounds = GetComponent<BoxCollider>();
        _bounds.isTrigger = true;
        SpawnShelf();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Assign a target prefab to this shelf from an external manager.</summary>
    public void SetTarget(GameObject prefab)
    {
        targetPrefab = prefab;
    }

    /// <summary>Clear target from this shelf — makes it distractor-only again.</summary>
    public void ClearTarget()
    {
        targetPrefab = null;
    }

    /// <summary>Clears and regenerates the shelf with current settings.</summary>
    public void RespawnShelf()
    {
        ClearShelf();
        SpawnShelf();
    }

    /// <summary>Returns true if this shelf has a target assigned.</summary>
    public bool HasTarget() => targetPrefab != null;

    /// <summary>Returns the world position of the target item. Vector3.zero if no target.</summary>
    public Vector3 GetTargetPosition()
    {
        if (_targetSlotIndex >= 0 && _targetSlotIndex < _spawnedItems.Count)
            return _spawnedItems[_targetSlotIndex].transform.position;
        return Vector3.zero;
    }

    // ── Spawning ──────────────────────────────────────────────────────────────
    private void SpawnShelf()
    {
        if (distractorPrefabs == null || distractorPrefabs.Count == 0)
        {
            Debug.LogError("[ShelfSpawner] No distractor prefabs assigned!");
            return;
        }

        Bounds bounds = _bounds.bounds;
        float topY = bounds.max.y;

        float spacingX = bounds.size.x / Mathf.Max(columns, 1);
        float spacingZ = bounds.size.z / Mathf.Max(rows, 1);
        float startX = bounds.min.x + spacingX * 0.5f;
        float startZ = bounds.min.z + spacingZ * 0.5f;

        int totalSlots = columns * rows;

        // Only reserve a target slot if a target prefab is assigned
        _targetSlotIndex = targetPrefab != null ? Random.Range(0, totalSlots) : -1;

        int slotIndex = 0;

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

                GameObject prefabToSpawn = (slotIndex == _targetSlotIndex && targetPrefab != null)
                    ? targetPrefab
                    : distractorPrefabs[Random.Range(0, distractorPrefabs.Count)];

                GameObject spawned = Instantiate(prefabToSpawn, basePosition + randomOffset, spawnRotation);
                _spawnedItems.Add(spawned);

                slotIndex++;
            };
        }

        if (logTargetPosition)
        {
            if (targetPrefab != null)
                Debug.Log($"[ShelfSpawner] '{gameObject.name}' — {totalSlots} items spawned. " +
                          $"Target '{targetPrefab.name}' at slot {_targetSlotIndex} " +
                          $"— world pos: {_spawnedItems[_targetSlotIndex].transform.position}");
            else
                Debug.Log($"[ShelfSpawner] '{gameObject.name}' — {totalSlots} distractor items spawned. No target assigned.");
        }
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