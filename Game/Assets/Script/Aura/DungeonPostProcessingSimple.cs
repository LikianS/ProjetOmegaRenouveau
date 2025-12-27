using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DungeonPostProcessingSimple: MonoBehaviour
{
    public Volume volume;
    private ColorAdjustments colorAdj;
    private Bloom bloom;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private FilmGrain grain;

    private Emotion currentEmotion;

    void Start()
    {
        volume.profile.TryGet(out colorAdj);
        volume.profile.TryGet(out bloom);
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out grain);

        if (GlobalEmotionManager.Instance != null)
        {
            GlobalEmotionManager.Instance.OnEmotionChanged += OnDungeonEmotionChanged;
            currentEmotion = GlobalEmotionManager.Instance.currentDungeonEmotion;
            OnDungeonEmotionChanged(currentEmotion);
        }
    }

    void Update()
    {
        
        float pulse = Mathf.Sin(Time.time * 2f) * 0.1f; 

        switch (currentEmotion)
        {
            case Emotion.Colere:
                bloom.intensity.value = 1.2f + pulse;
                vignette.intensity.value = 0.4f + pulse * 0.5f;

                chromatic.intensity.value = 0.05f + pulse * 0.05f;
                break;
            case Emotion.Tristesse:
                bloom.intensity.value = 0.3f + pulse * 0.3f; 
                vignette.intensity.value = 0.6f + pulse * 0.2f; 
                grain.intensity.value = 0.4f + pulse * 0.15f; 
                break;

            case Emotion.Joie:
                float joyPulse = Mathf.Sin(Time.time * 1.5f);
                bloom.intensity.value = 1.2f + joyPulse * 0.2f;
                vignette.intensity.value = 0.05f + joyPulse * 0.02f;
                break;

            case Emotion.Stress:
                float stressPulse = Mathf.PerlinNoise(Time.time * 3f, 0f);

                bloom.intensity.value = 0.8f + stressPulse * 0.3f;

                vignette.intensity.value = 0.6f + stressPulse * 0.2f + Random.Range(-0.02f, 0.02f);
                vignette.smoothness.value = 0.4f + stressPulse * 0.2f + Random.Range(-0.05f, 0.05f);

                chromatic.intensity.value = 0.2f + stressPulse * 0.1f;
                grain.intensity.value = 0.3f + stressPulse * 0.2f;
                break;


            case Emotion.None:
                bloom.intensity.value = 0.5f;
                vignette.intensity.value = 0f;
                chromatic.intensity.value = 0f;
                grain.intensity.value = 0f;
                break;
        }
    }

    void OnDungeonEmotionChanged(Emotion emotion)
    {
        currentEmotion = emotion;
        Debug.Log("Donjon Emotion dynamique : " + emotion);
    }
}
