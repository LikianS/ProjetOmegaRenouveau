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
        ApplySliderMaxValue(healthSlider, healthFillImage, maxHealth);
    }

    public void SetHealth(int health)
    {
        ApplySliderValue(healthSlider, healthFillImage, health);
    }

    public void SetMaxStamina(float maxStamina)
    {
        ApplySliderMaxValue(staminaSlider, staminaFillImage, maxStamina);
    }

    public void SetStamina(float stamina)
    {
        ApplySliderValue(staminaSlider, staminaFillImage, stamina);
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

    private void ApplySliderMaxValue(Slider slider, Image fillImage, float maxValue)
    {
        if (slider == null || fillImage == null) return;
        slider.maxValue = maxValue;
        slider.value = slider.value;
    }

    private void ApplySliderValue(Slider slider, Image fillImage, float value)
    {
        if (slider == null || fillImage == null) return;
        slider.value = value;
    }
}
