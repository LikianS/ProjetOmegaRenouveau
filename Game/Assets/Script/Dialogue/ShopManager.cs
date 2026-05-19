using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using UnityEngine.InputSystem;

[Serializable]
public class ShopItem
{
    public string itemName;
    public string description;
    public int price;
    public ItemType itemType;
    public int value;
    public Sprite icon;
    public string targetStatName;
    public float priceIncreaseFactor = 1.1f;
    public int statIncreaseValue = 1;
    public int purchaseLimit = 0; 
    public int currentPurchases = 0;

    public enum ItemType
    {
        BaseStatUpgrade,
        SpecificWeaponUpgrade
    }

    public void ApplyEffect(PlayerStats playerStats, WeaponStats targetWeapon = null)
    {
        switch (itemType)
        {
            case ItemType.BaseStatUpgrade:
                playerStats.IncreaseStat(targetStatName, value);
                break;

            case ItemType.SpecificWeaponUpgrade:
                targetWeapon.IncreaseStat(targetStatName, value);
                break;

            default:
                Debug.LogWarning($"Effet non géré pour l'item : {itemName}");
                break;
        }

        price = Mathf.CeilToInt(price * priceIncreaseFactor);
        value += statIncreaseValue;
        currentPurchases++;
    }

    public bool CanBePurchased()
    {
        return purchaseLimit == 0 || currentPurchases < purchaseLimit;
    }
}
public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;
    public Transform itemsContainer;
    public GameObject shopItemPrefab;
    public TextMeshProUGUI shopTitleText;
    public TextMeshProUGUI playerMoneyText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI previewText;
    public GameObject selectionArrow;
    public TextMeshProUGUI feedbackText;
    public GameObject statsPanel;

    [Header("Shops Configuration")]
    public List<ShopConfiguration> shopConfigurations = new List<ShopConfiguration>();

    [Header("Audio Settings")]
    public AudioClip errorSound;
    private AudioSource audioSource; 

    private PlayerStats playerStats;
    private List<GameObject> shopItemObjects = new List<GameObject>();
    private Dictionary<GameObject, ShopItem> shopItemMapping = new Dictionary<GameObject, ShopItem>();
    private int currentItemIndex = 0;
    private float inputCooldown = 0.2f;
    private float lastInputTime = 0f;

    private PlayerInput playerInput;
    private PlayerController playerController;
    private DualAnimPlayerController dualAnimPlayerController;

    public static ShopManager Instance { get; private set; }

    [Serializable]
    public class ShopConfiguration
    {
        public string shopType;
        public string shopTitle;
        public TargetType targetType;
        public string targetWeaponName; 
        public List<ShopItem> items = new List<ShopItem>();

        public enum TargetType
        {
            PlayerStats,
            WeaponStats
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        playerInput = FindAnyObjectByType<PlayerInput>();
        CachePlayerReferences();
        shopPanel.SetActive(false);
        statsText.text = "";
        previewText.text = "";

        if (selectionArrow != null)
        {
            selectionArrow.SetActive(false);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void OpenShop(string shopType, string weaponName = null)
    {
        CachePlayerReferences();

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Shop");
        if (playerController != null)
            playerController.SetDialogueMode(false);
        if (dualAnimPlayerController != null)
            dualAnimPlayerController.SetDialogueMode(false);

        ShopConfiguration shopConfig = shopConfigurations.Find(config => config.shopType == shopType);
        if (shopConfig == null)
        {
            Debug.LogError($"Shop de type '{shopType}' introuvable !");
            return;
        }

        if (playerController != null)
            playerController.SetDialogueMode(true);
        if (dualAnimPlayerController != null)
            dualAnimPlayerController.SetDialogueMode(true);

        if (shopConfig.targetType == ShopConfiguration.TargetType.WeaponStats && !string.IsNullOrEmpty(weaponName))
        {
            SetShopWeapon(weaponName);
        }

        shopTitleText.text = shopConfig.shopTitle;
        DisplayShopItems(shopConfig.items);
        UpdateMoneyDisplay();
        shopPanel.SetActive(true);
        statsPanel.SetActive(true);

        currentItemIndex = -1;
        if (selectionArrow != null)
            selectionArrow.SetActive(false);

        SoundManager.Instance.PlayUIOpen();
    }

    private void DisplayShopItems(List<ShopItem> items)
    {
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        shopItemObjects.Clear();
        shopItemMapping.Clear();

        foreach (var item in items)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, itemsContainer);
            ConfigureShopItemUI(itemObj, item);
            shopItemObjects.Add(itemObj);
            shopItemMapping[itemObj] = item;
        }

        currentItemIndex = 0;
        HighlightShopItem(currentItemIndex);
    }

    private void ConfigureShopItemUI(GameObject itemObject, ShopItem item)
    {
        Image iconImage = itemObject.transform.Find("ItemIcon").GetComponent<Image>();
        TextMeshProUGUI nameText = itemObject.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descText = itemObject.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = itemObject.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        Button buyButton = itemObject.transform.Find("BuyButton").GetComponent<Button>();
        GameObject overlay = itemObject.transform.Find("Overlay").gameObject;

        iconImage.sprite = item.icon;
        nameText.text = item.itemName;
        descText.text = item.description;
        priceText.text = $"{item.price} gold";
        if (!item.CanBePurchased())
        {
            buyButton.interactable = false;
            overlay.SetActive(true);
        }
        else
        {
            buyButton.interactable = true;
            overlay.SetActive(false); 
        }

        buyButton.onClick.AddListener(() => BuyItem(item));

        EventTrigger trigger = itemObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entry.callback.AddListener((eventData) => UpdateStatsUI(item));
        trigger.triggers.Add(entry);
    }

    private void HighlightShopItem(int index)
    {
        if (selectionArrow != null && index >= 0 && index < shopItemObjects.Count)
        {
            selectionArrow.SetActive(true);
            RectTransform itemRect = shopItemObjects[index].GetComponent<RectTransform>();
            RectTransform arrowRect = selectionArrow.GetComponent<RectTransform>();

            Vector3 newPos = new Vector3(
                itemRect.position.x - (itemRect.rect.width/2) - (arrowRect.rect.width/2) -1000,
                itemRect.position.y,
                itemRect.position.z
            );
            arrowRect.position = newPos;

            if (shopItemMapping.TryGetValue(shopItemObjects[index], out ShopItem selectedItem))
            {
                UpdateStatsUI(selectedItem);
            }
        }
        else
        {
            selectionArrow.SetActive(false);
        }
    }


    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!shopPanel.activeSelf || context.ReadValue<float>() <= 0)
            return;

        if (currentItemIndex >= 0 && currentItemIndex < shopItemObjects.Count)
        {
            GameObject selectedItemObject = shopItemObjects[currentItemIndex];
            if (shopItemMapping.TryGetValue(selectedItemObject, out ShopItem item))
            {
                BuyItem(item);
            }
            else
            {
                Debug.LogError("L'item sélectionné n'est pas associé à un ShopItem.");
            }
        }
    }

    public void CloseShop()
    {
        CachePlayerReferences();

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");
        shopPanel.SetActive(false);
        selectionArrow.SetActive(false);
        statsPanel.SetActive(false);

        if (playerController != null)
            playerController.SetDialogueMode(false);
        if (dualAnimPlayerController != null)
            dualAnimPlayerController.SetDialogueMode(false);

        SoundManager.Instance.PlayUIClose();
    }

    public void BuyItem(ShopItem item)
    {
        if (!item.CanBePurchased())
        {
            feedbackText.text = "Limite d'achat atteinte pour cet item !";
            SoundManager.Instance.PlayShopRefuse();
            StartCoroutine(HideFeedbackAfterDelay(2.0f));
            return;
        }

        if (playerStats.baseStats.gold < item.price)
        {
            feedbackText.text = "Pas assez d'or !";

            SoundManager.Instance.PlayShopRefuse();
            StartCoroutine(HideFeedbackAfterDelay(2.0f));
            return;
        }

        playerStats.baseStats.gold -= item.price;

        ShopConfiguration currentShop = shopConfigurations.Find(config => config.shopType == shopTitleText.text);
        if (currentShop == null)
        {
            Debug.LogError("Aucune configuration de shop trouvée.");
            return;
        }

        WeaponStats targetWeapon = null;
        if (currentShop.targetType == ShopConfiguration.TargetType.WeaponStats)
        {
            targetWeapon = playerStats.availableWeapons.Find(w => w.weaponName == currentShop.targetWeaponName);
            if (targetWeapon == null)
            {
                feedbackText.text = $"L'arme '{currentShop.targetWeaponName}' n'a pas été trouvée.";
                return;
            }
        }

        item.ApplyEffect(playerStats, targetWeapon);

        feedbackText.text = $"Vous avez acheté {item.itemName} !";
        SoundManager.Instance.PlayShopRefuse();
        StartCoroutine(HideFeedbackAfterDelay(2.0f));
        UpdateMoneyDisplay();
        UpdateShopItemUI(item);
        UpdateStatsUI(item);
        playerStats.UpdatePlayerStatsUI();
        if (shopItemMapping.TryGetValue(shopItemObjects[currentItemIndex], out ShopItem updatedItem))
        {
            GameObject itemObject = shopItemObjects[currentItemIndex];
            ConfigureShopItemUI(itemObject, updatedItem);
        }
    }
    private IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackText.text = ""; 
    }

    private void UpdateMoneyDisplay()
    {
        playerMoneyText.text = $"Gold: {playerStats.baseStats.gold}";
    }

    private void UpdateStatsUI(ShopItem item)
    {
        if (item == null)
        {
            statsText.text = "Aucune statistique disponible.";
            previewText.text = "";
            return;
        }

        ShopConfiguration currentShop = shopConfigurations.Find(config => config.shopType == shopTitleText.text);
        if (currentShop == null)
        {
            statsText.text = "Aucune configuration de shop trouvée.";
            previewText.text = "";
            return;
        }

        if (currentShop.targetType == ShopConfiguration.TargetType.WeaponStats)
        {
            WeaponStats weapon = playerStats.availableWeapons.Find(w => w.weaponName == currentShop.targetWeaponName);
            if (weapon == null)
            {
                statsText.text = "Aucune arme associée à ce shop.";
                previewText.text = "";
                return;
            }

            statsText.text = $"<b>Statistiques de l'arme : {weapon.weaponName}</b>\n" +
                             $"Dégâts : {weapon.damage}\n" +
                             $"Réduction coût endurance : {weapon.staminaCostReduction * 100}%\n" +
                             $"Vitesse d'attaque : {weapon.attackSpeed}\n" +
                             $"Taux de critique : {weapon.criticalHitRate * 100}%\n\n";

            string statName = item.targetStatName.ToLower();
            float newValue = 0;

            switch (statName)
            {
                case "damage":
                    newValue = Mathf.Min(weapon.maxDamage, weapon.damage + item.value);
                    previewText.text = $"<b>Impact :</b>\nDégâts : {weapon.damage} → {newValue}";
                    break;
                case "staminacostreduction":
                    newValue = Mathf.Min(weapon.maxStaminaCostReduction, weapon.staminaCostReduction + item.value);
                    previewText.text = $"<b>Impact :</b>\nRéduction coût endurance : {weapon.staminaCostReduction * 100}% → {newValue * 100}%";
                    break;
                case "attackspeed":
                    newValue = Mathf.Min(weapon.maxAttackSpeed, weapon.attackSpeed + item.value);
                    previewText.text = $"<b>Impact :</b>\nVitesse d'attaque : {weapon.attackSpeed} → {newValue}";
                    break;
                case "criticalhitrate":
                    newValue = Mathf.Min(weapon.maxCriticalHitRate, weapon.criticalHitRate + item.value);
                    previewText.text = $"<b>Impact :</b>\nTaux de critique : {weapon.criticalHitRate * 100}% → {newValue * 100}%";
                    break;
                default:
                    previewText.text = "Aucun impact détecté.";
                    break;
            }
        }
        else if (currentShop.targetType == ShopConfiguration.TargetType.PlayerStats)
        {
            statsText.text = $"<b>Statistiques actuelles du joueur :</b>\n" +
                             $"Santé : {playerStats.baseStats.maxHealth}\n" +
                             $"Endurance : {playerStats.baseStats.maxStamina}\n" +
                             $"Vitesse : {playerStats.baseStats.movementSpeed}\n\n";

            string statName = item.targetStatName.ToLower();
            float newValue = 0;

            switch (statName)
            {
                case "health":
                    newValue = playerStats.baseStats.maxHealth + item.value;
                    previewText.text = $"<b>Impact :</b>\nSanté : {playerStats.baseStats.maxHealth} → {newValue}";
                    break;
                case "stamina":
                    newValue = playerStats.baseStats.maxStamina + item.value;
                    previewText.text = $"<b>Impact :</b>\nEndurance : {playerStats.baseStats.maxStamina} → {newValue}";
                    break;
                case "movementspeed":
                    newValue = playerStats.baseStats.movementSpeed + item.value;
                    previewText.text = $"<b>Impact :</b>\nVitesse : {playerStats.baseStats.movementSpeed} → {newValue}";
                    break;
                default:
                    previewText.text = "Aucun impact détecté.";
                    break;
            }
        }
        else
        {
            statsText.text = "Aucune cible valide définie pour ce shop.";
            previewText.text = "";
        }
    }


    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            CloseShop();
        }
    }
    public void SetShopWeapon(string weaponName)
    {
        ShopConfiguration currentShop = shopConfigurations.Find(config => config.shopType == shopTitleText.text);
        if (currentShop == null)
        {
            Debug.LogError("Aucune configuration de shop trouvée.");
            return;
        }

        WeaponStats weapon = playerStats.availableWeapons.Find(w => w.weaponName == weaponName);
        if (weapon == null)
        {
            Debug.LogError($"L'arme '{weaponName}' n'a pas été trouvée dans les armes disponibles du joueur.");
            return;
        }

        currentShop.targetWeaponName = weaponName;
        Debug.Log($"L'arme '{weapon.weaponName}' a été définie pour le shop '{currentShop.shopTitle}'.");
    }
    private void UpdateShopItemUI(ShopItem item)
    {
        foreach (var kvp in shopItemMapping)
        {
            if (kvp.Value == item)
            {
                GameObject itemObject = kvp.Key;

                TextMeshProUGUI priceText = itemObject.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
                priceText.text = $"{item.price} gold";

                TextMeshProUGUI descText = itemObject.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>();
                descText.text = item.description;

                break;
            }
        }
    }
    public void SaveShopData(SaveData saveData)
    {
        saveData.shopItems.Clear();
        foreach (var shopConfig in shopConfigurations)
        {
            foreach (var item in shopConfig.items)
            {
                saveData.shopItems.Add(new ShopItemSaveData
                {
                    itemName = item.itemName,
                    currentPurchases = item.currentPurchases,
                    maxPurchases = item.purchaseLimit
                });
            }
        }
    }

    public void LoadShopData(SaveData saveData)
    {
        foreach (var shopItemSave in saveData.shopItems)
        {
            foreach (var shopConfig in shopConfigurations)
            {
                ShopItem item = shopConfig.items.Find(i => i.itemName == shopItemSave.itemName);
                if (item != null)
                {
                    item.currentPurchases = shopItemSave.currentPurchases;
                    item.purchaseLimit = shopItemSave.maxPurchases;
                }
            }
        }
    }

    public void OnShopNavigate(InputAction.CallbackContext context)
    {
        if (!shopPanel.activeSelf)
            return;

        if (Time.time - lastInputTime < inputCooldown)
            return;

        Vector2 input = context.ReadValue<Vector2>();
        if (input.y > 0.7f)
        {
            currentItemIndex = (currentItemIndex - 1 + shopItemObjects.Count) % shopItemObjects.Count;
            HighlightShopItem(currentItemIndex);
            lastInputTime = Time.time;
            SoundManager.Instance.PlayUIMove();
        }
        else if (input.y < -0.7f)
        {
            currentItemIndex = (currentItemIndex + 1) % shopItemObjects.Count;
            HighlightShopItem(currentItemIndex);
            lastInputTime = Time.time;
            SoundManager.Instance.PlayUIMove();
        }
    }

    public void OnShopBuy(InputAction.CallbackContext context)
    {
        if (!shopPanel.activeSelf)
            return;

        if (context.performed && currentItemIndex >= 0 && currentItemIndex < shopItemObjects.Count)
        {
            GameObject selectedItemObject = shopItemObjects[currentItemIndex];
            if (shopItemMapping.TryGetValue(selectedItemObject, out ShopItem item))
            {
                BuyItem(item);
            }
        }
    }

    public void OnShopCancel(InputAction.CallbackContext context)
    {
        if (!shopPanel.activeSelf)
            return;

        if (context.performed)
        {
            CloseShop();
        }
    }

    private void CachePlayerReferences()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (dualAnimPlayerController == null)
            dualAnimPlayerController = FindAnyObjectByType<DualAnimPlayerController>();

        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();

        if (playerInput == null)
            playerInput = FindAnyObjectByType<PlayerInput>();
    }

}
