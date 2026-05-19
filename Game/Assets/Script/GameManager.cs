using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStats playerStats;
    public QuestManager questManager;
    public ShopManager shopManager;

    public float autoSaveInterval = 1f;

    private PlayerController playerController;
    private DualAnimPlayerController dualAnimPlayerController;
    private Coroutine autoSaveCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        EnsureReferences();

        SaveData saveData = SaveManager.LoadGame();

        if (NeedsDefaultSaveData(saveData))
            saveData = CreateDefaultSaveData();

        if (playerStats != null)
            playerStats.LoadPlayerStats(saveData);
        if (questManager != null)
            questManager.LoadQuestData(saveData);
        if (shopManager != null)
            shopManager.LoadShopData(saveData);

        if (autoSaveCoroutine == null)
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }

    private void OnApplicationQuit()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }

        EnsureReferences();
        SaveData saveData = BuildSaveData();
        SaveManager.SaveGame(saveData);
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            try
            {
                SaveGame();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Erreur lors de la sauvegarde automatique : " + ex.Message);
            }
        }
    }

    public void SaveGame()
    {
        EnsureReferences();
        SaveData saveData = BuildSaveData();
        SaveManager.SaveGame(saveData);
    }

    public void LoadGame()
    {
        EnsureReferences();

        SaveData saveData = SaveManager.LoadGame();

        if (playerStats != null)
            playerStats.LoadPlayerStats(saveData);
        if (questManager != null)
            questManager.LoadQuestData(saveData);
        if (shopManager != null)
            shopManager.LoadShopData(saveData);
    }

    private void OnDestroy()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }

        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (questManager == null)
            questManager = FindAnyObjectByType<QuestManager>();
        if (shopManager == null)
            shopManager = FindAnyObjectByType<ShopManager>();

        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
        if (dualAnimPlayerController == null)
            dualAnimPlayerController = FindAnyObjectByType<DualAnimPlayerController>();

        if (playerStats == null)
        {
            if (playerController != null)
                playerStats = playerController.playerStats;
            else if (dualAnimPlayerController != null)
                playerStats = dualAnimPlayerController.playerStats;
            else
                playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (playerController != null && playerController.playerStats != null)
            playerStats = playerController.playerStats;
        else if (dualAnimPlayerController != null && dualAnimPlayerController.playerStats != null)
            playerStats = dualAnimPlayerController.playerStats;
    }

    private bool NeedsDefaultSaveData(SaveData saveData)
    {
        return saveData == null || saveData.playerStats == null || saveData.weapons.Count == 0;
    }

    private SaveData CreateDefaultSaveData()
    {
        SaveData saveData = new SaveData();
        if (playerStats == null)
            return saveData;

        saveData.playerStats.maxHealth = playerStats.baseStats.maxHealth;
        saveData.playerStats.currentHealth = playerStats.baseStats.maxHealth;
        saveData.playerStats.maxStamina = playerStats.baseStats.maxStamina;
        saveData.playerStats.currentStamina = playerStats.baseStats.maxStamina;
        saveData.playerStats.movementSpeed = playerStats.baseStats.movementSpeed;
        saveData.playerStats.gold = playerStats.baseStats.gold;
        saveData.playerStats.achievement = playerStats.baseStats.achievement;

        foreach (var weapon in playerStats.availableWeapons)
        {
            saveData.weapons.Add(new WeaponSaveData
            {
                weaponName = weapon.weaponName,
                damage = weapon.damage,
                staminaCostReduction = weapon.staminaCostReduction,
                attackSpeed = weapon.attackSpeed,
                criticalHitRate = weapon.criticalHitRate,
                range = weapon.range,
                maxDamage = weapon.maxDamage,
                maxStaminaCostReduction = weapon.maxStaminaCostReduction,
                maxAttackSpeed = weapon.maxAttackSpeed,
                maxCriticalHitRate = weapon.maxCriticalHitRate
            });
        }

        return saveData;
    }

    private SaveData BuildSaveData()
    {
        SaveData saveData = new SaveData();

        if (playerStats != null)
            playerStats.SavePlayerStats(saveData);
        if (questManager != null)
            questManager.SaveQuestData(saveData);
        if (shopManager != null)
            shopManager.SaveShopData(saveData);

        saveData.weapons.Clear();
        if (playerStats != null)
        {
            foreach (var weapon in playerStats.availableWeapons)
            {
                saveData.weapons.Add(weapon.SaveWeaponStats());
            }
        }

        return saveData;
    }
}
