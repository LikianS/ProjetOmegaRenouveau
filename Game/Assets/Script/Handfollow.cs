using UnityEngine;

public class HandTargetFollow : MonoBehaviour
{
    public Transform controller;

    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        transform.position =
            controller.position +
            controller.TransformDirection(positionOffset);

        transform.rotation =
            controller.rotation *
            Quaternion.Euler(rotationOffset);
    }
}