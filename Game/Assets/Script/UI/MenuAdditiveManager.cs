using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Cinemachine;
using System.Collections.Generic;

public class MenuAdditiveManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuPanel;
    public List<Button> menuButtons;
    public RectTransform selectionArrow;
    public GameObject controlsPanel;
    public GameObject soundPanel;

    [Header("Cameras")]
    public CinemachineVirtualCamera menuVirtualCamera;
    public CinemachineVirtualCamera gameplayVirtualCamera;

    [Header("Nom de la sc�ne menu")]
    public string menuSceneName = "MenuScene";

    private int currentIndex = 0;
    private float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    private PlayerController playerController;
    private DualAnimPlayerController dualAnimController;
    private PlayerInput playerInput;

    public ControlsPanelUI controlsPanelUI;
    public SoundPanelUI soundPanelUI;


    private void Start()
    {

        Transform menuCamPosition = GameObject.Find("MenuCamPosition")?.transform;
        if (menuCamPosition != null && menuVirtualCamera != null)
        {
            menuVirtualCamera.Follow = menuCamPosition;
            menuVirtualCamera.LookAt = menuCamPosition;
            menuVirtualCamera.transform.position = menuCamPosition.position;
            menuVirtualCamera.transform.rotation = menuCamPosition.rotation;
            var group = menuVirtualCamera.GetComponent<CinemachineTargetGroup>();
            if (group != null) group.gameObject.SetActive(false);
        }

        menuVirtualCamera.Priority = 20;
        if (gameplayVirtualCamera != null)
            gameplayVirtualCamera.Priority = 0;


        CachePlayerReferences();

        if (playerController != null)
            playerController.enabled = false;
        if (dualAnimController != null)
            dualAnimController.enabled = false;


        if (menuPanel != null)
            menuPanel.SetActive(true);

        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null && !playerInput.enabled)
            playerInput.enabled = true;

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("UI");

        HighlightButton(currentIndex);
    }

    private void Update()
    {
        Vector2 nav = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        if (Mathf.Abs(nav.y) > 0.5f && Time.time - lastInputTime > inputCooldown)
        {
            if (nav.y > 0.5f)
                MoveSelection(-1);
            else if (nav.y < -0.5f)
                MoveSelection(1);

            lastInputTime = Time.time;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            ValidateSelection();
        }
    }

    private void MoveSelection(int direction)
    {
        SoundManager.Instance.PlayUIMove();
        currentIndex = (currentIndex + direction + menuButtons.Count) % menuButtons.Count;
        HighlightButton(currentIndex);
    }

    private void HighlightButton(int index)
    {
        if (selectionArrow == null || menuButtons == null || menuButtons.Count == 0)
            return;

        selectionArrow.gameObject.SetActive(true);
        selectionArrow.position = menuButtons[index].transform.position + Vector3.left * 150f;
    }


    private void ValidateSelection()
    {
        SoundManager.Instance.PlayUIMove();
        switch (currentIndex)
        {
            case 0:
                OnPlayButton();
                break;
            case 1:
                OnControlsButton();
                break;
            case 2:
                OnResetSaveButton();
                break;
            case 3:
                OnSoundButton();
                break;
            case 4:
                OnQuitButton();
                break;

        }
    }

    public void OnPlayButton()
    {
        CachePlayerReferences();

        if (playerController != null)
            playerController.enabled = true;
        if (dualAnimController != null)
            dualAnimController.enabled = true;

        if (menuVirtualCamera != null)
            menuVirtualCamera.Priority = 0;
        if (gameplayVirtualCamera != null)
            gameplayVirtualCamera.Priority = 20;

        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null && !playerInput.enabled)
            playerInput.enabled = true;

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");

        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (selectionArrow != null)
            selectionArrow.gameObject.SetActive(false);
        SceneManager.UnloadSceneAsync(menuSceneName);
    }

    public void OnControlsButton()
    {
        if (controlsPanelUI == null)
            controlsPanelUI = controlsPanel.GetComponent<ControlsPanelUI>();
        if (controlsPanelUI != null)
            controlsPanelUI.menuPanel = menuPanel;
        ShowSubPanel(controlsPanel);
    }

    public void OnSoundButton()
    {
        if (soundPanelUI == null)
            soundPanelUI = soundPanel.GetComponent<SoundPanelUI>();
        if (soundPanelUI != null)
            soundPanelUI.menuPanel = menuPanel;
        ShowSubPanel(soundPanel);

        if (soundPanelUI != null && soundPanelUI.musicSlider != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(soundPanelUI.musicSlider.gameObject);
    }


    public void OnQuitButton()
    {
        if (selectionArrow != null)
            selectionArrow.gameObject.SetActive(false);
        Application.Quit();
    }
    public void OnResetSaveButton()
    {
        PlayerStats refStats = FindAnyObjectByType<PlayerStats>();
        if (refStats == null)
        {
            Debug.LogError("Aucun PlayerStats trouv� dans la sc�ne pour le reset !");
            return;
        }

        SaveData newSave = new SaveData();

        newSave.playerStats = new PlayerStatsData();
        newSave.playerStats.maxHealth = refStats.baseStats.maxHealth;
        newSave.playerStats.maxStamina = refStats.baseStats.maxStamina;
        newSave.playerStats.movementSpeed = refStats.baseStats.movementSpeed;
        newSave.playerStats.gold = refStats.baseStats.gold;
        newSave.playerStats.achievement = refStats.baseStats.achievement;

        newSave.playerStats.staminaRegenRate = refStats.staminaRegenRate;
        newSave.playerStats.staminaRegenDelay = refStats.staminaRegenDelay;
        newSave.playerStats.runningStaminaCost = refStats.runningStaminaCost;
        newSave.playerStats.dashStaminaCost = refStats.dashStaminaCost;
        newSave.playerStats.attackStaminaCost = refStats.attackStaminaCost;

        newSave.weapons = new List<WeaponSaveData>();
        foreach (var weapon in refStats.availableWeapons)
        {
            newSave.weapons.Add(weapon.SaveWeaponStats());
        }

        newSave.shopItems = new List<ShopItemSaveData>();
        foreach (var shopConfig in ShopManager.Instance.shopConfigurations)
        {
            foreach (var item in shopConfig.items)
            {
                var itemSave = new ShopItemSaveData();
                itemSave.itemName = item.itemName;
                itemSave.currentPurchases = 0;
                itemSave.maxPurchases = item.purchaseLimit;
                newSave.shopItems.Add(itemSave);
            }
        }

        newSave.quests = new List<QuestSaveData>();
        QuestDataScriptable[] allQuests = Resources.LoadAll<QuestDataScriptable>("Quete");
        foreach (var quest in allQuests)
        {
            var questSave = new QuestSaveData();
            questSave.questName = quest.questName;
            questSave.isStarted = false;
            questSave.isCompleted = false;
            questSave.stepsCompleted = new List<bool>();
            questSave.stepsStarted = new List<bool>();
            questSave.stepsCurrentKillCount = new List<int>();
            foreach (var step in quest.steps)
            {
                questSave.stepsCompleted.Add(false);
                questSave.stepsStarted.Add(false);
                questSave.stepsCurrentKillCount.Add(0);
            }
            newSave.quests.Add(questSave);
        }

        if (refStats.availableWeapons.Count > 0)
            newSave.equippedWeaponName = refStats.availableWeapons[0].weaponName;
        else
            newSave.equippedWeaponName = "�p�e";

        newSave.currentSceneName = SceneManager.GetActiveScene().name;

        SaveManager.SaveGame(newSave);

        foreach (var quest in allQuests)
        {
            quest.isStarted = false;
            quest.isCompleted = false;
            if (quest.steps != null)
            {
                foreach (var step in quest.steps)
                {
                    step.isStarted = false;
                    step.isCompleted = false;
                }
            }
        }
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.activeQuests.Clear(); 
            QuestManager.Instance.UpdateQuestUI();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (controlsPanel != null && controlsPanel.activeSelf)
            {
                ReturnToMainPanel(controlsPanel);
                HighlightButton(currentIndex);
            }
            else if (soundPanel != null && soundPanel.activeSelf)
            {
                ReturnToMainPanel(soundPanel);
                HighlightButton(currentIndex);
            }
            else
            {
                if (selectionArrow != null)
                    selectionArrow.gameObject.SetActive(false);
                SceneManager.UnloadSceneAsync(menuSceneName);
            }
        }
    }

    private void CachePlayerReferences()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
        if (dualAnimController == null)
            dualAnimController = FindAnyObjectByType<DualAnimPlayerController>();
    }

    private void ShowSubPanel(GameObject subPanel)
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (subPanel != null)
            subPanel.SetActive(true);
    }

    private void ReturnToMainPanel(GameObject subPanel)
    {
        if (subPanel != null)
            subPanel.SetActive(false);
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

}
