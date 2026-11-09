using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LayoutManager : MonoBehaviour
{
    public static LayoutManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (config == null)
            config = ExperimentConfigLoader.Current;

        // NOTE: useReplayLayout is decided later in InitWhenConfigReady, once we know
        // whether a saved layout actually exists. Setting it here caused other scripts
        // to think a replay was coming before the layout had been loaded.
    }

    [Header("Shelf References")]
    public List<ShelfSpawner> shelves = new();

    [Tooltip("Max saved sessions to keep per experiment. 0 = keep all.")]
    public int maxSavedSessions = 50;

    [Header("Replay Mode")]
    public bool useReplayLayout = false;

    public int selectedSessionIndex = 0;
    public List<string> savedSessionNames = new();

    [HideInInspector]
    public List<string> savedSessionPaths = new();

    [Header("Config")]
    public ExperimentConfig config;
    [Tooltip("Max seconds to wait for the experiment config before generating a normal layout.")]
    public float configWaitTimeout = 5f;

    /// <summary>True once the layout decision is final (a saved layout was applied, or a new one will be generated).</summary>
    public bool IsReady { get; private set; }

    /// <summary>True only if a saved layout was successfully applied to the shelves.</summary>
    public bool IsReplaying => IsReady && _layoutWasReplayed;

    private bool _layoutWasReplayed;

    private LayoutSnapshot _currentLayout;
    private string _saveDir;

    private void Start()
    {
        _saveDir = Path.Combine(Application.persistentDataPath, "layouts");
        Directory.CreateDirectory(_saveDir);

        WarnOnDuplicateShelfNames();
        StartCoroutine(InitWhenConfigReady());
    }

    // Layouts are matched to shelves by GameObject name, so names must be unique.
    private void WarnOnDuplicateShelfNames()
    {
        var seen = new HashSet<string>();
        foreach (ShelfSpawner shelf in shelves)
        {
            if (shelf == null) continue;
            if (!seen.Add(shelf.gameObject.name))
                Debug.LogWarning($"[LayoutManager] Two shelves are named '{shelf.gameObject.name}'. " +
                                 "Rename them so each has a unique name, or replay layouts will be wrong.");
        }
    }

    private IEnumerator InitWhenConfigReady()
    {
        try
        {
            float waited = 0f;
            while (!(ExperimentConfigLoader.IsReady &&
                     ExperimentConfigLoader.Current != null &&
                     !string.IsNullOrEmpty(ExperimentConfigLoader.Current.experimentId)) &&
                   waited < configWaitTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (ExperimentConfigLoader.Current == null)
            {
                Debug.LogWarning("[LayoutManager] Config not available. Generating a normal layout.");
                useReplayLayout = false;
                yield break;
            }

            config = ExperimentConfigLoader.Current;
            useReplayLayout = config.oddItem_RepeatLayout;

            RefreshSessionList();

            if (useReplayLayout && savedSessionPaths.Count > 0)
            {
                yield return StartCoroutine(LoadAndApplyReplayLayout());
            }
            else
            {
                useReplayLayout = false;
                Debug.Log($"[LayoutManager] No replay file for experimentId='{config.experimentId}'. " +
                          "Will generate a new layout" + (config.oddItem_RepeatLayout ? " and save it." : "."));
            }
        }
        finally
        {
            // Always release anything waiting on the layout decision
            IsReady = true;
        }
    }

    public void RefreshSessionList()
    {
        savedSessionNames.Clear();
        savedSessionPaths.Clear();

        string dir = Path.Combine(Application.persistentDataPath, "layouts");
        if (!Directory.Exists(dir)) return;

        string experimentId = config != null ? config.experimentId : "";
        if (string.IsNullOrEmpty(experimentId))
        {
            Debug.LogWarning("[LayoutManager] No experimentId – cannot filter layout files.");
            return;
        }

        // Only keep files that belong to the current experiment
        string[] files = Directory.GetFiles(dir, "*.json")
            .Where(f => Path.GetFileName(f).Contains(experimentId))
            .OrderByDescending(f => f) // newest first
            .ToArray();

        foreach (string file in files)
        {
            savedSessionPaths.Add(file);
            savedSessionNames.Add(Path.GetFileNameWithoutExtension(file));
        }

        Debug.Log($"[LayoutManager] Found {savedSessionNames.Count} saved sessions for experimentId='{experimentId}'.");
    }

    public void CaptureLayout(GameObject targetInstance, GameObject chosenOddItemPrefab)
    {
        if (targetInstance == null || chosenOddItemPrefab == null) return;

        _currentLayout = new LayoutSnapshot
        {
            capturedAtUtc = DateTime.UtcNow.ToString("o"),
            chosenOddItem = chosenOddItemPrefab.name,
            targetPosition = Vec3(targetInstance.transform.position),
            studentId = config != null ? config.studentId : "unknown",
            experimentId = config != null ? config.experimentId : "",
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

        Debug.Log($"[LayoutManager] Layout captured — odd item: '{chosenOddItemPrefab.name}'");
    }

    public string SaveSession(float searchTimeSeconds, bool foundItem = true)
    {
        if (config == null || !config.oddItem_RepeatLayout)
        {
            Debug.Log("[LayoutManager] Repeat Layout is OFF – skipping save.");
            return null;
        }

        if (_layoutWasReplayed)
        {
            Debug.Log("[LayoutManager] This session used a saved layout – not saving a duplicate.");
            return null;
        }

        if (_currentLayout == null)
        {
            Debug.LogWarning("[LayoutManager] SaveSession called but no layout was captured.");
            return null;
        }

        string experimentId = config.experimentId ?? "unknown";

        _currentLayout.studentId = config.studentId ?? "unknown";
        _currentLayout.experimentId = experimentId;
        _currentLayout.searchTimeSeconds = searchTimeSeconds;
        _currentLayout.itemFound = foundItem;
        _currentLayout.completedAtUtc = DateTime.UtcNow.ToString("o");

        // Filename contains experimentId so we can filter later
        string fileName = $"session_{experimentId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(_saveDir, fileName);
        string json = JsonUtility.ToJson(_currentLayout, prettyPrint: true);

        // Keep only the newest N files for this experiment
        if (maxSavedSessions > 0)
        {
            RefreshSessionList();
            while (savedSessionPaths.Count >= maxSavedSessions)
            {
                string oldest = savedSessionPaths[savedSessionPaths.Count - 1];
                if (File.Exists(oldest)) File.Delete(oldest);
                savedSessionPaths.RemoveAt(savedSessionPaths.Count - 1);
                savedSessionNames.RemoveAt(savedSessionNames.Count - 1);
            }
        }

        File.WriteAllText(path, json);
        RefreshSessionList();

        Debug.Log($"[LayoutManager] Session saved for experiment '{experimentId}' → {path}");
        return path;
    }

    public LayoutSnapshot GetCurrentLayout() => _currentLayout;

    public List<string> GetAllSavedSessionPaths()
    {
        RefreshSessionList();
        return savedSessionPaths;
    }

    private IEnumerator LoadAndApplyReplayLayout()
    {
        yield return null;

        RefreshSessionList();

        if (savedSessionPaths.Count == 0)
        {
            Debug.LogWarning("[LayoutManager] No saved layout found for this experiment. Generating new one.");
            useReplayLayout = false;
            yield break;
        }

        // Always use the newest matching file
        int index = Mathf.Clamp(selectedSessionIndex, 0, savedSessionPaths.Count - 1);
        string path = savedSessionPaths[index];

        if (!File.Exists(path))
        {
            Debug.LogError($"[LayoutManager] Replay file not found: {path}");
            useReplayLayout = false;
            yield break;
        }

        string json = File.ReadAllText(path);
        LayoutSnapshot snapshot = JsonUtility.FromJson<LayoutSnapshot>(json);

        if (snapshot == null)
        {
            Debug.LogError("[LayoutManager] Failed to parse layout JSON.");
            useReplayLayout = false;
            yield break;
        }

        _currentLayout = snapshot;

        bool applyFailed = false;
        try
        {
            ApplyReplayLayout(snapshot);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LayoutManager] Applying the saved layout threw an error: {ex}");
            applyFailed = true;
        }

        yield return null;   // let the replaced items finish being destroyed

        // Verify that every saved item was really restored. Never leave the shelves empty.
        int expected = 0, actual = 0;
        foreach (ShelfSnapshot shelfSnap in snapshot.shelves)
        {
            ShelfSpawner shelf = shelves.Find(s => s != null && s.gameObject.name == shelfSnap.shelfName);
            expected += shelfSnap.items != null ? shelfSnap.items.Count : 0;

            if (shelf == null) continue;
            foreach (GameObject go in shelf.GetSpawnedItems())
                if (go != null) actual++;
        }

        if (applyFailed || actual < expected)
        {
            Debug.LogError($"[LayoutManager] Replay restored only {actual} of {expected} items " +
                           "(usually a prefab that could not be found, or a shelf name mismatch). " +
                           "Falling back to a fresh random layout.");

            foreach (ShelfSpawner shelf in shelves)
                if (shelf != null) shelf.RespawnShelf();

            _currentLayout = null;
            _layoutWasReplayed = false;
            useReplayLayout = false;
            yield return null;
            yield break;
        }

        _layoutWasReplayed = true;
        Debug.Log($"[LayoutManager] Replaying '{savedSessionNames[index]}' — odd item: '{snapshot.chosenOddItem}' " +
                  $"({actual}/{expected} items restored)");
    }

    private void ApplyReplayLayout(LayoutSnapshot snapshot)
    {
        foreach (ShelfSnapshot shelfSnap in snapshot.shelves)
        {
            ShelfSpawner shelf = shelves.Find(s => s != null && s.gameObject.name == shelfSnap.shelfName);
            if (shelf == null)
            {
                Debug.LogWarning($"[LayoutManager] Shelf '{shelfSnap.shelfName}' not found.");
                continue;
            }
            shelf.ApplySnapshot(shelfSnap);
        }
        Debug.Log("[LayoutManager] Replay layout applied.");
    }

    private static SerializableVector3 Vec3(Vector3 v) =>
        new SerializableVector3 { x = v.x, y = v.y, z = v.z };

    // ─────────────────────────────────────────────
    // Data models
    // ─────────────────────────────────────────────

    [Serializable]
    public class LayoutSnapshot
    {
        public string sessionId;
        public string studentId;
        public string experimentId;          // ← new
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