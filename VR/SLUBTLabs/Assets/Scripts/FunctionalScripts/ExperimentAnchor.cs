using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Firebase.Firestore;
using System.Threading.Tasks;

[RequireComponent(typeof(TeleportationAnchor))]
public class ExperimentAnchor : MonoBehaviour
{
    private bool _hasTriggered = false;

    [HideInInspector]
    public string sceneToLoad; // Field used by the custom editor

    private void OnEnable()
    {
        _hasTriggered = false;
        GetComponent<TeleportationAnchor>().selectExited.AddListener(OnAnchorSelected);
    }

    private void OnDisable()
    {
        GetComponent<TeleportationAnchor>().selectExited.RemoveListener(OnAnchorSelected);
    }

    private async void OnAnchorSelected(SelectExitEventArgs args)
    {
        if (_hasTriggered) return;
        _hasTriggered = true;

        // 1. Config must be ready
        if (!ExperimentConfigLoader.IsReady || ExperimentConfigLoader.Current == null)
        {
            Debug.LogWarning("[ExperimentAnchor] Config not ready yet.");
            _hasTriggered = false;
            return;
        }

        ExperimentConfig config = ExperimentConfigLoader.Current;

        Debug.Log("[ExperimentAnchor] Checking for newly assigned web sessions...");
        await config.FetchActiveSessionFromFirestore();
        if (SessionController.Instance != null && !string.IsNullOrEmpty(config.progressId))
        {
            SessionController.Instance.SwitchSession(config.progressId);
        }
        // 2. Must have a progress document
        if (string.IsNullOrEmpty(config.progressId))
        {
            Debug.LogWarning("[ExperimentAnchor] No active progress. Connect VR first from the web.");
            _hasTriggered = false;
            return;
        }

        // 3. Check that the progress is still active
        bool isActive = await IsProgressStillActive(config.progressId);
        if (!isActive)
        {
            Debug.LogWarning("[ExperimentAnchor] Progress is already Completed/ended. Waiting for a new session.");
            _hasTriggered = false;
            return;
        }

        // 4. Resolve scene and load
        string targetScene = ResolveSceneName(config);
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[ExperimentAnchor] Could not resolve scene name.");
            _hasTriggered = false;
            return;
        }

        ExperimentLoader loader = FindAnyObjectByType<ExperimentLoader>();
        if (loader != null)
            loader.LoadScene(targetScene);
        else
            Debug.LogError($"[ExperimentAnchor] ExperimentLoader not found.");
    }

    private async Task<bool> IsProgressStillActive(string progressId)
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentSnapshot snap = await db.Collection("experimentProgress")
                .Document(progressId)
                .GetSnapshotAsync();

            if (!snap.Exists) return false;

            string status = snap.ContainsField("completionStatus")
                ? snap.GetValue<string>("completionStatus") ?? ""
                : "";

            string control = snap.ContainsField("sessionControl")
                ? snap.GetValue<string>("sessionControl") ?? ""
                : "";

            Debug.Log($"[ExperimentAnchor] Checking session status -> status: '{status}' | control: '{control}'");

            bool isNotEnded = control != "ended";
            bool isValidStatus = status == "In Progress" || status == "Not Started" || string.IsNullOrEmpty(status);

            return isValidStatus && isNotEnded;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ExperimentAnchor] Failed to check progress: {e.Message}");
            return false;
        }
    }

    // Made public so the Custom Editor can call it properly
    public string ResolveSceneName(ExperimentConfig config)
    {
        // Debug log to check the live values coming from the config
        Debug.Log($"[ExperimentAnchor] Resolving Scene -> sceneId: '{config.sceneId}' | moduleId: '{config.moduleId}'");

       // if (!string.IsNullOrEmpty(config.sceneId))
       //     return config.sceneId;

        switch (config.moduleId)
        {
            case "Depth_Perception": return "Depth Perception Scene";
            case "Distance_Perception": return "Distance Perception Scene";
            case "Attentional_Blindness": return "Attentional Blindness Scene";
            case "Odd_Item_Detection": return "Odd Item Detection Scene";
            case "Memory2": return "Memory Scene 2";
            case "Memory": return "Memory Scene";
            default:
                Debug.LogWarning($"[ExperimentAnchor] Unknown moduleId: '{config.moduleId}'");
                return null;
        }
    }
}