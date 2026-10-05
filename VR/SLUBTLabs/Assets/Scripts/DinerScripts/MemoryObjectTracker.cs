using System.Collections.Generic;
using UnityEngine;

public enum TrialType { ChangeDetection, Location, Recognition }

[System.Serializable]
public class MemoryTrial
{
    public TrialType type;
    public string question;
    public string[] choices;
    public int correctIndex;
    public float onsetTime;
}

public class MemoryObjectTracker : MonoBehaviour
{
    [System.Serializable]
    public class TrackedObject
    {
        public Transform transform;
        public Vector3 originalPos;
        public Quaternion originalRot;
        public bool isCritical = true;
    }

    [Header("Assign ONLY the objects you separated in the scene")]
    public List<Transform> memoryObjects = new List<Transform>();   // ← drag your separated items here

    [Header("Settings")]
    public int maxChangesPerTrial = 2;

    private List<TrackedObject> objects = new List<TrackedObject>();

    public void CaptureOriginalLayout()
    {
        objects.Clear();

        foreach (var t in memoryObjects)
        {
            if (t == null) continue;

            objects.Add(new TrackedObject
            {
                transform = t,
                originalPos = t.position,
                originalRot = t.rotation,
                isCritical = true
            });
        }

        Debug.Log($"[Memory] Captured {objects.Count} memory objects");
    }

    public MemoryTrial CreateRandomTrial()
    {
        var trial = new MemoryTrial
        {
            type = Random.value > 0.5f ? TrialType.ChangeDetection : TrialType.Location,
            onsetTime = Time.time
        };

        // Reset everything first
        ResetToOriginal();

        // Make 1–maxChangesPerTrial changes
        int changes = Random.Range(1, maxChangesPerTrial + 1);
        var candidates = new List<TrackedObject>(objects);

        for (int i = 0; i < changes && candidates.Count > 0; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            var obj = candidates[idx];
            candidates.RemoveAt(idx);

            // Simple but visible change – move a little or hide
            if (Random.value > 0.4f)
            {
                obj.transform.position += Random.insideUnitSphere * 0.25f;
            }
            else
            {
                obj.transform.gameObject.SetActive(false);
            }
        }

        // Build question
        if (trial.type == TrialType.ChangeDetection)
        {
            trial.question = "What changed in the scene?";
            trial.choices = new[]
            {
                "Nothing changed",
                "Some objects moved",
                "Some objects disappeared",
                "Objects changed color"
            };
            trial.correctIndex = changes > 0 ? 1 : 0;   // simple heuristic
        }
        else
        {
            trial.question = "Which statement is true?";
            trial.choices = new[]
            {
                "All objects are in their original places",
                "At least one object was moved",
                "At least one object was removed",
                "The lighting changed"
            };
            trial.correctIndex = changes > 0 ? 1 : 0;
        }

        return trial;
    }

    public void ApplyTrialChanges(MemoryTrial t)
    {
        // Changes are already applied inside CreateRandomTrial
        // (keeps the code simple). You can expand this later.
    }

    public void ResetToOriginal()
    {
        foreach (var o in objects)
        {
            if (o.transform == null) continue;
            o.transform.position = o.originalPos;
            o.transform.rotation = o.originalRot;
            o.transform.gameObject.SetActive(true);
        }
    }
}