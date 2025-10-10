using UnityEngine;
using UnityEngine.UI;

public class HealthStaminaUI : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;
    public Image healthFillImage;

    [Header("Stamina UI")]
    public Slider staminaSlider;
    public Image staminaFillImage;

    public void SetMaxHealth(int maxHealth)
    {
        if (!IsValid(healthSlider) || !IsValid(healthFillImage)) return;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = healthSlider.value;
    }

    public void SetHealth(int health)
    {
        if (!IsValid(healthSlider) || !IsValid(healthFillImage)) return;
        healthSlider.value = health;
    }

    public void SetMaxStamina(float maxStamina)
    {
        if (!IsValid(staminaSlider) || !IsValid(staminaFillImage)) return;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = staminaSlider.value;
    }

    public void SetStamina(float stamina)
    {
        if (!IsValid(staminaSlider) || !IsValid(staminaFillImage)) return;
        staminaSlider.value = stamina;
    }

    public void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        SetMaxHealth(maxHealth);
        SetHealth(currentHealth);
    }

    public void HandleStaminaChanged(float currentStamina, float maxStamina)
    {
        SetMaxStamina(maxStamina);
        SetStamina(currentStamina);
    }

    private bool IsValid(Object obj)
    {
        return obj != null;
    }
}
