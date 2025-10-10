using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartup : MonoBehaviour
{
    public string menuSceneName = "MenuScene";

    private void Start()
    {
        if (!SceneManager.GetSceneByName(menuSceneName).isLoaded)
        {
            SceneManager.LoadScene(menuSceneName, LoadSceneMode.Additive);
        }
    }
}
