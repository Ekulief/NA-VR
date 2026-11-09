using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderController : MonoBehaviour
{
    void Awake()
    {
        // Keeps this object alive so SendMessage never fails due to scene transitions
        DontDestroyOnLoad(gameObject);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName) && sceneName != "undefined")
        {
            Debug.Log("Switching preview scene via JS message: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}