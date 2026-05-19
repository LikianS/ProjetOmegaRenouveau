using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEndChecker : MonoBehaviour
{
    [Tooltip("Liste des scenes a charger aleatoirement quand tout est termine")]
    public string[] nextSceneNames;

    [SerializeField, Min(0.05f)]
    private float checkInterval = 0.25f;

    private bool sceneLoaded = false;
    private float checkTimer;

    void Update()
    {
        if (sceneLoaded) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        int enemyCount = EnemyController.ActiveCount + MeleeEnemyController.ActiveCount;
        int collectibleCount = Collectible.ActiveCount;

        if (enemyCount == 0 && collectibleCount == 0)
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