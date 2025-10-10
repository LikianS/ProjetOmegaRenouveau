using UnityEngine;
using Cinemachine;

public class CameraShake : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;

    private float shakeTimer;
    private float shakeIntensity;
    private CinemachineBasicMultiChannelPerlin[] noiseComponents = new CinemachineBasicMultiChannelPerlin[3];

    private void Awake()
    {
        if (freeLookCamera == null)
            freeLookCamera = FindFirstObjectByType<CinemachineFreeLook>();

        if (freeLookCamera != null)
        {
            for (int i = 0; i < 3; i++) 
            {
                noiseComponents[i] = freeLookCamera.GetRig(i).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
        }
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            if (shakeTimer <= 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (noiseComponents[i] != null)
                    {
                        noiseComponents[i].m_AmplitudeGain = 0f;
                    }
                }
            }
        }
    }

    public void ShakeCamera(float intensity, float duration)
    {
        for (int i = 0; i < 3; i++)
        {
            if (noiseComponents[i] != null)
            {
                noiseComponents[i].m_AmplitudeGain = intensity;
            }
        }

        shakeTimer = duration;
        shakeIntensity = intensity;
    }
}