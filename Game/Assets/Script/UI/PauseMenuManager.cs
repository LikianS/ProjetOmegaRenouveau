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
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    private void Start()
    {
        currentIndex = 0;
        SelectCurrentButton();

        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
            CacheUIActions();
        }

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
        SelectCurrentButton();

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
            if (navigateAction != null)
                nav = navigateAction.ReadValue<Vector2>().y;

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
                SelectCurrentButton();
            }
        }

        UpdateSelectionArrow();

        if (submitAction != null && submitAction.WasPressedThisFrame())
            InvokeCurrentButton();

        if (cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            if (controlsPanel != null && controlsPanel.activeSelf)
                ReturnToMainMenu(controlsPanel);
            else if (soundPanel != null && soundPanel.activeSelf)
                ReturnToMainMenu(soundPanel);
            else
                ResumeGame();
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
        ShowSubPanel(controlsPanel);
    }

    public void OnSoundButton()
    {
        if (soundPanelUI != null)
            soundPanelUI.menuPanel = pauseMenuPanel;
        ShowSubPanel(soundPanel);

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

    private void ShowSubPanel(GameObject panel)
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (panel != null)
            panel.SetActive(true);
    }

    private void ReturnToMainMenu(GameObject subPanel)
    {
        if (subPanel != null)
            subPanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        SelectCurrentButton();
    }

    private void UpdateSelectionArrow()
    {
        if (pauseMenuButtons == null || pauseMenuButtons.Count == 0 || selectionArrow == null)
            return;

        selectionArrow.gameObject.SetActive(true);
        selectionArrow.position = pauseMenuButtons[currentIndex].transform.position + Vector3.left * 150f;
    }

    private void SelectCurrentButton()
    {
        if (pauseMenuButtons == null || pauseMenuButtons.Count == 0)
            return;

        if (pauseMenuButtons[currentIndex] != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(pauseMenuButtons[currentIndex].gameObject);
    }

    private void InvokeCurrentButton()
    {
        if (pauseMenuButtons == null || pauseMenuButtons.Count == 0 || pauseMenuButtons[currentIndex] == null)
            return;

        pauseMenuButtons[currentIndex].onClick.Invoke();
    }

    private void CacheUIActions()
    {
        if (playerInput == null || playerInput.actions == null) return;

        var uiMap = playerInput.actions.FindActionMap("UI", false);
        if (uiMap == null) return;

        navigateAction = uiMap.FindAction("Navigate", false);
        submitAction = uiMap.FindAction("Submit", false);
        cancelAction = uiMap.FindAction("Cancel", false);
    }
}

