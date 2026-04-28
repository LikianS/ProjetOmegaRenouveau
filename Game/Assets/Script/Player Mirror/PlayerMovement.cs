using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private float rotationX = 0f;
    private float rotationY = 0f;

    public Transform cameraTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;  // Cache et verrouille le curseur
    }

    void Update()
    {
        MovePlayer();
        RotateCamera();
    }

    void MovePlayer()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -80f, 80f);

        // Rotation cam�ra
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        // Rotation joueur (le Yaw)
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }
}
