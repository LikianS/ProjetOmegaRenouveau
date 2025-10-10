using UnityEngine;
using UnityEngine.SceneManagement;

public class AmbianceManager : MonoBehaviour
{
    private void Start()
    {
        PlayAmbianceForCurrentScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayAmbianceForCurrentScene();
    }

    private void PlayAmbianceForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Design world")
        {
            SoundManager.Instance.PlayVillageAmbience();
        }
        else if (sceneName == "DonjonMob")
        {
            SoundManager.Instance.PlayDungeonAmbience();
        }
        else if (sceneName == "House Interior")
        {
            SoundManager.Instance.PlayWorldAmbience();
        }
    }
}
