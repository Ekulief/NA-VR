using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Layout Manager
/// Captures the shelf layout after spawning, saves it with session results,
/// and can replay a saved layout so multiple participants see the exact same setup.
///
/// SETUP:
///   a) Attach to the same GameObject as OddItemManager.
///   b) Assign all ShelfSpawner references in the Inspector.
///   c) OddItemManager calls CaptureLayout() and SaveSession() — no other wiring needed.
///
/// USAGE:
///   Normal mode  → layout is random each session, saved for reference.
///   Replay mode  → tick useReplayLayout, assign a saved JSON file path,
///                  and the exact same layout is reconstructed for each participant.
///
/// SAVE LOCATION:
///   Application.persistentDataPath/layouts/
///   Windows editor: %AppData%\..\LocalLow\[Company]\[Product]\layouts\
/// </summary>
public class LayoutManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static LayoutManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Shelf References")]
    [Tooltip("Drag all ShelfSpawner GameObjects here.")]
    public List<ShelfSpawner> shelves = new();

    [Tooltip("Max saved sessions to keep. 0 = keep all.")]
    public int maxSavedSessions = 50;

    [Header("Replay Mode")]
    [Tooltip("If true, loads a saved layout instead of generating a random one. " +
             "All participants will see the exact same shelf configuration.")]
    public bool useReplayLayout = false;

    [Tooltip("Full path to the layout JSON file to replay. " +
             "Leave empty to pick the most recent saved layout automatically.")]
    public string replayFilePath = "";

    [Header("Config")]
    public ExperimentConfig config;

    // ── Internal ──────────────────────────────────────────────────────────────
    private LayoutSnapshot _currentLayout;
    private string _saveDir;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        _saveDir = Path.Combine(Application.persistentDataPath, "layouts");
        Directory.CreateDirectory(_saveDir);

        if (useReplayLayout)
            StartCoroutine(LoadAndApplyReplayLayout());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from OddItemManager after the target is injected and items are spawned.
    /// Captures the full shelf layout into memory.
    /// </summary>
    public void CaptureLayout(GameObject targetInstance, GameObject chosenOddItemPrefab)
    {
        _currentLayout = new LayoutSnapshot
        {
            capturedAtUtc = DateTime.UtcNow.ToString("o"),
            chosenOddItem = chosenOddItemPrefab.name,
            targetPosition = Vec3(targetInstance.transform.position),
            participantId = config != null ? config.participantId : "unknown",
            sessionId = $"layout_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
            shelves = new List<ShelfSnapshot>()
        };

        // Capture each shelf's items
        foreach (ShelfSpawner shelf in shelves)
        {
            if (shelf == null) continue;

            var shelfSnap = new ShelfSnapshot
            {
                shelfName = shelf.gameObject.name,
                items = new List<ItemSnapshot>()
            };

            List<GameObject> spawnedItems = shelf.GetSpawnedItems();
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                GameObject item = spawnedItems[i];
                if (item == null) continue;

                shelfSnap.items.Add(new ItemSnapshot
                {
                    slot = i,
                    prefabName = item.name.Replace("(Clone)", "").Trim(),
                    isOddItem = item == targetInstance,
                    position = Vec3(item.transform.position),
                    rotation = item.transform.eulerAngles.y
                });
            }

            _currentLayout.shelves.Add(shelfSnap);
        }

        Debug.Log($"[LayoutManager] Layout captured — {shelves.Count} shelves, " +
                  $"odd item: '{chosenOddItemPrefab.name}'");
    }

    /// <summary>
    /// Call this from OddItemManager when the participant finds the odd item.
    /// Adds the result to the layout snapshot and saves everything to JSON.
    /// Returns the file path that was written.
    /// </summary>
    public string SaveSession(float searchTimeSeconds, bool foundItem = true)
    {
        if (_currentLayout == null)
        {
            Debug.LogWarning("[LayoutManager] SaveSession called but no layout was captured.");
            return null;
        }

        _currentLayout.participantId = config != null ? config.participantId : "unknown";
        _currentLayout.searchTimeSeconds = searchTimeSeconds;
        _currentLayout.itemFound = foundItem;
        _currentLayout.completedAtUtc = DateTime.UtcNow.ToString("o");

        string fileName = $"session_{_currentLayout.participantId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(_saveDir, fileName);
        string json = JsonUtility.ToJson(_currentLayout, prettyPrint: true);

        if (maxSavedSessions > 0)
        {
            var all = GetAllSavedSessionPaths();
            while (all.Count > maxSavedSessions)
            {
                File.Delete(all[0]);
                all.RemoveAt(0);
            }
        }
        File.WriteAllText(path, json);
        Debug.Log($"[LayoutManager] Session saved → {path}");
        return path;

    }

    /// <summary>
    /// Returns the layout snapshot currently in memory.
    /// Useful for the web dashboard to read the layout after a session.
    /// </summary>
    public LayoutSnapshot GetCurrentLayout() => _currentLayout;

    /// <summary>
    /// Returns JSON of all saved layout files in the layouts directory.
    /// Call this from a web dashboard integration to list past sessions.
    /// </summary>
    public List<string> GetAllSavedSessionPaths()
    {
        var paths = new List<string>();
        if (!Directory.Exists(_saveDir)) return paths;

        foreach (string file in Directory.GetFiles(_saveDir, "*.json"))
            paths.Add(file);

        paths.Sort(); // alphabetical = chronological since filenames include timestamp
        return paths;
    }

    // ── Replay ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a saved layout and instructs each ShelfSpawner to reconstruct it exactly.
    /// Called automatically at Start() when useReplayLayout is true.
    /// </summary>
    private IEnumerator LoadAndApplyReplayLayout()
    {
        // Wait one frame for ShelfSpawners to finish their default spawn
        yield return null;

        string path = replayFilePath;

        // If no path specified, use the most recent saved layout
        if (string.IsNullOrEmpty(path))
        {
            var all = GetAllSavedSessionPaths();
            if (all.Count == 0)
            {
                Debug.LogWarning("[LayoutManager] Replay mode enabled but no saved layouts found. " +
                                 "Running with random layout.");
                yield break;
            }
            path = all[all.Count - 1]; // most recent
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"[LayoutManager] Replay file not found: {path}");
            yield break;
        }

        string json = File.ReadAllText(path);
        LayoutSnapshot snapshot = JsonUtility.FromJson<LayoutSnapshot>(json);

        if (snapshot == null)
        {
            Debug.LogError("[LayoutManager] Failed to parse layout JSON.");
            yield break;
        }

        _currentLayout = snapshot;
        ApplyReplayLayout(snapshot);

        Debug.Log($"[LayoutManager] Replay layout loaded from '{path}' — " +
                  $"odd item: '{snapshot.chosenOddItem}'");
    }

    private void ApplyReplayLayout(LayoutSnapshot snapshot)
    {
        foreach (ShelfSnapshot shelfSnap in snapshot.shelves)
        {
            ShelfSpawner shelf = shelves.Find(s => s.gameObject.name == shelfSnap.shelfName);
            if (shelf == null)
            {
                Debug.LogWarning($"[LayoutManager] Shelf '{shelfSnap.shelfName}' not found in scene.");
                continue;
            }

            shelf.ApplySnapshot(shelfSnap);
        }

        Debug.Log("[LayoutManager] Replay layout applied to all shelves.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static SerializableVector3 Vec3(Vector3 v) =>
        new SerializableVector3 { x = v.x, y = v.y, z = v.z };

    // ── Data models ───────────────────────────────────────────────────────────

    [Serializable]
    public class LayoutSnapshot
    {
        public string sessionId;
        public string participantId;
        public string capturedAtUtc;
        public string completedAtUtc;
        public string chosenOddItem;
        public float searchTimeSeconds;
        public bool itemFound;
        public SerializableVector3 targetPosition;
        public List<ShelfSnapshot> shelves;
    }

    [Serializable]
    public class ShelfSnapshot
    {
        public string shelfName;
        public List<ItemSnapshot> items;
    }

    [Serializable]
    public class ItemSnapshot
    {
        public int slot;
        public string prefabName;
        public bool isOddItem;
        public SerializableVector3 position;
        public float rotation;
    }

    [Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
}