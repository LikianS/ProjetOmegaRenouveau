using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float openAngle = 90f;   // Angle d'ouverture
    public float openSpeed = 2f;    // Vitesse d'ouverture
    private bool isOpening = false; // Si la porte est en train de s'ouvrir
    private Quaternion targetRotation;  // Rotation cible

    void Start()
    {
        // Initialisation de la rotation cible
        targetRotation = transform.rotation * Quaternion.Euler(0, openAngle, 0);
    }

    public void OpenDoor()
    {
        // Si la porte n'est pas déjà en train de s'ouvrir
        if (!isOpening)
        {
            isOpening = true;  // La porte commence à s'ouvrir
        }
    }

    void Update()
    {
        if (isOpening)
        {
            // Applique la rotation à la porte
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);

            // Si la porte est proche de la rotation cible
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation; // La porte est entièrement ouverte
                isOpening = false;
            }
        }
    }
}
