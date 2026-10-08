using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                IsInitialized = true;
                Debug.Log("[Firebase] Successfully initialized dependencies.");
            }
            else
            {
                Debug.LogError($"[Firebase] Dependency check failed: {dependencyStatus}");
            }
        });
    }
}