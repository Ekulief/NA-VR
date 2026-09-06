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
///   Normal mode  → layout is random each session, saved automatically.
///   Replay mode  → tick useReplayLayout, click Refresh Session List,
///                  pick a session from the dropdown, hit Play.
///                  All participants will see that exact shelf configuration.
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

    [Tooltip("Index of the session to replay from the list below. " +
             "0 = most recent. Use the Editor button to refresh the list.")]
    public int selectedSessionIndex = 0;

    [Tooltip("Read-only list of saved session file names — refreshed by the Editor button.")]
    public List<string> savedSessionNames = new();

    [HideInInspector]
    public List<string> savedSessionPaths = new();

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

        RefreshSessionList();

        if (useReplayLayout)
            StartCoroutine(LoadAndApplyReplayLayout());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the saved session list — call this from the Inspector button
    /// or at runtime to update the dropdown.
    /// </summary>
    public void RefreshSessionList()
    {
        savedSessionNames.Clear();
        savedSessionPaths.Clear();

        string dir = Path.Combine(Application.persistentDataPath, "layouts");
        if (!Directory.Exists(dir)) return;

        string[] files = Directory.GetFiles(dir, "*.json");
        Array.Sort(files);
        Array.Reverse(files); // most recent first

        foreach (string file in files)
        {
            savedSessionPaths.Add(file);
            savedSessionNames.Add(Path.GetFileNameWithoutExtension(file));
        }

        Debug.Log($"[LayoutManager] Found {savedSessionNames.Count} saved sessions.");
    }

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
    /// Call this from OddItemManager when the participant finds the odd item or times out.
    /// Saves the layout + result to JSON.
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

        // Prune old sessions if limit set
        if (maxSavedSessions > 0)
        {
            RefreshSessionList();
            while (savedSessionPaths.Count >= maxSavedSessions)
            {
                File.Delete(savedSessionPaths[savedSessionPaths.Count - 1]);
                savedSessionPaths.RemoveAt(savedSessionPaths.Count - 1);
                savedSessionNames.RemoveAt(savedSessionNames.Count - 1);
            }
        }

        File.WriteAllText(path, json);
        RefreshSessionList();

        Debug.Log($"[LayoutManager] Session saved → {path}");
        return path;
    }

    public LayoutSnapshot GetCurrentLayout() => _currentLayout;

    public List<string> GetAllSavedSessionPaths()
    {
        RefreshSessionList();
        return savedSessionPaths;
    }

    // ── Replay ────────────────────────────────────────────────────────────────
    private IEnumerator LoadAndApplyReplayLayout()
    {
        yield return null; // wait for ShelfSpawners

        RefreshSessionList();

        if (savedSessionPaths.Count == 0)
        {
            Debug.LogWarning("[LayoutManager] Replay mode enabled but no saved sessions found.");
            yield break;
        }

        // Clamp index to valid range
        int index = Mathf.Clamp(selectedSessionIndex, 0, savedSessionPaths.Count - 1);
        string path = savedSessionPaths[index];

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

        Debug.Log($"[LayoutManager] Replaying session '{savedSessionNames[index]}' — " +
                  $"odd item: '{snapshot.chosenOddItem}'");
    }

    private void ApplyReplayLayout(LayoutSnapshot snapshot)
    {
        foreach (ShelfSnapshot shelfSnap in snapshot.shelves)
        {
            ShelfSpawner shelf = shelves.Find(s => s.gameObject.name == shelfSnap.shelfName);
            if (shelf == null)
            {
                Debug.LogWarning($"[LayoutManager] Shelf '{shelfSnap.shelfName}' not found.");
                continue;
            }
            shelf.ApplySnapshot(shelfSnap);
        }
        Debug.Log("[LayoutManager] Replay layout applied.");
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