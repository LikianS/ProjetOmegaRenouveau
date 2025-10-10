using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public PlayerStatsData playerStats = new PlayerStatsData();
    public List<WeaponSaveData> weapons = new List<WeaponSaveData>();
    public List<QuestSaveData> quests = new List<QuestSaveData>();
    public List<ShopItemSaveData> shopItems = new List<ShopItemSaveData>();

    public string equippedWeaponName;

    public string currentSceneName;
}

[System.Serializable]
public class PlayerStatsData
{
    public int maxHealth;
    public int currentHealth;
    public float maxStamina;
    public float currentStamina;
    public float movementSpeed;
    public int gold;
    public int achievement;

    public float staminaRegenRate;
    public float staminaRegenDelay;
    public float runningStaminaCost;
    public float dashStaminaCost;
    public float attackStaminaCost;
}

[System.Serializable]
public class WeaponSaveData
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
}

[System.Serializable]
public class QuestSaveData
{
    public string questName;
    public bool isStarted;
    public bool isCompleted;
    public List<bool> stepsCompleted = new List<bool>();
    public List<bool> stepsStarted = new List<bool>();
    public List<int> stepsCurrentKillCount = new List<int>();

}

[System.Serializable]
public class ShopItemSaveData
{
    public string itemName;
    public int currentPurchases;
    public int maxPurchases;
}
