using UnityEngine;

public class MirrorPlacer : MonoBehaviour
{
    public float rotationStep = 45f;
    private int currentRotationStep = 0;
    private bool isHeld = false;
    private Transform holdPoint;

    void Update()
    {
        if (isHeld && holdPoint != null)
        {
            
            Vector3 rawPos = holdPoint.position + holdPoint.forward * 0.1f;

            
            float gridSize = 0.5f; 
            Vector3 snappedPos = rawPos;
            snappedPos.x = Mathf.Round(snappedPos.x / gridSize) * gridSize;
            snappedPos.z = Mathf.Round(snappedPos.z / gridSize) * gridSize;
            snappedPos.y = 0.9f; 

            transform.position = snappedPos;

            
            transform.rotation = Quaternion.Euler(0, currentRotationStep * rotationStep, 0);
        }
    }


    public void PickUp(Transform playerHoldPoint)
    {
        isHeld = true;
        holdPoint = playerHoldPoint;
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
    }

    

    public void TryPlace()
    {
        transform.SetParent(null);
        isHeld = false;
    }



    public void Rotate()
    {
        currentRotationStep = (currentRotationStep + 1) % 8;
        transform.rotation = Quaternion.Euler(0, currentRotationStep * rotationStep, 0);
    }
}
