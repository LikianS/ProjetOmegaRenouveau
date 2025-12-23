using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    public LineRenderer lineRenderer;  // Le composant LineRenderer
    public int maxReflections = 5;     // Nombre maximal de rebonds
    public float maxDistance = 100f;   // Distance maximale du rayon

    void Update()
    {
        Vector3 direction = transform.forward;  // Direction du rayon
        Vector3 currentPos = transform.position;  // Position de départ

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, currentPos);

        for (int i = 0; i < maxReflections; i++)
        {
            Ray ray = new Ray(currentPos, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                //Debug.Log(">>> Quelque chose a été touché : " + hit.collider.name);

                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);

                if (hit.collider.CompareTag("Mirror"))
                {
                    //Debug.Log(">>> Miroir détecté !");
                    direction = Vector3.Reflect(direction, hit.normal);
                    currentPos = hit.point;
                }
                else if (hit.collider.CompareTag("Receiver"))
                {
                    //Debug.Log(">>> Récepteur touché !");
                    Receiver receiver = hit.collider.GetComponent<Receiver>();
                    if (receiver != null)
                    {
                        receiver.Activate();
                    }
                    break; // on arrête après avoir touché le receiver
                }
                else
                {
                    //Debug.Log(">>> Objet NON miroir et NON receiver détecté : " + hit.collider.tag);
                    break;
                }
            }
            else
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, currentPos + direction * maxDistance);
                break;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
