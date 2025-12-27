using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionMirror : MonoBehaviour
{
    public Transform holdPoint;
    private GameObject heldMirror;

    void Update()
    {
        if (Gamepad.current.leftTrigger.wasPressedThisFrame)
        {
            if (heldMirror == null)
            {
                TryPickMirror();
            }
            else
            {
                heldMirror.GetComponent<MirrorPlacer>().TryPlace();
                heldMirror = null;
            }
        }

        if (heldMirror != null && Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            heldMirror.GetComponent<MirrorPlacer>().Rotate();
        }
    }

    void TryPickMirror()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (Collider hit in hits)
        {
            MirrorPlacer mirror = hit.GetComponent<MirrorPlacer>();
            if (mirror != null)
            {
                heldMirror = hit.gameObject;
                mirror.PickUp(holdPoint);
                break;
            }
        }
    }
}
