using UnityEngine;
using UnityEngine.SceneManagement;

public class GazeAndTakeCamera : MonoBehaviour
{
    public string targetScene;
    public string spawnPointName;
    public float gazeTime = 2.0f;

    private float timer;
    private bool isGazing;

    public void OnPointerEnter() => isGazing = true;
    public void OnPointerExit() { isGazing = false; timer = 0; }

    void Update()
    {
        if (isGazing)
        {
            timer += Time.deltaTime;
            if (timer >= gazeTime)
            {
                ExecuteTeleport();
            }
        }
    }

    void ExecuteTeleport()
    {

        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>();
        if (reticle != null)
        {
            reticle.gameObject.SetActive(false);
        }

 
        GameObject camRig = Camera.main.transform.root.gameObject;
        DontDestroyOnLoad(camRig);

        SceneManager.LoadScene(targetScene);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawn = GameObject.Find(spawnPointName);
        if (spawn != null)
        {
            GameObject camRig = Camera.main.transform.root.gameObject;
            camRig.transform.position = spawn.transform.position;
            camRig.transform.rotation = spawn.transform.rotation;
        }

        // Re-enable the reticle so you can see it in the new scene
        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>(FindObjectsInactive.Include);
        if (reticle != null)
        {
            reticle.gameObject.SetActive(true);

            // FORCE RESET: This tells the reticle "You aren't looking at anything anymore"
            // It forces the circle to shrink back to a dot.
            reticle.SendMessage("OnPointerExit", null, SendMessageOptions.DontRequireReceiver);
        }

        // Clean up the event listener
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}