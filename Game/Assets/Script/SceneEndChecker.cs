using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEndChecker : MonoBehaviour
{
    [Tooltip("Liste des scènes à charger aléatoirement quand tout est terminé")]
    public string[] nextSceneNames;

    private bool sceneLoaded = false;

    void Update()
    {
        if (sceneLoaded) return;

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        var collectibles = GameObject.FindGameObjectsWithTag("Collectible");

        if (enemies.Length == 0 && collectibles.Length == 0)
        {
            sceneLoaded = true;
            if (nextSceneNames != null && nextSceneNames.Length > 0)
            {
                int randomIndex = Random.Range(0, nextSceneNames.Length);
                string nextScene = nextSceneNames[randomIndex];
                ScreenFader.FadeAndLoadScene(nextScene, 1f);
            }
        }
    }
}