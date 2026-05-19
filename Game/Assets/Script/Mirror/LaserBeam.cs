using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    public LineRenderer lineRenderer;  // Le composant LineRenderer
    public int maxReflections = 5;     // Nombre maximal de rebonds
    public float maxDistance = 100f;   // Distance maximale du rayon
    public LayerMask raycastMask = ~0;
    public LayerMask mirrorLayerMask;
    public LayerMask receiverLayerMask;

    void Awake()
    {
        if (mirrorLayerMask.value == 0)
        {
            int mirrorLayer = LayerMask.NameToLayer("Mirror");
            if (mirrorLayer >= 0)
                mirrorLayerMask = 1 << mirrorLayer;
        }

        if (receiverLayerMask.value == 0)
        {
            int receiverLayer = LayerMask.NameToLayer("Receiver");
            if (receiverLayer >= 0)
                receiverLayerMask = 1 << receiverLayer;
        }
    }

    void Update()
    {
        Vector3 direction = transform.forward;  // Direction du rayon
        Vector3 currentPos = transform.position;  // Position de d�part

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, currentPos);

        for (int i = 0; i < maxReflections; i++)
        {
            Ray ray = new Ray(currentPos, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                //Debug.Log(">>> Quelque chose a �t� touch� : " + hit.collider.name);

                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);

                int hitLayerBit = 1 << hit.collider.gameObject.layer;
                if ((mirrorLayerMask.value & hitLayerBit) != 0)
                {
                    //Debug.Log(">>> Miroir d�tect� !");
                    direction = Vector3.Reflect(direction, hit.normal);
                    currentPos = hit.point;
                }
                else if ((receiverLayerMask.value & hitLayerBit) != 0)
                {
                    //Debug.Log(">>> R�cepteur touch� !");
                    if (hit.collider.TryGetComponent<Receiver>(out Receiver receiver))
                    {
                        receiver.Activate();
                    }
                    break; // on arr�te apr�s avoir touch� le receiver
                }
                else
                {
                    //Debug.Log(">>> Objet NON miroir et NON receiver d�tect� : " + hit.collider.tag);
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
