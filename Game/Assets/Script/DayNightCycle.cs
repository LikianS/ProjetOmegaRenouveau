using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Temps")]
    [Tooltip("Durée d'une journée complète en secondes")]
    public float dayDuration = 120f;
    
    [Range(0, 1)] 
    public float timeOfDay = 0.25f;

    [Header("Lumière & Ambiance")]
    public Light sunLight;
    public Gradient sunColor;
    public Gradient skyColor;
    public Gradient equatorColor;
    public Gradient fogColor;

    [Header("Réglages Soleil")]
    public float maxIntensity = 1.5f;
    public float minIntensity = 0.1f;

    private void Update()
    {
        timeOfDay += Time.deltaTime / dayDuration;
        if (timeOfDay >= 1) timeOfDay -= 1;

        float sunAngle = (timeOfDay * 360f) - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0);

        CheckLighting();
    }

    void CheckLighting()
    {
        sunLight.color = sunColor.Evaluate(timeOfDay);

        RenderSettings.ambientSkyColor = skyColor.Evaluate(timeOfDay);
        RenderSettings.ambientEquatorColor = equatorColor.Evaluate(timeOfDay);
        RenderSettings.fogColor = fogColor.Evaluate(timeOfDay);

        float intensityMultiplier = 1;
        
        if (timeOfDay <= 0.23f || timeOfDay >= 0.75f) 
            intensityMultiplier = 0;
        else if (timeOfDay <= 0.25f) 
            intensityMultiplier = Mathf.Clamp01((timeOfDay - 0.23f) * 50);
        else if (timeOfDay >= 0.73f) 
            intensityMultiplier = Mathf.Clamp01((0.75f - timeOfDay) * 50);
        if (sunLight.intensity > 0.01f)
            sunLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, intensityMultiplier);
        if (timeOfDay < 0.2f || timeOfDay > 0.8f) 
        {
            if (sunLight.shadows != LightShadows.None) 
                sunLight.shadows = LightShadows.None;
        }
        else 
        {
            if (sunLight.shadows == LightShadows.None) 
                sunLight.shadows = LightShadows.Soft;
        }
    }
}