using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        if (config != null)
            useReplayLayout = config.oddItem_RepeatLayout;
    }

    [Header("Shelf References")]
    public List<ShelfSpawner> shelves = new();

    [Tooltip("Max saved sessions to keep. 0 = keep all.")]
    public int maxSavedSessions = 50;

    [Header("Replay Mode")]
    [Tooltip("If true, loads a saved layout instead of generating a random one.")]
    public bool useReplayLayout = false;

    public int selectedSessionIndex = 0;
    public List<string> savedSessionNames = new();

    [HideInInspector]
    public List<string> savedSessionPaths = new();

    [Header("Config")]
    public ExperimentConfig config;

    public bool IsReplaying => useReplayLayout;

    private LayoutSnapshot _currentLayout;
    private string _saveDir;

    private void Start()
    {
        _saveDir = Path.Combine(Application.persistentDataPath, "layouts");
        Directory.CreateDirectory(_saveDir);

        if (config == null)
            config = ExperimentConfigLoader.Current;

        if (config != null)
            useReplayLayout = config.oddItem_RepeatLayout;

        RefreshSessionList();

        if (useReplayLayout)
            StartCoroutine(LoadAndApplyReplayLayout());
    }

    public void RefreshSessionList()
    {
        savedSessionNames.Clear();
        savedSessionPaths.Clear();

        string dir = Path.Combine(Application.persistentDataPath, "layouts");
        if (!Directory.Exists(dir)) return;

        string[] files = Directory.GetFiles(dir, "*.json");
        Array.Sort(files);
        Array.Reverse(files);

        foreach (string file in files)
        {
            savedSessionPaths.Add(file);
            savedSessionNames.Add(Path.GetFileNameWithoutExtension(file));
        }

        Debug.Log($"[LayoutManager] Found {savedSessionNames.Count} saved sessions.");
    }

    public void CaptureLayout(GameObject targetInstance, GameObject chosenOddItemPrefab)
    {
        _currentLayout = new LayoutSnapshot
        {
            capturedAtUtc = DateTime.UtcNow.ToString("o"),
            chosenOddItem = chosenOddItemPrefab.name,
            targetPosition = Vec3(targetInstance.transform.position),
            studentId = config != null ? config.studentId : "unknown",
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

    public string SaveSession(float searchTimeSeconds, bool foundItem = true)
    {
        if (_currentLayout == null)
        {
            Debug.LogWarning("[LayoutManager] SaveSession called but no layout was captured.");
            return null;
        }

        _currentLayout.studentId = config != null ? config.studentId : "unknown";
        _currentLayout.searchTimeSeconds = searchTimeSeconds;
        _currentLayout.itemFound = foundItem;
        _currentLayout.completedAtUtc = DateTime.UtcNow.ToString("o");

        string fileName = $"session_{_currentLayout.studentId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(_saveDir, fileName);
        string json = JsonUtility.ToJson(_currentLayout, prettyPrint: true);

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

    private IEnumerator LoadAndApplyReplayLayout()
    {
        yield return null;

        RefreshSessionList();

        if (savedSessionPaths.Count == 0)
        {
            Debug.LogWarning("[LayoutManager] Replay mode enabled but no saved sessions found.");
            yield break;
        }

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

    private static SerializableVector3 Vec3(Vector3 v) =>
        new SerializableVector3 { x = v.x, y = v.y, z = v.z };

    [Serializable]
    public class LayoutSnapshot
    {
        public string sessionId;
        public string studentId;
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