using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

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
    public bool freezeTimeScaleWhenPaused = false; // usually false in VR; we freeze systems manually

    public SessionState CurrentState { get; private set; } = SessionState.Idle;
    public bool IsPaused => CurrentState == SessionState.Paused;

    public event Action OnPaused;
    public event Action OnResumed;
    public event Action OnSessionEnded;
    public event Action<SessionState> OnStateChanged;

    private ListenerRegistration _listener;
    private bool _movementFrozen;

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

    /// <summary>Call after Connect VR / when you know the progress doc id.</summary>
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
                // Connected but not started: ensure not frozen in a weird way
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

        // Prefer your existing loader if present
        var loader = FindFirstObjectByType<ExperimentLoader>();
        if (loader != null)
        {
            loader.ReturnToHub();
            yield break;
        }

        Debug.LogWarning("[SessionController] No ExperimentLoader — add your hub teleport here.");
    }

    /// <summary>
    /// Disable XR move / turn while paused.
    /// Adjust component names to match your rig if needed.
    /// </summary>
    private void SetMovementEnabled(bool enabled)
    {
        _movementFrozen = !enabled;

        // XR Interaction Toolkit continuous move / turn (common setup)
        var movers = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in movers)
            m.enabled = enabled;

        // If the above type doesn't exist in your XRI version, use a broader approach:
        // Disable CharacterController driven scripts or your custom locomotor.
        var characterControllers = FindObjectsByType<CharacterController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        // Don't disable CC itself (teleport needs it); disable move providers only.
    }

    private void FreezeExperimentSystems(bool freeze)
    {
        // Memory Scene 2 style
        if (ExperimentManager.Instance != null)
        {
            // Soft freeze: pause flag other scripts can read
            // possible Add public bool IsExternallyPaused on ExperimentManager 
        }

        // Optional: disable question buttons while paused
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        // Prefer explicit IsPaused checks inside Update/timers rather than killing all UI
    }

    private void OnDestroy()
    {
        StopListening();
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }
}