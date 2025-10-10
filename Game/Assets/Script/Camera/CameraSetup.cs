using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSetup : MonoBehaviour
{
    [Header("References")]
    public CinemachineFreeLook freeLookCamera;
    public CinemachineTargetGroup targetGroup;
    public Transform playerTransform;

    [Header("Settings")]
    public float horizontalSensitivity = 2f;
    public float verticalSensitivity = 2f;

    private void Awake()
    {

        freeLookCamera.Follow = targetGroup.transform;
        freeLookCamera.LookAt = targetGroup.transform;

        freeLookCamera.m_XAxis.m_MaxSpeed = 300 * horizontalSensitivity;
        freeLookCamera.m_YAxis.m_MaxSpeed = 2 * verticalSensitivity;

    }
}