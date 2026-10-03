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

        // GRAB THE RIG: Since the Main Camera is inside your Player_6DoF_Rig, 
        // its direct parent is the object we need to take to the next scene.
        GameObject camRig = Camera.main.transform.parent != null ?
                           Camera.main.transform.parent.gameObject : Camera.main.gameObject;

        DontDestroyOnLoad(camRig);

        SceneManager.LoadScene(targetScene);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawn = GameObject.Find(spawnPointName);
        GameObject camRig = Camera.main.transform.parent != null ?
                           Camera.main.transform.parent.gameObject : Camera.main.gameObject;

        if (spawn != null && camRig != null)
        {
            // 1. Force the AR tracking code to instantly clear its baseline history
            Cardboard6DoF dynamicTracker = camRig.GetComponent<Cardboard6DoF>();
            if (dynamicTracker != null)
            {
                dynamicTracker.Recalibrate();
            }

            // 2. Snap the rig directly onto the spawn point coordinates
            camRig.transform.position = spawn.transform.position;
            camRig.transform.rotation = spawn.transform.rotation;
        }

        // Re-enable the reticle so you can see it in the new scene
        var reticle = Object.FindAnyObjectByType<CardboardReticlePointer>(FindObjectsInactive.Include);
        if (reticle != null)
        {
            reticle.gameObject.SetActive(true);
            reticle.SendMessage("OnPointerExit", null, SendMessageOptions.DontRequireReceiver);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}