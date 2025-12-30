using UnityEngine;

// Attach this to a GameObject with a BoxCollider set to IsTrigger
// When the Player enters, it switches particles + player shader for the specified emotion
// It also stops previous particles and resets shader/materials before applying the new ones
public class ZoneEmotionTrigger : MonoBehaviour
{
    [Header("Zone Emotion")]
    public Emotion emotionToApply = Emotion.None;
    public bool clearOnExit = true; // Optional: restore to None on exit

    [Header("Zone Particles (optional)")]
    public ParticleSystem fireParticles;
    public ParticleSystem waterParticles;
    public ParticleSystem lightParticles;
    public ParticleSystem glitchParticles;

    [Header("Player Renderer + Materials")]
    public Renderer playerRenderer;            // Assign Player's Renderer in inspector
    public Material normalMaterial;            // Base material
    public Material glowMaterial;              // Glow/emissive material (for Joie)
    public Color glowColor = Color.yellow;     // Glow color to set on emissive

    private Emotion _currentApplied = Emotion.None;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // If playerRenderer not set, try find on player
        if (playerRenderer == null)
        {
            playerRenderer = other.GetComponentInChildren<Renderer>();
        }

        ApplyEmotion(emotionToApply);
    }

    private void ApplyEmotion(Emotion emotion)
    {
        // Stop previous zone particles
        StopAllZoneParticles();
        // Reset player materials to normal before applying new emotion
        SetPlayerNormal();

        switch (emotion)
        {
            case Emotion.Colere:
                if (fireParticles) fireParticles.Play();
                break;
            case Emotion.Tristesse:
                if (waterParticles) waterParticles.Play();
                break;
            case Emotion.Joie:
                if (lightParticles) lightParticles.Play();
                SetPlayerJoyGlow();
                break;
            case Emotion.Stress:
                if (glitchParticles) glitchParticles.Play();
                break;
            case Emotion.None:
                // keep normal
                break;
        }

        _currentApplied = emotion;

        // Broadcast to global systems if present (post-processing, enemies, etc.)
        if (GlobalEmotionManager.Instance != null)
        {
            GlobalEmotionManager.Instance.SetDungeonEmotion(emotion);
        }
    }

    private void StopAllZoneParticles()
    {
        if (fireParticles) fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (waterParticles) waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (lightParticles) lightParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (glitchParticles) glitchParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetPlayerJoyGlow()
    {
        if (playerRenderer == null)
            return;

        if (normalMaterial != null && glowMaterial != null)
        {
            playerRenderer.materials = new Material[] { normalMaterial, glowMaterial };
            // Set emissive color if the shader uses _EmissionColor
            if (glowMaterial.HasProperty("_EmissionColor"))
            {
                glowMaterial.SetColor("_EmissionColor", glowColor);
            }
        }
    }
    private void SetPlayerNormal()
    {
        if (playerRenderer == null || normalMaterial == null)
            return;

        playerRenderer.materials = new Material[] { normalMaterial };
    }
}

