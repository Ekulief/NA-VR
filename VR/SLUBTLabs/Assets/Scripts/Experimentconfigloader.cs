using System.Collections;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Experiment Config Loader
/// Attach to your experiment manager GameObjects alongside the experiment script.
/// Fetches remote config before the experiment starts so all parameters are ready.
///
/// SETUP:
///   a) Create one ExperimentConfig asset: Assets → Create → SLUBT Labs → Experiment Config
///   b) Assign it to this component in the Inspector
///   c) Attach this to the same GameObject as your experiment manager
///   d) Your experiment manager reads from config after this loader finishes
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
            Debug.Log("[ConfigLoader] Fetching remote config...");

            // TODO: Replace with actual Firebase async call
            // yield return StartCoroutine(FetchFirebaseConfig());

            // Stub — simulate network delay
            yield return new WaitForSeconds(0.5f);
            config.LoadFromRemoteConfig();
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