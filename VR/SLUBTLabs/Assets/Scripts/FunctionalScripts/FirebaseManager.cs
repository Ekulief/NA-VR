using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using System.Collections.Generic;
using UnityEngine;

// Initializes Firebase, anonymously authenticates the headset, and asks the trusted
// Firebase callable to save publisherAuthUid on its matching vrDevices document.
// No manual publisher UID pairing or inspector-supplied document ID is required.
public class FirebaseManager : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }
    public static bool IsAuthenticated => FirebaseAuth.DefaultInstance.CurrentUser != null;
    public static string AuthUid => FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
    public static string RegisteredDeviceDocumentId { get; private set; } = "";
    public static string RegisteredVrId { get; private set; } = "";
    public static bool InitializationFailed { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(InitializeFirebase());
    }

    private IEnumerator InitializeFirebase()
    {
        IsInitialized = false;
        InitializationFailed = false;
        RegisteredDeviceDocumentId = "";
        RegisteredVrId = "";
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);
        if (dependencyTask.IsFaulted || dependencyTask.IsCanceled ||
            dependencyTask.Result != DependencyStatus.Available)
        {
            Debug.LogError("[Firebase] Dependencies unavailable: " +
                (dependencyTask.Exception?.ToString() ??
                 (dependencyTask.IsCanceled ? "Canceled" : dependencyTask.Result.ToString())));
            InitializationFailed = true;
            yield break;
        }

        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.Log("[Firebase] Signing in VR anonymously...");
            var signIn = auth.SignInAnonymouslyAsync();
            yield return new WaitUntil(() => signIn.IsCompleted);
            if (signIn.IsFaulted || signIn.IsCanceled)
            {
                Debug.LogError("[Firebase] Anonymous VR sign-in failed: " + signIn.Exception);
                InitializationFailed = true;
                yield break;
            }
        }
        if (auth.CurrentUser == null || !auth.CurrentUser.IsAnonymous)
        {
            Debug.LogError("[Firebase] The VR must use its own anonymous device session.");
            InitializationFailed = true;
            yield break;
        }

        // This cloud function verifies the registered hardware ID and writes the
        // anonymous publisherAuthUid to the VR document using Admin SDK.
        string hardwareId = SystemInfo.deviceUniqueIdentifier;
        var registration = FirebaseFunctions.DefaultInstance
            .GetHttpsCallable("registerVrPublisher")
            .CallAsync(new Dictionary<string, object> { { "deviceId", hardwareId } });
        yield return new WaitUntil(() => registration.IsCompleted);
        if (registration.IsFaulted || registration.IsCanceled)
        {
            Debug.LogError("[Firebase] Automatic publisher registration failed: " + registration.Exception);
            InitializationFailed = true;
            yield break;
        }

        var data = registration.Result.Data as IDictionary<string, object>;
        if (data == null || !data.ContainsKey("vrDeviceDocumentId") || !data.ContainsKey("vrId"))
        {
            Debug.LogError("[Firebase] registerVrPublisher returned an invalid payload.");
            InitializationFailed = true;
            yield break;
        }
        RegisteredDeviceDocumentId = data["vrDeviceDocumentId"]?.ToString() ?? "";
        RegisteredVrId = data["vrId"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(RegisteredDeviceDocumentId) || string.IsNullOrEmpty(RegisteredVrId))
        {
            Debug.LogError("[Firebase] Missing registered VR identity.");
            InitializationFailed = true;
            yield break;
        }

        Debug.Log($"[Firebase] VR {RegisteredVrId} auto-registered as publisher UID {AuthUid} (document {RegisteredDeviceDocumentId}).");
        IsInitialized = true; // Authentication AND registration are now ready.
    }
}
