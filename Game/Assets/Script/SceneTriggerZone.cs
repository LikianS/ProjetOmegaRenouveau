using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [Tooltip("Liste des noms de scènes à charger aléatoirement")]
    public string[] sceneNames;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && sceneNames != null && sceneNames.Length > 0)
        {
            int randomIndex = Random.Range(0, sceneNames.Length);
            string randomScene = sceneNames[randomIndex];
            ScreenFader.FadeAndLoadScene(randomScene, 1f);
        }
    }
}
