using UnityEngine;

public class EnemyEmotion : MonoBehaviour
{
    public ParticleSystem fireParticles;
    public ParticleSystem waterParticles;
    public ParticleSystem lightParticles;
    public ParticleSystem glitchParticles;

    private void Start()
    {
        // Abonne le mob aux changements d'émotion du donjon
        if (GlobalEmotionManager.Instance != null)
        {
            GlobalEmotionManager.Instance.OnEmotionChanged += OnDungeonEmotionChanged;
            // Initialiser avec l'émotion actuelle du donjon
            OnDungeonEmotionChanged(GlobalEmotionManager.Instance.currentDungeonEmotion);
        }
    }

    void OnDungeonEmotionChanged(Emotion emotion)
    {
        StopAllParticles();

        switch (emotion)
        {
            case Emotion.Colere: fireParticles?.Play(); break;
            case Emotion.Tristesse: waterParticles?.Play(); break;
            case Emotion.Joie: lightParticles?.Play(); break;
            case Emotion.Stress: glitchParticles?.Play(); break;
            case Emotion.None: break; // rien
        }

        Debug.Log($"Mob '{gameObject.name}' particules mises à jour : {emotion}");
    }

    void StopAllParticles()
    {
        fireParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        waterParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        lightParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        glitchParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (GlobalEmotionManager.Instance != null)
            GlobalEmotionManager.Instance.OnEmotionChanged -= OnDungeonEmotionChanged;
    }
}
