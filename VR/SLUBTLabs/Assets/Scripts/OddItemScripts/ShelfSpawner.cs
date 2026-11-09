using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Shelf Spawner
/// Attach to an invisible cube (Box Collider, no Mesh Renderer) placed on top of your shelf.
/// Resize the Box Collider to cover the shelf surface — items spawn evenly across its top face.
///
/// Items are now placed using their real size:
///   - their base always sits on the shelf (works whatever the prefab pivot is)
///   - random offset is limited to the free space inside the item's own cell,
///     so neighbouring items can never overlap
///   - physics is frozen on spawned items so they cannot be pushed or tilted
///
/// MULTI-SHELF SETUP:
///   Place this script on each shelf. Leave targetPrefab empty on all shelves.
///   OddItemManager calls InjectTarget() on one randomly chosen shelf
///   to place the unique odd item among the distractors.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ShelfSpawner : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Optional — assign your ExperimentConfig asset to override grid settings from web app.")]
    public ExperimentConfig config;
    [Header("Grid Configuration")]
    [Tooltip("If true, Columns/Rows come from ExperimentConfig. If false, use the values set in the Inspector.")]
    public bool useConfigGrid = true;
    public int columns = 2;
    public int rows = 2;
    public float randomPositionOffset = 0.04f;
    public float randomRotationOffset = 15f;

    [Header("Placement")]
    [Tooltip("Freeze Rigidbodies on spawned items so overlapping colliders can't shove or tilt them.")]
    public bool freezePhysics = true;
    [Tooltip("Tiny gap between the item base and the shelf surface (metres).")]
    public float surfaceOffset = 0.003f;
    [Tooltip("Log a warning when an item is larger than its grid cell.")]
    public bool warnWhenItemTooBig = true;

    [Header("Items")]
    [Tooltip("Drag all your distractor prefabs here.")]
    public List<GameObject> distractorPrefabs = new();

    [Tooltip("Optional — leave empty. Set via InjectTarget() from OddItemManager.")]
    public GameObject targetPrefab;

    [Header("Experiment")]
    public bool logTargetPosition = true;

    // ── Internal state ────────────────────────────────────────────────────────
    private List<GameObject> _spawnedItems = new();
    private int _targetSlotIndex = -1;
    private BoxCollider _bounds;
    private GameObject _itemHolder;
    private readonly HashSet<string> _warnedPrefabs = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _bounds = GetComponent<BoxCollider>();
        _bounds.isTrigger = true;

        // Apply config overrides only when useConfigGrid is enabled
        ExperimentConfig cfg = config;
        if (cfg != null && useConfigGrid)
        {
            columns = cfg.shelf_Columns;
            rows = cfg.shelf_Rows;
            randomPositionOffset = cfg.shelf_RandomPositionOffset;
            randomRotationOffset = cfg.shelf_RandomRotationOffset;
        }

        // All spawned items are parented under this holder so they get
        // destroyed automatically when the scene unloads
        _itemHolder = new GameObject($"{gameObject.name}_Items");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
            _itemHolder, gameObject.scene);

        SpawnShelf();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void RespawnShelf()
    {
        ClearShelf();
        SpawnShelf();
    }

    public bool HasTarget() => targetPrefab != null;

    /// <summary>The odd item on this shelf, or null if the target is on another shelf.</summary>
    public GameObject GetTargetInstance()
    {
        if (_targetSlotIndex >= 0 && _targetSlotIndex < _spawnedItems.Count)
            return _spawnedItems[_targetSlotIndex];
        return null;
    }

    public Vector3 GetTargetPosition()
    {
        if (_targetSlotIndex >= 0 && _targetSlotIndex < _spawnedItems.Count)
            return _spawnedItems[_targetSlotIndex].transform.position;
        return Vector3.zero;
    }

    /// <summary>
    /// Injects a unique target prefab into a slot on this shelf.
    /// If a viewer is given, the slot is chosen from the row/column nearest the viewer
    /// so the target is not hidden behind another item.
    /// Destroys whatever distractor was in that slot and replaces it with the target.
    /// </summary>
    public GameObject InjectTarget(GameObject targetPrefabToInject, Transform viewer = null)
    {
        if (_spawnedItems.Count == 0)
        {
            Debug.LogWarning($"[ShelfSpawner] '{gameObject.name}' has no spawned items to inject into.");
            return null;
        }

        int slot = PickVisibleSlot(viewer);

        if (_spawnedItems[slot] != null)
            Destroy(_spawnedItems[slot]);

        GameObject target = Instantiate(targetPrefabToInject, _itemHolder.transform);
        target.tag = "SpawnedItem";
        PlaceInSlot(target, slot);

        _spawnedItems[slot] = target;
        _targetSlotIndex = slot;

        if (logTargetPosition)
            Debug.Log($"[ShelfSpawner] Target '{targetPrefabToInject.name}' injected at slot {slot} " +
                      $"on '{gameObject.name}' — world pos: {target.transform.position}");

        return target;
    }

    /// <summary>Returns all currently spawned items on this shelf.</summary>
    public List<GameObject> GetSpawnedItems() => _spawnedItems;

    /// <summary>
    /// Reconstructs a saved shelf layout from a LayoutManager snapshot.
    /// Called by LayoutManager during replay mode.
    /// Each item is placed at its exact saved position and rotation.
    /// </summary>
    public void ApplySnapshot(LayoutManager.ShelfSnapshot snapshot)
    {
        ClearShelf();

        foreach (LayoutManager.ItemSnapshot itemSnap in snapshot.items)
        {
            GameObject prefab = null;

            if (itemSnap.isOddItem)
            {
                OddItemManager mgr = FindAnyObjectByType<OddItemManager>();
                if (mgr != null)
                    prefab = mgr.oddItemPrefabs.Find(p =>
                        p != null && p.name == itemSnap.prefabName);
            }
            else
            {
                prefab = distractorPrefabs.Find(p => p != null && p.name == itemSnap.prefabName);

                // Shelf item lists are shuffled per session, so the saved prefab may now
                // belong to another shelf's list. Look everywhere before giving up.
                if (prefab == null)
                    prefab = FindPrefabOnAnyShelf(itemSnap.prefabName);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[ShelfSpawner] Replay: could not find prefab '{itemSnap.prefabName}' " +
                                 $"on shelf '{gameObject.name}'. Skipping slot {itemSnap.slot}.");
                _spawnedItems.Add(null);
                continue;
            }

            Vector3 pos = itemSnap.position.ToVector3();
            Quaternion rot = Quaternion.Euler(0f, itemSnap.rotation, 0f);

            GameObject spawned = Instantiate(prefab, pos, rot, _itemHolder.transform);
            spawned.tag = "SpawnedItem";
            FreezePhysics(spawned);
            _spawnedItems.Add(spawned);

            if (itemSnap.isOddItem)
                _targetSlotIndex = itemSnap.slot;
        }

        Debug.Log($"[ShelfSpawner] Snapshot applied to '{gameObject.name}' — " +
                  $"{_spawnedItems.Count} items reconstructed.");
    }

    private static GameObject FindPrefabOnAnyShelf(string prefabName)
    {
        foreach (ShelfSpawner shelf in FindObjectsByType<ShelfSpawner>(FindObjectsSortMode.None))
        {
            GameObject match = shelf.distractorPrefabs.Find(p => p != null && p.name == prefabName);
            if (match != null) return match;
        }
        return null;
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnShelf()
    {
        if (distractorPrefabs == null || distractorPrefabs.Count == 0)
        {
            Debug.LogError($"[ShelfSpawner] '{gameObject.name}' has no distractor prefabs assigned!");
            return;
        }

        int totalSlots = Mathf.Max(columns, 1) * Mathf.Max(rows, 1);

        for (int slot = 0; slot < totalSlots; slot++)
        {
            GameObject prefab = distractorPrefabs[Random.Range(0, distractorPrefabs.Count)];

            GameObject spawned = Instantiate(prefab, _itemHolder.transform);
            spawned.tag = "SpawnedItem";
            PlaceInSlot(spawned, slot);
            _spawnedItems.Add(spawned);
        }

        if (logTargetPosition)
            Debug.Log($"[ShelfSpawner] '{gameObject.name}' spawned {totalSlots} distractor items.");
    }

    /// <summary>
    /// Places an item inside its grid cell using its real size, so it sits on the
    /// shelf and never pokes into a neighbouring cell.
    /// </summary>
    private void PlaceInSlot(GameObject item, int slot)
    {
        Bounds area = _bounds.bounds;
        float cellX = area.size.x / Mathf.Max(columns, 1);
        float cellZ = area.size.z / Mathf.Max(rows, 1);
        Vector3 cell = GetSlotBasePosition(slot);

        FreezePhysics(item);

        // 1. Random yaw only (never tilted)
        float yaw = Random.Range(-randomRotationOffset, randomRotationOffset);
        item.transform.SetPositionAndRotation(cell, Quaternion.Euler(0f, yaw, 0f));

        Bounds b = GetItemBounds(item);

        // 2. If the rotated footprint doesn't fit the cell, try without rotation
        if (b.size.x > cellX || b.size.z > cellZ)
        {
            item.transform.rotation = Quaternion.identity;
            b = GetItemBounds(item);

            if (warnWhenItemTooBig && (b.size.x > cellX || b.size.z > cellZ) &&
                _warnedPrefabs.Add(item.name))
            {
                Debug.LogWarning(
                    $"[ShelfSpawner] '{gameObject.name}': item '{item.name}' " +
                    $"({b.size.x:F2} x {b.size.z:F2} m) is larger than a grid cell " +
                    $"({cellX:F2} x {cellZ:F2} m). It will overlap neighbours — " +
                    "use fewer columns/rows or a larger shelf area.");
            }
        }

        // 3. Base on the shelf surface, centred in the cell (works for any pivot)
        Vector3 delta = new Vector3(
            cell.x - b.center.x,
            (area.max.y + surfaceOffset) - b.min.y,
            cell.z - b.center.z);
        item.transform.position += delta;

        // 4. Random shift limited to the free space inside the cell
        float freeX = Mathf.Max(0f, (cellX - b.size.x) * 0.5f);
        float freeZ = Mathf.Max(0f, (cellZ - b.size.z) * 0.5f);
        float jx = Mathf.Min(randomPositionOffset, freeX);
        float jz = Mathf.Min(randomPositionOffset, freeZ);

        item.transform.position += new Vector3(
            Random.Range(-jx, jx),
            0f,
            Random.Range(-jz, jz));
    }

    private void FreezePhysics(GameObject item)
    {
        if (!freezePhysics) return;

        foreach (Rigidbody rb in item.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
        }
    }

    private static Bounds GetItemBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds b = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                b.Encapsulate(colliders[i].bounds);
            return b;
        }

        return new Bounds(go.transform.position, Vector3.one * 0.1f);
    }

    // ── Slot geometry ─────────────────────────────────────────────────────────

    // Centre of a grid cell at shelf-top height (no random offset)
    private Vector3 GetSlotBasePosition(int slot)
    {
        Bounds b = _bounds.bounds;
        float spacingX = b.size.x / Mathf.Max(columns, 1);
        float spacingZ = b.size.z / Mathf.Max(rows, 1);

        int col = slot % Mathf.Max(columns, 1);
        int row = slot / Mathf.Max(columns, 1);

        return new Vector3(
            b.min.x + spacingX * 0.5f + col * spacingX,
            b.max.y,
            b.min.z + spacingZ * 0.5f + row * spacingZ);
    }

    /// <summary>
    /// Chooses a random slot from the row/column nearest the viewer so the target
    /// is never directly behind another item. Falls back to any slot if no viewer is given.
    /// </summary>
    private int PickVisibleSlot(Transform viewer)
    {
        int total = _spawnedItems.Count;
        if (viewer == null) return Random.Range(0, total);

        Vector3 center = _bounds.bounds.center;
        Vector3 toViewer = viewer.position - center;
        toViewer.y = 0f;
        if (toViewer.sqrMagnitude < 0.0001f) return Random.Range(0, total);
        toViewer.Normalize();

        var scored = new List<(int slot, float score)>();
        for (int s = 0; s < total; s++)
        {
            Vector3 offset = GetSlotBasePosition(s) - center;
            offset.y = 0f;
            scored.Add((s, Vector3.Dot(offset, toViewer)));
        }
        scored.Sort((a, b) => b.score.CompareTo(a.score));

        int frontCount = Mathf.Abs(toViewer.z) >= Mathf.Abs(toViewer.x) ? columns : rows;
        frontCount = Mathf.Clamp(frontCount, 1, total);

        return scored[Random.Range(0, frontCount)].slot;
    }

    private void ClearShelf()
    {
        foreach (GameObject item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();
        _targetSlotIndex = -1;
    }

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