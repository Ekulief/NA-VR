using System.Collections;
using Firebase;
using Firebase.Auth;
using UnityEngine;

// Initializes Firebase AND authenticates this VR installation without a student login.
// During development, copy the logged anonymous UID to vrDevices/{deviceDocId}.publisherAuthUid
// using a trusted admin/Firebase console. Do not make pairing self-service for anonymous clients.
public class FirebaseManager : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }
    public static bool IsAuthenticated => FirebaseAuth.DefaultInstance.CurrentUser != null;
    public static string AuthUid => FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(InitializeFirebase());
    }

    private IEnumerator InitializeFirebase()
    {
        IsInitialized = false;
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);
        if (dependencyTask.IsFaulted || dependencyTask.IsCanceled ||
            dependencyTask.Result != DependencyStatus.Available)
        {
            Debug.LogError("[Firebase] Dependencies unavailable: " +
                (dependencyTask.Exception?.ToString() ??
                 (dependencyTask.IsCanceled ? "Canceled" : dependencyTask.Result.ToString())));
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
                yield break;
            }
        }
        if (auth.CurrentUser == null || !auth.CurrentUser.IsAnonymous)
        {
            Debug.LogError("[Firebase] The VR must use its own anonymous device session during development.");
            yield break;
        }

        Debug.Log("[Firebase] Anonymous VR UID (pair this UID with its vrDevices document): " + auth.CurrentUser.UserId);
        IsInitialized = true; // Existing loaders now wait for both Firebase and auth.
    }
}
