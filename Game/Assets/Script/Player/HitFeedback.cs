using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitFeedback : MonoBehaviour
{
    [Header("Visual Feedback")]
    public MeshRenderer characterMesh;
    public Material normalMaterial;
    public Material hitMaterial;
    public float hitFlashDuration = 0.15f;

    [Header("Camera Shake")]
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    private Material[] originalMaterials;
    private Material[] hitMaterials;

    private void Start()
    {
        if (characterMesh != null)
        {
            originalMaterials = characterMesh.materials;

            hitMaterials = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                hitMaterials[i] = hitMaterial;
            }
        }
    }

    public void PlayHitFeedback()
    {
        if (characterMesh != null)
        {
            StartCoroutine(FlashMaterial());
        }

        CameraShake cameraShake = Camera.main.GetComponent<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeDuration);
        }

        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    private IEnumerator FlashMaterial()
    {
        characterMesh.materials = hitMaterials;

        yield return new WaitForSeconds(hitFlashDuration);

        characterMesh.materials = originalMaterials;
    }
}