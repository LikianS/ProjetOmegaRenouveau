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
    }

    private void Start()
    {
        // Recherche dynamique des managers
        if (questManager == null)
            questManager = FindAnyObjectByType<QuestManager>();
        if (shopManager == null)
            shopManager = FindAnyObjectByType<ShopManager>();

        // Recherche PlayerStats via PlayerController ou DualAnimPlayerController
        if (playerStats == null)
        {
            playerStats = null;

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                playerStats = pc.playerStats;

            if (playerStats == null)
            {
                DualAnimPlayerController dac = FindAnyObjectByType<DualAnimPlayerController>();
                if (dac != null)
                    playerStats = dac.playerStats;
            }

            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
            }
        }

        SaveData saveData = SaveManager.LoadGame();

        if (saveData == null || saveData.playerStats == null || saveData.weapons.Count == 0)
        {
            saveData = new SaveData();

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
        }

        if (playerStats != null)
            playerStats.LoadPlayerStats(saveData);
        if (questManager != null)
            questManager.LoadQuestData(saveData);
        if (shopManager != null)
            shopManager.LoadShopData(saveData);

        StartCoroutine(AutoSaveRoutine());
    }

    private void OnApplicationQuit()
    {
        SaveData saveData = new SaveData();

        // Recherche dynamique des managers
        PlayerStats playerStats = null;
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
            playerStats = pc.playerStats;
        else
        {
            DualAnimPlayerController dac = FindAnyObjectByType<DualAnimPlayerController>();
            if (dac != null)
                playerStats = dac.playerStats;
            else
                playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (questManager == null)
            questManager = FindAnyObjectByType<QuestManager>();
        if (shopManager == null)
            shopManager = FindAnyObjectByType<ShopManager>();

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
        // Recherche dynamique des managers
        if (questManager == null)
            questManager = FindAnyObjectByType<QuestManager>();
        if (shopManager == null)
            shopManager = FindAnyObjectByType<ShopManager>();

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

        SaveManager.SaveGame(saveData);
    }

    public void LoadGame()
    {
        if (questManager == null)
            questManager = FindAnyObjectByType<QuestManager>();
        if (shopManager == null)
            shopManager = FindAnyObjectByType<ShopManager>();

        SaveData saveData = SaveManager.LoadGame();

        if (playerStats == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                playerStats = pc.playerStats;

            if (playerStats == null)
            {
                DualAnimPlayerController dac = FindAnyObjectByType<DualAnimPlayerController>();
                if (dac != null)
                    playerStats = dac.playerStats;
            }

            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
            }
        }

        if (playerStats != null)
            playerStats.LoadPlayerStats(saveData);
        if (questManager != null)
            questManager.LoadQuestData(saveData);
        if (shopManager != null)
            shopManager.LoadShopData(saveData);
    }
}
