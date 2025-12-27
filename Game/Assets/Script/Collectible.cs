using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Type de Collectible")]
    [Tooltip("D�finissez le type de collectible : Item, Weapon ou StatBoost.")]
    public CollectibleType collectibleType;

    [Header("Param�tres pour les Items")]
    [Tooltip("Quantit� d'or � ajouter au joueur (si applicable).")]
    public int goldAmount = 0;

    [Tooltip("Points d'achievement � ajouter au joueur (si applicable).")]
    public int achievementPoints = 0;

    [Header("Param�tres pour les Armes")]
    [Tooltip("Nom de l'arme existante � �quiper.")]
    public string weaponNameToEquip;

    [Header("Param�tres pour les Boosts de Statistiques")]
    [Tooltip("Nom de la statistique � booster (par exemple : 'damage', 'speed').")]
    public string targetStatName;

    [Tooltip("Valeur du boost � appliquer.")]
    public float statBoostValue = 0;

    [Tooltip("Cochez cette case pour appliquer le boost au joueur. Sinon, il sera appliqu� � une arme.")]
    public bool applyToPlayer = true;

    [Tooltip("Arme sp�cifique � booster (laisser vide pour booster l'arme �quip�e).")]
    public WeaponStats targetWeapon;

    [Header("Effets Visuels et Sonores")]
    public ParticleSystem collectEffect;
    public AudioClip collectSound;
    private AudioSource audioSource;
    public string itemName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowInteraction($"Appuyer sur A pour ramasser {itemName}");
                InteractionManager.Instance.PositionInteractionUI(transform.position + Vector3.up * 1.5f);
            }

            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.SetInteractableItem(this);
            }

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.SetInteractableItem(null);
            }

            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.HideInteraction();
            }
        }
    }
    public void Collect(PlayerStats playerStats)
    {
        PlayCollectEffects();
        switch (collectibleType)
        {
            case CollectibleType.Item:
                CollectItem(playerStats);
                break;

            case CollectibleType.Weapon:
                CollectWeapon(playerStats);
                break;

            case CollectibleType.StatBoost:
                ApplyStatBoost(playerStats);
                break;
        }
        InteractionManager.Instance.HideInteraction();
        Destroy(gameObject);
    }

    private void CollectItem(PlayerStats playerStats)
    {
        playerStats.AddGold(goldAmount);
        playerStats.AddAchievement(achievementPoints);
        InteractionManager.Instance.HideInteraction();
    }

    private void CollectWeapon(PlayerStats playerStats)
    {
        if (!string.IsNullOrEmpty(weaponNameToEquip))
        {
            WeaponStats weaponToEquip = playerStats.GetWeaponByName(weaponNameToEquip);
            if (weaponToEquip != null)
            {
                playerStats.EquipWeapon(weaponToEquip);
                PlayCollectEffects();
            }
            else
            {
                Debug.LogWarning($"Aucune arme existante nomm�e {weaponNameToEquip} trouv�e.");
            }
        }
        else
        {
            Debug.LogWarning("Aucun nom d'arme sp�cifi� pour le collectible.");
        }
    }


    private void ApplyStatBoost(PlayerStats playerStats)
    {
        if (applyToPlayer)
        {
            playerStats.IncreaseStat(targetStatName, statBoostValue);
        }
        else if (targetWeapon != null)
        {
            targetWeapon.IncreaseStat(targetStatName, (int)statBoostValue);
        }
        else if (playerStats.equippedWeapon != null)
        {
            playerStats.IncreaseSpecificWeaponStat(playerStats.equippedWeapon.weaponName, targetStatName, statBoostValue);
        }
    }
    private void PlayCollectEffects()
    {
        Instantiate(collectEffect, transform.position, Quaternion.identity);
        SoundManager.Instance.PlayItemPickup();
    }

}
