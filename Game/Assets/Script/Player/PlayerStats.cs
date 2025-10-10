using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class StatsData
{
    public int maxHealth = 100;
    public float maxStamina = 100f;
    public float movementSpeed = 5f;
    public int achievement = 0;
    public int gold = 0;
}

[System.Serializable]
public class QuestData
{
    public string questName;
    public bool isCompleted;
}
[Serializable]
public class WeaponStats
{
    public string weaponName;
    public int damage;
    public float staminaCostReduction;
    public float attackSpeed;
    public float criticalHitRate;
    public float range;
    public int maxDamage;
    public float maxStaminaCostReduction;
    public float maxAttackSpeed;
    public float maxCriticalHitRate;

    public WeaponStats(string name, int dmg, float staminaReduction, float atkSpeed, float critRate, float rng, int maxDmg, float maxStaminaReduction, float maxAtkSpeed, float maxCritRate)
    {
        weaponName = name;
        damage = dmg;
        staminaCostReduction = staminaReduction;
        attackSpeed = atkSpeed;
        criticalHitRate = critRate;
        range = rng;
        maxDamage = maxDmg;
        maxStaminaCostReduction = maxStaminaReduction;
        maxAttackSpeed = maxAtkSpeed;
        maxCriticalHitRate = maxCritRate;
    }
    public WeaponSaveData SaveWeaponStats()
    {
        return new WeaponSaveData
        {
            weaponName = this.weaponName,
            damage = this.damage,
            staminaCostReduction = this.staminaCostReduction,
            attackSpeed = this.attackSpeed,
            criticalHitRate = this.criticalHitRate,
            range = this.range,
            maxDamage = this.maxDamage,
            maxStaminaCostReduction = this.maxStaminaCostReduction,
            maxAttackSpeed = this.maxAttackSpeed,
            maxCriticalHitRate = this.maxCriticalHitRate
        };
    }

    public void LoadWeaponStats(WeaponSaveData saveData)
    {
        weaponName = saveData.weaponName;
        damage = saveData.damage;
        staminaCostReduction = saveData.staminaCostReduction;
        attackSpeed = saveData.attackSpeed;
        criticalHitRate = saveData.criticalHitRate;
        range = saveData.range;
        maxDamage = saveData.maxDamage;
        maxStaminaCostReduction = saveData.maxStaminaCostReduction;
        maxAttackSpeed = saveData.maxAttackSpeed;
        maxCriticalHitRate = saveData.maxCriticalHitRate;

    }

    public void IncreaseStat(string statName, int value)
    {
        switch (statName.ToLower())
        {
            case "damage":
                damage += value;
                break;
            case "attackspeed":
                attackSpeed += value;
                break;
            case "criticalhitrate":
                criticalHitRate += value / 100f;
                break;
            case "staminacostreduction":
                staminaCostReduction += value / 100f;
                break;
        }
    }
}

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public StatsData baseStats = new StatsData();

    [Header("Available Weapons")]
    public List<WeaponStats> availableWeapons = new List<WeaponStats>();
    public WeaponStats equippedWeapon;

    [Header("Stamina Settings")]
    public float staminaRegenRate = 10f;
    public float staminaRegenDelay = 1f;
    public float runningStaminaCost = 10f;
    public float dashStaminaCost = 20f;
    public float attackStaminaCost = 15f;

    public float currentHealth;
    public float currentStamina;
    private float lastStaminaUseTime;

    [Header("Health Regeneration")]
    public float healthRegenRate = 5f;
    public float healthRegenDelay = 5f;

    private float lastDamageTime;

    public HealthStaminaUI uiManager;

    public event Action<int, int> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;


    public int CurrentHealth => Mathf.RoundToInt(currentHealth);
    public float CurrentStamina => currentStamina;

    public float AttackSpeedMultiplier => equippedWeapon != null ? equippedWeapon.attackSpeed : 1f;
    public float SpeedMultiplier => 1f + (baseStats.movementSpeed - 1f) * 0.05f;
    public float DamageMultiplier => equippedWeapon != null ? equippedWeapon.damage : 1f;
    private void Start()
    {
        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<HealthStaminaUI>();
            if (uiManager == null)
            {
                Debug.LogError("uiManager n'est pas assigné dans PlayerStats et aucun HealthStaminaUI trouvé dans la scène !");
            }
        }

        if (uiManager != null)
        {
            OnHealthChanged += uiManager.HandleHealthChanged;
            OnStaminaChanged += uiManager.HandleStaminaChanged;
        }
        GameManager.Instance.LoadGame();

    }

    private void Awake()
    {
        currentHealth = baseStats.maxHealth;
        currentStamina = baseStats.maxStamina;
        InitializeWeapons();
    }
    private void Update()
    {
        if (!IsDead)
        {
            RegenerateStamina();
            RegenerateHealth();
        }
    }

    private void OnApplicationQuit()
    {
        GameManager.Instance.SaveGame();
    }

    /*private void OnApplicationPause(bool pause)
    {
        if (false)
        {
            GameManager.Instance.SaveGame();
        }
    }*/
    public WeaponStats GetWeaponByName(string weaponName)
    {
        return availableWeapons.Find(w => w.weaponName == weaponName);
    }
    private void InitializeWeapons()
    {
        availableWeapons.Add(new WeaponStats("Grande Hache", 50, 0.1f, 0.8f, 0.2f, 3f, 100, 0.3f, 1.2f, 0.5f));
        availableWeapons.Add(new WeaponStats("Arc", 30, 0.2f, 1.5f, 0.3f, 10f, 60, 0.4f, 2f, 0.6f));
        availableWeapons.Add(new WeaponStats("Épée", 40, 0.15f, 1f, 0.25f, 2f, 80, 0.35f, 1.5f, 0.5f));
        availableWeapons.Add(new WeaponStats("Dague", 25, 0.3f, 2f, 0.4f, 1.5f, 50, 0.5f, 2.5f, 0.7f));
        equippedWeapon = availableWeapons[2];
    }

    public void IncreaseStat(string statName, float amount)
    {
        switch (statName.ToLower())
        {
            case "health":
                baseStats.maxHealth = Mathf.RoundToInt(baseStats.maxHealth + amount);
                break;
            case "stamina":
                baseStats.maxStamina = Mathf.Min(200f, baseStats.maxStamina + amount);
                break;
            case "movementspeed":
                baseStats.movementSpeed = Mathf.Min(10f, baseStats.movementSpeed + amount);
                break;
            case "achievement":
                baseStats.achievement = Mathf.RoundToInt(baseStats.achievement + amount);
                break;
        }
        GameManager.Instance.SaveGame();
        UpdatePlayerStatsUI();
    }


    public void IncreaseSpecificWeaponStat(string weaponName, string statName, float amount)
    {
        WeaponStats weapon = availableWeapons.Find(w => w.weaponName == weaponName);
        if (weapon == null) return;

        switch (statName.ToLower())
        {
            case "damage":
                weapon.damage = Mathf.Min(weapon.maxDamage, weapon.damage + Mathf.RoundToInt(amount));
                break;
            case "staminacostreduction":
                weapon.staminaCostReduction = Mathf.Min(weapon.maxStaminaCostReduction, weapon.staminaCostReduction + amount);
                break;
            case "attackspeed":
                weapon.attackSpeed = Mathf.Min(weapon.maxAttackSpeed, weapon.attackSpeed + amount);
                break;
            case "criticalhitrate":
                weapon.criticalHitRate = Mathf.Min(weapon.maxCriticalHitRate, weapon.criticalHitRate + amount);
                break;
        }
        GameManager.Instance.SaveGame();
    }
    private void RegenerateStamina()
    {
        if (Time.time - lastStaminaUseTime >= staminaRegenDelay)
        {
            currentStamina = Mathf.Min(baseStats.maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, baseStats.maxStamina);
        }
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(currentStamina, baseStats.maxStamina);
    }
    private void RegenerateHealth()
    {
        if (Time.time - lastDamageTime >= healthRegenDelay)
        {
            currentHealth = Mathf.Min(baseStats.maxHealth, currentHealth + healthRegenRate * Time.deltaTime);
            OnHealthChanged?.Invoke(Mathf.RoundToInt(currentHealth), baseStats.maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        lastDamageTime = Time.time;
        OnHealthChanged?.Invoke(Mathf.RoundToInt(currentHealth), baseStats.maxHealth);
    }

    public void EquipWeapon(WeaponStats weapon)
    {
        equippedWeapon = weapon;
        Debug.Log($"Arme équipée : {weapon.weaponName}");
        UpdatePlayerStatsUI();
        GameManager.Instance.SaveGame();
    }
    public void UpdatePlayerStatsUI()
    {
        uiManager.SetMaxHealth(baseStats.maxHealth);
        uiManager.SetHealth(Mathf.RoundToInt(currentHealth));
        uiManager.SetMaxStamina(baseStats.maxStamina);
        uiManager.SetStamina(currentStamina);
    }

    public void SavePlayerStats(SaveData saveData)
    {
        saveData.playerStats.maxHealth = baseStats.maxHealth;
        saveData.playerStats.currentHealth = Mathf.RoundToInt(currentHealth);
        saveData.playerStats.maxStamina = baseStats.maxStamina;
        saveData.playerStats.currentStamina = CurrentStamina;
        saveData.playerStats.movementSpeed = baseStats.movementSpeed;
        saveData.playerStats.gold = baseStats.gold;
        saveData.playerStats.achievement = baseStats.achievement;

        saveData.playerStats.staminaRegenRate = staminaRegenRate;
        saveData.playerStats.staminaRegenDelay = staminaRegenDelay;
        saveData.playerStats.runningStaminaCost = runningStaminaCost;
        saveData.playerStats.dashStaminaCost = dashStaminaCost;
        saveData.playerStats.attackStaminaCost = attackStaminaCost;

        saveData.weapons.Clear();
        foreach (var weapon in availableWeapons)
        {
            saveData.weapons.Add(weapon.SaveWeaponStats());
        }
        saveData.equippedWeaponName = equippedWeapon != null ? equippedWeapon.weaponName : null;
    }


    public void LoadPlayerStats(SaveData saveData)
    {
        baseStats.maxHealth = saveData.playerStats.maxHealth > 0 ? saveData.playerStats.maxHealth : baseStats.maxHealth;
        currentHealth = saveData.playerStats.currentHealth > 0 ? saveData.playerStats.currentHealth : baseStats.maxHealth;
        baseStats.maxStamina = saveData.playerStats.maxStamina > 0 ? saveData.playerStats.maxStamina : baseStats.maxStamina;
        currentStamina = saveData.playerStats.currentStamina > 0 ? saveData.playerStats.currentStamina : baseStats.maxStamina;
        baseStats.movementSpeed = saveData.playerStats.movementSpeed > 0 ? saveData.playerStats.movementSpeed : baseStats.movementSpeed;
        baseStats.gold = saveData.playerStats.gold > 0 ? saveData.playerStats.gold : baseStats.gold;
        baseStats.achievement = saveData.playerStats.achievement > 0 ? saveData.playerStats.achievement : baseStats.achievement;

        staminaRegenRate = saveData.playerStats.staminaRegenRate > 0 ? saveData.playerStats.staminaRegenRate : staminaRegenRate;
        staminaRegenDelay = saveData.playerStats.staminaRegenDelay > 0 ? saveData.playerStats.staminaRegenDelay : staminaRegenDelay;
        runningStaminaCost = saveData.playerStats.runningStaminaCost > 0 ? saveData.playerStats.runningStaminaCost : runningStaminaCost;
        dashStaminaCost = saveData.playerStats.dashStaminaCost > 0 ? saveData.playerStats.dashStaminaCost : dashStaminaCost;
        attackStaminaCost = saveData.playerStats.attackStaminaCost > 0 ? saveData.playerStats.attackStaminaCost : attackStaminaCost;

        foreach (var weapon in saveData.weapons)
        {
            WeaponStats existingWeapon = availableWeapons.Find(w => w.weaponName == weapon.weaponName);

            if (existingWeapon != null)
            {
                existingWeapon.damage = weapon.damage;
                existingWeapon.staminaCostReduction = weapon.staminaCostReduction;
                existingWeapon.attackSpeed = weapon.attackSpeed;
                existingWeapon.criticalHitRate = weapon.criticalHitRate;
                existingWeapon.range = weapon.range;
                existingWeapon.maxDamage = weapon.maxDamage;
                existingWeapon.maxStaminaCostReduction = weapon.maxStaminaCostReduction;
                existingWeapon.maxAttackSpeed = weapon.maxAttackSpeed;
                existingWeapon.maxCriticalHitRate = weapon.maxCriticalHitRate;
            }
        }

        if (!string.IsNullOrEmpty(saveData.equippedWeaponName))
        {
            equippedWeapon = availableWeapons.Find(w => w.weaponName == saveData.equippedWeaponName);
        }
        UpdatePlayerStatsUI();
    }
    public bool IsDead
    {
        get { return currentHealth <= 0; }
    }

    public void AddGold(int amount)
    {
        baseStats.gold += amount;
    }

    public void AddAchievement(int points)
    {
        baseStats.achievement += points;
    }

    private void OnDestroy()
    {
        if (uiManager != null)
        {
            OnHealthChanged -= uiManager.HandleHealthChanged;
            OnStaminaChanged -= uiManager.HandleStaminaChanged;
        }
    }

}
