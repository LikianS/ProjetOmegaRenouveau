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

    private PlayerInput playerInput;
    private InputAction cancelAction;

    private void OnEnable()
    {
        SelectMusicSlider();
        SetMenuPanelActive(false);
        CacheCancelAction();
    }

    private void Update()
    {
        if (cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            ReturnToMenu();
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

    private void SelectMusicSlider()
    {
        if (musicSlider != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
    }

    private void SetMenuPanelActive(bool isActive)
    {
        if (menuPanel != null)
            menuPanel.SetActive(isActive);
    }

    private void ReturnToMenu()
    {
        gameObject.SetActive(false);
        SetMenuPanelActive(true);
    }

    private void CacheCancelAction()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        var uiMap = playerInput.actions.FindActionMap("UI", false);
        if (uiMap == null) return;

        cancelAction = uiMap.FindAction("Cancel", false);
    }
}
