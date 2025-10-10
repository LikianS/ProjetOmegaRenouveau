using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    private static ScreenFader instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void FadeAndLoadScene(string sceneName, float duration)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.FadeOutAndLoad(sceneName, duration));
        }
    }

    private IEnumerator FadeOutAndLoad(string sceneName, float duration)
    {
        yield return StartCoroutine(Fade(0f, 1f, duration));
        SceneManager.LoadScene(sceneName);
        Debug.Log("ScreenFader: fadeImage n'est pas null après le chargement de la scène !");
        yield return StartCoroutine(Fade(1f, 0f, duration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            color.a = Mathf.Lerp(from, to, t);
            fadeImage.color = color;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        color.a = to;
        fadeImage.color = color;
    }
}
