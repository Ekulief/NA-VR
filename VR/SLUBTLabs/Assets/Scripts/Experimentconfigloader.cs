using System.Collections;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Experiment Config Loader
/// Attach to your experiment manager GameObjects alongside the experiment script.
/// Waits for Firebase initialization, fetches parameters from Firestore, and sets IsReady = true.
/// </summary>
public class ExperimentConfigLoader : MonoBehaviour
{
    [Tooltip("Drag your ExperimentConfig asset here.")]
    public ExperimentConfig config;

    public static ExperimentConfig Current { get; private set; }
    public static bool IsReady { get; private set; } = false;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[ConfigLoader] No ExperimentConfig assigned!");
            return;
        }

        Current = config;
        IsReady = false;

        StartCoroutine(LoadConfig());
    }

    private IEnumerator LoadConfig()
    {
        if (config.useRemoteConfig)
        {
            Debug.Log("[ConfigLoader] Waiting for Firebase initialization...");

            // Wait until FirebaseManager dependencies are fully initialized
            yield return new WaitUntil(() => FirebaseManager.IsInitialized);

            Debug.Log("[ConfigLoader] Fetching parameters from Firestore...");

            // Execute the Task returned by FetchFromFirestoreAsync
            var fetchTask = config.FetchFromFirestoreAsync();

            // Wait for the async Firestore Task to complete
            yield return new WaitUntil(() => fetchTask.IsCompleted);

            if (fetchTask.IsFaulted)
            {
                Debug.LogWarning($"[ConfigLoader] Firestore fetch encountered an error: {fetchTask.Exception}");
            }
        }
        else
        {
            Debug.Log("[ConfigLoader] Using local config values.");
            yield return null;
        }

        IsReady = true;
        Debug.Log("[ConfigLoader] Config ready.");
    }
}