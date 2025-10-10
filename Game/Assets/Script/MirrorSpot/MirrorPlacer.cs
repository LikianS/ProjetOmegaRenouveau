using UnityEngine;

public class MirrorPlacer : MonoBehaviour
{
    private bool isHeld = false;
    private Transform holdPoint;
    private int currentRotationStep = 0;
    public float rotationStep = 45f;

    void Start()
    {
        // Crée un point fictif devant la caméra pour tenir le miroir
        GameObject holder = new GameObject("HoldPoint");
        holder.transform.SetParent(Camera.main.transform);
        holder.transform.localPosition = new Vector3(0, -0.2f, 2f); // légèrement en dessous du centre
        holdPoint = holder.transform;
    }

    void Update()
    {
        if (isHeld)
        {
            // Suivre la position du point de tenue
            transform.position = holdPoint.position;
            transform.rotation = holdPoint.rotation;

            // Rotation avec la touche R
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentRotationStep = (currentRotationStep + 1) % 8;
                float newY = currentRotationStep * rotationStep;
                transform.rotation = Quaternion.Euler(0, newY, 0);
            }

            // Déposer avec clic gauche
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceMirror();
            }
        }
    }

    void OnMouseDown()
    {
        if (!isHeld)
        {
            isHeld = true;
        }
    }

    void TryPlaceMirror()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            if (hit.collider.CompareTag("MirrorSpot"))
            {
                // Snapping : position et alignement
                transform.position = hit.collider.transform.position + Vector3.up * 0.9f;
                transform.rotation = Quaternion.Euler(0, currentRotationStep * rotationStep, 0);
                isHeld = false;
            }
            else
            {
                Debug.Log("Pas un emplacement valide.");
            }
        }
        else
        {
            Debug.Log(" Rien sous le miroir.");
        }
    }
}
