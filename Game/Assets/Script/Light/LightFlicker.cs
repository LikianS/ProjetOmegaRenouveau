using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    public Light flickerLight;
    public float minIntensity = 0.8f;
    public float maxIntensity = 2.0f;
    public float minFlickerSpeed = 0.05f;
    public float maxFlickerSpeed = 0.3f;

    [Header("Ambiance dynamique")]
    public Color coldColor = new Color(0.5f, 0.6f, 1f);
    public Color warmColor = new Color(1f, 0.85f, 0.6f);
    public float coldIntensity = 0.8f;
    public float warmIntensity = 2.0f;

    public float coldFlickerAmount = 0.5f; 
    public float warmFlickerAmount = 0.05f;
    public float coldFlickerSpeed = 0.3f;
    public float warmFlickerSpeed = 0.8f;

    [Header("Post Processing")]
    public Volume postProcessVolume;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private float timer;
    private float nextFlickerTime;
    private int totalPickups = -1;

    void Start()
    {
        if (flickerLight == null)
        {
            flickerLight = GetComponent<Light>();
        }
        SetNextFlickerTime();

        totalPickups = FindObjectsByType<AchievementPickup>(FindObjectsSortMode.None).Length;
        if (totalPickups == 0) totalPickups = 1;

        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out colorAdjustments);
            postProcessVolume.profile.TryGet(out vignette);
        }
    }

    void Update()
    {
        int remaining = FindObjectsByType<AchievementPickup>(FindObjectsSortMode.None).Length;
        float t = 1 - (remaining / (float)totalPickups);

        flickerLight.color = Color.Lerp(coldColor, warmColor, t);
        float baseIntensity = Mathf.Lerp(coldIntensity, warmIntensity, t);

        float flickerAmount = Mathf.Lerp(coldFlickerAmount, warmFlickerAmount, t);
        float flickerSpeed = Mathf.Lerp(coldFlickerSpeed, warmFlickerSpeed, t);

        timer += Time.deltaTime;

        if (timer >= nextFlickerTime)
        {
            float flicker = 1f + Random.Range(-flickerAmount, flickerAmount);
            flickerLight.intensity = baseIntensity * flicker;
            timer = 0f;
            nextFlickerTime = Random.Range(flickerSpeed, flickerSpeed * 1.5f);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(-80f, 0f, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(-0.5f, 0.2f, t);
            colorAdjustments.contrast.value = Mathf.Lerp(-20f, 10f, t);
        }
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0.45f, 0.15f, t);
            vignette.smoothness.value = Mathf.Lerp(0.7f, 0.3f, t);
            vignette.color.value = Color.Lerp(new Color(0.1f, 0.1f, 0.15f), new Color(0.2f, 0.15f, 0.1f), t);
        }
    }

    void SetNextFlickerTime()
    {
        nextFlickerTime = Random.Range(minFlickerSpeed, maxFlickerSpeed);
    }
}
