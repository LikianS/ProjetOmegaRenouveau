using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerStatsUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject statsPanel;
    public TextMeshProUGUI playerStatsText;
    public TextMeshProUGUI weaponStatsText;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        statsPanel.SetActive(false);
    }

    public void OnToggleStats(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleStatsPanel();
        }
    }

    private void ToggleStatsPanel()
    {
        if (statsPanel.activeSelf)
        {
            statsPanel.SetActive(false);
        }
        else
        {
            UpdateStatsUI();
            statsPanel.SetActive(true);
        }
    }

    private void UpdateStatsUI()
    {
        if (playerStats != null)
        {
            playerStatsText.text = $"<b>Player Stats</b>\n" +
                                   $"Health: {playerStats.baseStats.maxHealth}\n" +
                                   $"Stamina: {playerStats.baseStats.maxStamina}\n" +
                                   $"Speed: {playerStats.baseStats.movementSpeed}\n" +
                                   $"Achievements: {playerStats.baseStats.achievement}\n" +
                                   $"Gold: {playerStats.baseStats.gold}";
        }

        if (playerStats.equippedWeapon != null)
        {
            weaponStatsText.text = $"<b>Weapon Stats</b>\n" +
                                   $"Name: {playerStats.equippedWeapon.weaponName}\n" +
                                   $"Damage: {playerStats.equippedWeapon.damage}\n" +
                                   $"Stamina Cost Reduction: {playerStats.equippedWeapon.staminaCostReduction * 100}%\n" +
                                   $"Attack Speed: {playerStats.equippedWeapon.attackSpeed}\n" +
                                   $"Critical Hit Rate: {playerStats.equippedWeapon.criticalHitRate * 100}%\n" +
                                   $"Range: {playerStats.equippedWeapon.range}";
        }
        else
        {
            weaponStatsText.text = "<b>Weapon Stats</b>\nNo weapon equipped.";
        }
    }
}