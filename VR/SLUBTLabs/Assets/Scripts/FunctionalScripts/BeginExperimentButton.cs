
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BeginExperimentButton : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private UnityEvent onSessionStarted;

    private bool starting;

    private void Awake()
    {
        if (startButton == null) startButton = GetComponent<Button>();
        startButton.onClick.AddListener(OnBeginPressed);
    }

    private async void OnBeginPressed()
    {
        if (starting) return;
        starting = true;
        startButton.interactable = false;

        string progressId = ExperimentConfigLoader.Current != null
            ? ExperimentConfigLoader.Current.progressId
            : null;

        Debug.Log($"[BeginExperiment] progressId = '{progressId}'");

        if (string.IsNullOrEmpty(progressId))
        {
            Debug.LogWarning("[BeginExperiment] No progressId – web app was never connected!");
            startButton.interactable = true;
            starting = false;
            return;
        }

        bool started = await TryStartSession(progressId);
        Debug.Log($"[BeginExperiment] TryStartSession returned: {started}");

        if (started)
        {
            LocomotionLocker.Unlock();
            onSessionStarted.Invoke();
        }
        else
        {
            startButton.interactable = true;
        }

        starting = false;
    }
    private static async Task<bool> TryStartSession(string progressId)
    {
        var db = FirebaseFirestore.DefaultInstance;
        var docRef = db.Collection("experimentProgress").Document(progressId);

        try
        {
            return await db.RunTransactionAsync(async tx =>
            {
                var snap = await tx.GetSnapshotAsync(docRef);
                if (!snap.Exists) return false;
                if (snap.GetValue<string>("sessionControl") != "idle") return false;

                tx.Update(docRef, new Dictionary<string, object>
                {
                    { "sessionControl", "running" },
                    { "elapsedSeconds", 0 },
                    { "updatedAt", FieldValue.ServerTimestamp },
                });
                return true;
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BeginExperiment] Failed to start session: {e}");
            return false;
        }
    }
}