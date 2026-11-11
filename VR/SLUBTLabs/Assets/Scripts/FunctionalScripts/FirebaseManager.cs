using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using UnityEngine;

// Initializes Firebase, anonymously authenticates the headset, and asks the trusted
// Firebase callable to save publisherAuthUid on its matching vrDevices document.
// No manual publisher UID pairing or inspector-supplied document ID is required.
//
// Startup is retried automatically when the failure looks temporary (no Wi-Fi, timeout,
// service unavailable). It stops immediately when the failure is permanent (for example,
// the device ID is not registered in vrDevices). After the last attempt, InitializationFailed
// becomes true, and Retry() / RetryNow() can start the whole process again.
public class FirebaseManager : MonoBehaviour
{
    [Header("Startup retry")]
    [Tooltip("How many times to try before giving up.")]
    [SerializeField] private int maxAttempts = 5;
    [Tooltip("Seconds to wait between attempts.")]
    [SerializeField] private float retryDelaySeconds = 5f;
    [Tooltip("Seconds to wait for each step (dependency check, sign-in, registration).")]
    [SerializeField] private float stepTimeoutSeconds = 15f;

    public static bool IsInitialized { get; private set; }
    public static bool IsAuthenticated => FirebaseAuth.DefaultInstance.CurrentUser != null;
    public static string AuthUid => FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
    public static string RegisteredDeviceDocumentId { get; private set; } = "";
    public static string RegisteredVrId { get; private set; } = "";
    public static bool InitializationFailed { get; private set; }

    // Read these from a UI script to show a message in the headset.
    public static bool IsRetrying { get; private set; }
    public static int AttemptNumber { get; private set; }
    public static int MaxAttempts { get; private set; }
    public static string LastError { get; private set; } = "";
    // True when retrying will not help (for example, the device ID is not registered).
    public static bool LastErrorIsPermanent { get; private set; }

    private static FirebaseManager instance;
    private Coroutine initRoutine;

    private enum AttemptResult { Success, Retry, Permanent }

    private void Awake()
    {
        // Prevent a second copy (for example, after a scene reload) from starting another startup.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        StartInitialization();
    }

    // Can be hooked to a UI button (UnityEvent) when the manager is in the same scene.
    public void Retry()
    {
        if (IsInitialized || initRoutine != null) return;
        StartInitialization();
    }

    // Can be called from any script, for example a Retry button on a "not connected" screen.
    public static void RetryNow()
    {
        if (instance != null) instance.Retry();
    }

    private void StartInitialization()
    {
        if (initRoutine != null) StopCoroutine(initRoutine);
        initRoutine = StartCoroutine(InitializeWithRetry());
    }

    private IEnumerator InitializeWithRetry()
    {
        IsInitialized = false;
        InitializationFailed = false;
        IsRetrying = false;
        LastError = "";
        LastErrorIsPermanent = false;
        RegisteredDeviceDocumentId = "";
        RegisteredVrId = "";
        MaxAttempts = Mathf.Max(1, maxAttempts);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            AttemptNumber = attempt;
            AttemptResult result = AttemptResult.Retry;
            yield return TryInitializeOnce(r => result = r);

            if (result == AttemptResult.Success)
            {
                IsRetrying = false;
                IsInitialized = true; // Authentication AND registration are now ready.
                initRoutine = null;
                yield break;
            }

            if (result == AttemptResult.Permanent)
            {
                break; // Retrying will not fix this.
            }

            if (attempt < MaxAttempts)
            {
                IsRetrying = true;
                Debug.LogWarning($"[Firebase] Attempt {attempt}/{MaxAttempts} failed: {LastError}. Retrying in {retryDelaySeconds:0} s...");
                yield return new WaitForSecondsRealtime(retryDelaySeconds);
            }
        }

        IsRetrying = false;
        InitializationFailed = true;
        initRoutine = null;
        Debug.LogError($"[Firebase] Startup failed after {AttemptNumber} attempt(s): {LastError}");
    }

    // One full pass: dependencies, anonymous sign-in, registration.
    private IEnumerator TryInitializeOnce(Action<AttemptResult> done)
    {
        // 1. Dependencies
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        bool finished = false;
        yield return WaitForTask(dependencyTask, ok => finished = ok);
        if (!finished)
        {
            Fail("Dependency check timed out", false);
            done(AttemptResult.Retry);
            yield break;
        }
        if (dependencyTask.IsFaulted || dependencyTask.IsCanceled)
        {
            Fail("Dependency check failed: " +
                 (dependencyTask.Exception?.GetBaseException().Message ?? "Canceled"), false);
            done(AttemptResult.Retry);
            yield break;
        }
        if (dependencyTask.Result != DependencyStatus.Available)
        {
            Fail("Firebase dependencies unavailable: " + dependencyTask.Result, true);
            done(AttemptResult.Permanent);
            yield break;
        }

        // 2. Anonymous sign-in (skipped if a session already exists)
        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.Log("[Firebase] Signing in VR anonymously...");
            var signIn = auth.SignInAnonymouslyAsync();
            finished = false;
            yield return WaitForTask(signIn, ok => finished = ok);
            if (!finished)
            {
                Fail("Anonymous sign-in timed out (check Wi-Fi)", false);
                done(AttemptResult.Retry);
                yield break;
            }
            if (signIn.IsFaulted || signIn.IsCanceled)
            {
                Fail("Anonymous sign-in failed: " +
                     (signIn.Exception?.GetBaseException().Message ?? "Canceled"), false);
                done(AttemptResult.Retry);
                yield break;
            }
        }
        if (auth.CurrentUser == null || !auth.CurrentUser.IsAnonymous)
        {
            Fail("The VR must use its own anonymous device session", true);
            done(AttemptResult.Permanent);
            yield break;
        }

        // 3. Registration. The cloud function verifies the registered hardware ID and writes
        // the anonymous publisherAuthUid to the VR document using the Admin SDK.
        string hardwareId = SystemInfo.deviceUniqueIdentifier;
        var registration = FirebaseFunctions.DefaultInstance
            .GetHttpsCallable("registerVrPublisher")
            .CallAsync(new Dictionary<string, object> { { "deviceId", hardwareId } });
        finished = false;
        yield return WaitForTask(registration, ok => finished = ok);
        if (!finished)
        {
            Fail("Registration timed out (check Wi-Fi)", false);
            done(AttemptResult.Retry);
            yield break;
        }
        if (registration.IsFaulted || registration.IsCanceled)
        {
            bool permanent = IsPermanentFunctionError(registration.Exception, out string message);
            Fail("Registration failed: " + message, permanent);
            done(permanent ? AttemptResult.Permanent : AttemptResult.Retry);
            yield break;
        }

        var data = registration.Result.Data as IDictionary<string, object>;
        if (data == null || !data.ContainsKey("vrDeviceDocumentId") || !data.ContainsKey("vrId"))
        {
            Fail("registerVrPublisher returned an invalid payload", true);
            done(AttemptResult.Permanent);
            yield break;
        }
        RegisteredDeviceDocumentId = data["vrDeviceDocumentId"]?.ToString() ?? "";
        RegisteredVrId = data["vrId"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(RegisteredDeviceDocumentId) || string.IsNullOrEmpty(RegisteredVrId))
        {
            Fail("Missing registered VR identity", true);
            done(AttemptResult.Permanent);
            yield break;
        }

        Debug.Log($"[Firebase] VR {RegisteredVrId} auto-registered as publisher UID {AuthUid} (document {RegisteredDeviceDocumentId}).");
        done(AttemptResult.Success);
    }

    // Waits for a task, but gives up after stepTimeoutSeconds. completed(true) means it finished in time.
    private IEnumerator WaitForTask(Task task, Action<bool> completed)
    {
        float elapsed = 0f;
        while (!task.IsCompleted && elapsed < stepTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        completed(task.IsCompleted);
    }

    private static void Fail(string message, bool permanent)
    {
        LastError = message;
        LastErrorIsPermanent = permanent;
        Debug.LogWarning("[Firebase] " + message);
    }

    // Errors that mean the request itself is wrong (device not registered, not allowed),
    // so waiting and retrying will not help. Network and server problems are retried.
    private static bool IsPermanentFunctionError(AggregateException exception, out string message)
    {
        message = exception?.GetBaseException().Message ?? "Unknown error";
        if (exception == null) return false;

        foreach (var inner in exception.Flatten().InnerExceptions)
        {
            if (inner is FunctionsException functionsException)
            {
                message = functionsException.ErrorCode + ": " + functionsException.Message;
                switch (functionsException.ErrorCode)
                {
                    case FunctionsErrorCode.NotFound:
                    case FunctionsErrorCode.PermissionDenied:
                    case FunctionsErrorCode.InvalidArgument:
                    case FunctionsErrorCode.FailedPrecondition:
                        return true;
                    default:
                        return false;
                }
            }
        }
        return false;
    }
}