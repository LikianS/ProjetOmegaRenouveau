using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SoundPanelUI : MonoBehaviour
{
    public Slider musicSlider;
    public Slider effectsSlider;
    public Slider voiceSlider;
    [HideInInspector] public GameObject menuPanel;
    private float navCooldown = 0.25f;
    private float lastNavTime = 0f;

    private void OnEnable()
    {
        if (musicSlider != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void Update()
    {
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(true);
            return;
        }
    }


    private void Start()
    {
        musicSlider.value = SoundManager.Instance.MusicVolume;
        effectsSlider.value = SoundManager.Instance.EffectsVolume;
        voiceSlider.value = SoundManager.Instance.VoiceVolume;

        musicSlider.onValueChanged.AddListener(SoundManager.Instance.SetMusicVolume);
        effectsSlider.onValueChanged.AddListener(SoundManager.Instance.SetEffectsVolume);
        voiceSlider.onValueChanged.AddListener(SoundManager.Instance.SetVoiceVolume);

    }
}
