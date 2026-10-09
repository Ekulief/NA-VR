using Firebase.Firestore;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;   // ← for InputActionManager

/// <summary>
/// Listens to experimentProgress.sessionControl and applies:
/// running | paused | ended | idle
/// </summary>
public class SessionController : MonoBehaviour
{
    public static SessionController Instance { get; private set; }

    public enum SessionState
    {
        Idle,
        Running,
        Paused,
        Ended
    }

    [Header("Firestore")]
    [Tooltip("Progress document id. Can be set at runtime when instructor connects.")]
    public string progressDocumentId = "";

    [Header("Behaviour")]
    public bool returnToHubOnEnd = true;
    public bool freezeTimeScaleWhenPaused = false; // usually false in VR

    public SessionState CurrentState { get; private set; } = SessionState.Idle;
    public bool IsPaused => CurrentState == SessionState.Paused;

    public event Action OnPaused;
    public event Action OnResumed;
    public event Action OnSessionEnded;
    public event Action<SessionState> OnStateChanged;

    private ListenerRegistration _listener;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(progressDocumentId))
            StartListening(progressDocumentId);
    }

    public void StartListening(string progressId)
    {
        if (string.IsNullOrEmpty(progressId))
        {
            Debug.LogWarning("[SessionController] progressId empty.");
            return;
        }

        progressDocumentId = progressId;
        StopListening();

        DocumentReference doc = FirebaseFirestore.DefaultInstance
            .Collection("experimentProgress")
            .Document(progressDocumentId);

        _listener = doc.Listen(snapshot =>
        {
            if (!snapshot.Exists)
            {
                Debug.LogWarning("[SessionController] Progress doc deleted — treating as ended.");
                ApplyState(SessionState.Ended);
                return;
            }

            string control = "idle";
            if (snapshot.ContainsField("sessionControl"))
                control = snapshot.GetValue<string>("sessionControl") ?? "idle";

            ApplyState(ParseControl(control));
        });

        Debug.Log($"[SessionController] Listening to experimentProgress/{progressDocumentId}");
    }

    public void StopListening()
    {
        _listener?.Stop();
        _listener = null;
    }

    private static SessionState ParseControl(string control)
    {
        switch ((control ?? "idle").Trim().ToLowerInvariant())
        {
            case "running":
            case "play":
            case "resume":
                return SessionState.Running;
            case "paused":
            case "pause":
                return SessionState.Paused;
            case "ended":
            case "stopped":
            case "stop":
            case "aborted":
                return SessionState.Ended;
            default:
                return SessionState.Idle;
        }
    }

    private void ApplyState(SessionState next)
    {
        if (next == CurrentState) return;

        SessionState prev = CurrentState;
        CurrentState = next;
        Debug.Log($"[SessionController] {prev} → {next}");

        switch (next)
        {
            case SessionState.Paused:
                EnterPaused();
                break;
            case SessionState.Running:
                EnterRunning();
                break;
            case SessionState.Ended:
                EnterEnded();
                break;
            case SessionState.Idle:
                PauseOverlay.Instance?.Hide();
                SetMovementEnabled(true);
                break;
        }

        OnStateChanged?.Invoke(next);
    }

    private void EnterPaused()
    {
        PauseOverlay.Instance?.Show();
        SetMovementEnabled(false);
        FreezeExperimentSystems(true);

        if (freezeTimeScaleWhenPaused)
            Time.timeScale = 0f;

        OnPaused?.Invoke();
    }

    private void EnterRunning()
    {
        PauseOverlay.Instance?.Hide();
        SetMovementEnabled(true);
        FreezeExperimentSystems(false);

        if (freezeTimeScaleWhenPaused)
            Time.timeScale = 1f;

        OnResumed?.Invoke();
    }

    private void EnterEnded()
    {
        PauseOverlay.Instance?.Hide();
        SetMovementEnabled(true);
        FreezeExperimentSystems(false);
        Time.timeScale = 1f;

        OnSessionEnded?.Invoke();

        if (returnToHubOnEnd)
            StartCoroutine(ReturnToHubNextFrame());
    }

    private IEnumerator ReturnToHubNextFrame()
    {
        yield return null;

        var loader = FindFirstObjectByType<ExperimentLoader>();
        if (loader != null)
        {
            loader.ReturnToHub();
            yield break;
        }

        Debug.LogWarning("[SessionController] No ExperimentLoader — add your hub teleport here.");
    }

    /// <summary>
    /// Freezes / unfreezes player movement by enabling/disabling
    /// the XR Interaction Simulator (keeps hands locked in place).
    /// </summary>
    private void SetMovementEnabled(bool enabled)
    {
        // Find every XR Interaction Simulator in the scene (including inactive)
        var simulators = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int count = 0;
        foreach (var mb in simulators)
        {
            // Match by class name so we don't need the exact namespace
            if (mb.GetType().Name == "XRInteractionSimulator")
            {
                mb.enabled = enabled;
                count++;
                Debug.Log($"[SessionController] XRInteractionSimulator on '{mb.gameObject.name}' → enabled = {enabled}");
            }
        }

        if (count == 0)
            Debug.LogWarning("[SessionController] No XRInteractionSimulator found in scene.");
    }

    private void FreezeExperimentSystems(bool freeze)
    {
        // Soft freeze flag other scripts can read
        if (ExperimentManager.Instance != null)
        {
            // ExperimentManager.Instance.IsExternallyPaused = freeze;   // uncomment when you add the flag
        }

        // Prefer explicit IsPaused checks inside your experiment scripts
        // rather than disabling whole canvases.
    }

    private void OnDestroy()
    {
        StopListening();
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }
}