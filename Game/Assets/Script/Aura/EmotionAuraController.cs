using UnityEngine;
using UnityEngine.InputSystem;

public class EmotionAuraController : MonoBehaviour
{
    public ParticleSystem fireParticles;
    public ParticleSystem waterParticles;
    public ParticleSystem lightParticles;
    public ParticleSystem glitchParticles;

    public Emotion currentEmotion;


    [Header("Player Renderer")]
    public Renderer playerRenderer;

    public Material normalMaterial;
    public Material glowMaterial;


    [Header("Glow Settings")]
    public Color glowColor = Color.yellow; 


    private bool isJoyGlowActive = false;
    private Material[] normalMaterialSet;
    private Material[] joyMaterialSet;

    void Start()
    {
        normalMaterialSet = new[] { normalMaterial };
        joyMaterialSet = new[] { normalMaterial, glowMaterial };
        StopAllParticles();
        SetEmotion(currentEmotion);
    }

    void Update()
    {
        if (Gamepad.current == null)
            return;

        if (Gamepad.current.dpad.up.wasPressedThisFrame)
            SetEmotion(Emotion.Colere);

        if (Gamepad.current.dpad.down.wasPressedThisFrame)
            SetEmotion(Emotion.Tristesse);

        if (Gamepad.current.dpad.right.wasPressedThisFrame)
            SetEmotion(Emotion.Joie);

        if (Gamepad.current.dpad.left.wasPressedThisFrame)
            SetEmotion(Emotion.Stress);
        if (isJoyGlowActive)
        {
            glowMaterial.SetColor("_EmissionColor", glowColor);
        }
    }


   

    void SetEmotion(Emotion emotion)
    {
        currentEmotion = emotion;

        StopAllParticles();
        SetPlayerNormal(); 

        switch (emotion)
        {
            case Emotion.Colere:
                fireParticles.Play();
                break;

            case Emotion.Tristesse:
                waterParticles.Play();
                break;

            case Emotion.Joie:
                lightParticles.Play();
                Debug.Log("Materials avant : " + playerRenderer.materials.Length);
                SetPlayerJoyGlow();
                Debug.Log("Materials apr�s : " + playerRenderer.materials.Length);

                break;

            case Emotion.Stress:
                glitchParticles.Play();
                break;

            case Emotion.None:
                // rien
                break;
        }

        Debug.Log("Emotion actuelle : " + emotion);
    }


    void StopAllParticles()
    {
        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        lightParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        glitchParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void SetPlayerNormal()
    {
        if (playerRenderer == null || normalMaterialSet == null)
            return;

        playerRenderer.materials = normalMaterialSet;

        isJoyGlowActive = false;
    }

    void SetPlayerJoyGlow()
    {
        if (playerRenderer == null || joyMaterialSet == null)
            return;

        playerRenderer.materials = joyMaterialSet;

        if (glowMaterial != null && glowMaterial.HasProperty("_EmissionColor"))
            glowMaterial.SetColor("_EmissionColor", glowColor);

        isJoyGlowActive = true;
    }

}
