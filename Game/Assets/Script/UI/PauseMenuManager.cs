using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuPanel;
    public List<Button> pauseMenuButtons;
    public RectTransform selectionArrow;
    public GameObject controlsPanel;
    public GameObject soundPanel;
    public ControlsPanelUI controlsPanelUI;
    public SoundPanelUI soundPanelUI;
    private float navCooldown = 0.25f;
    private float lastNavTime = 0f;
    private int currentIndex = 0;

    private PlayerInput playerInput;

    private void Start()
    {
        currentIndex = 0;
        if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[0].gameObject);

        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("UI");

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);
        if (soundPanel != null)
            soundPanel.SetActive(false);

        if (controlsPanelUI != null)
            controlsPanelUI.menuPanel = pauseMenuPanel;
        if (soundPanelUI != null)
            soundPanelUI.menuPanel = pauseMenuPanel;

        if (pauseMenuButtons != null && pauseMenuButtons.Count >= 5)
        {
            pauseMenuButtons[0].onClick.AddListener(ResumeGame);
            pauseMenuButtons[1].onClick.AddListener(OnControlsButton);
            pauseMenuButtons[2].onClick.AddListener(OnSoundButton);
            pauseMenuButtons[3].onClick.AddListener(OnSaveButton);
            pauseMenuButtons[4].onClick.AddListener(OnQuitButton);
        }

        if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[0].gameObject);

        Time.timeScale = 0f;
    }

    private void OnEnable()
    {
        currentIndex = 0;
        if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[0].gameObject);

        if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[0] != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[0].gameObject);

        if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && selectionArrow != null)
        {
            selectionArrow.gameObject.SetActive(true);
            selectionArrow.position = pauseMenuButtons[currentIndex].transform.position + Vector3.left * 150f;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");
    }

    private void Update()
    {
        bool moved = false;
        if (Time.unscaledTime - lastNavTime > navCooldown)
        {
            float nav = 0f;
            if (Gamepad.current != null)
                nav = Gamepad.current.leftStick.ReadValue().y;

            if (nav > 0.5f && currentIndex > 0)
            {
                currentIndex--;
                moved = true;
            }
            else if (nav < -0.5f && currentIndex < pauseMenuButtons.Count - 1)
            {
                currentIndex++;
                moved = true;
            }

            if (moved)
            {
                lastNavTime = Time.unscaledTime;
                if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[currentIndex] != null && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[currentIndex].gameObject);
            }
        }

        if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && selectionArrow != null)
        {
            selectionArrow.gameObject.SetActive(true);
            selectionArrow.position = pauseMenuButtons[currentIndex].transform.position + Vector3.left * 150f;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            pauseMenuButtons[currentIndex].onClick.Invoke();
        }

        if ((Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            if (controlsPanel != null && controlsPanel.activeSelf)
            {
                controlsPanel.SetActive(false);
                pauseMenuPanel.SetActive(true);
                if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[currentIndex] != null && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[currentIndex].gameObject);
            }
            else if (soundPanel != null && soundPanel.activeSelf)
            {
                soundPanel.SetActive(false);
                pauseMenuPanel.SetActive(true);
                if (pauseMenuButtons != null && pauseMenuButtons.Count > 0 && pauseMenuButtons[currentIndex] != null && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[currentIndex].gameObject);
            }
            else
            {
                ResumeGame();
            }
        }
    }


    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");
        if (selectionArrow != null)
            selectionArrow.gameObject.SetActive(false);
        SceneManager.UnloadSceneAsync("PauseMenu");
    }

    public void OnControlsButton()
    {
        if (controlsPanelUI != null)
            controlsPanelUI.menuPanel = pauseMenuPanel;
        pauseMenuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void OnSoundButton()
    {
        if (soundPanelUI != null)
            soundPanelUI.menuPanel = pauseMenuPanel;
        pauseMenuPanel.SetActive(false);
        soundPanel.SetActive(true);

        if (soundPanelUI != null && soundPanelUI.musicSlider != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(soundPanelUI.musicSlider.gameObject);
    }


    public void OnSaveButton()
    {
        GameManager.Instance.SaveGame();
    }

    public void OnQuitButton()
    {
        Time.timeScale = 1f;
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");
        if (selectionArrow != null)
            selectionArrow.gameObject.SetActive(false);
        Application.Quit();
    }
}

