using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public GameObject optionsPanel;
    public GameObject pausePanel;
    public AudioMixer audioMixer;

    void Start()
    {
        LoadSettings(); // Charge les paramètres au démarrage
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MyExposedParam", Mathf.Log10(volume) * 20);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void BackToPauseMenu()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
    public void SaveSettings()
    {
        // Sauvegarder le volume
        float volume;
        audioMixer.GetFloat("MyExposedParam", out volume); // Récupère la valeur actuelle du volume
        PlayerPrefs.SetFloat("Volume", volume);

        // Sauvegarder la qualité graphique
        int quality = QualitySettings.GetQualityLevel(); // Récupère le niveau de qualité actuel
        PlayerPrefs.SetInt("Quality", quality);

        // Sauvegarde dans PlayerPrefs
        PlayerPrefs.Save();
        Debug.Log("Paramètres sauvegardés !");
    }

    public void LoadSettings()
    {
        // Charger le volume
        if (PlayerPrefs.HasKey("Volume"))
        {
            float volume = PlayerPrefs.GetFloat("Volume");
            audioMixer.SetFloat("MyExposedParam", volume);
        }

        // Charger la qualité graphique
        if (PlayerPrefs.HasKey("Quality"))
        {
            int quality = PlayerPrefs.GetInt("Quality");
            QualitySettings.SetQualityLevel(quality);
        }
    }

}
