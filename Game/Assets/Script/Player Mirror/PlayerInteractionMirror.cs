using UnityEngine;
using UnityEngine.InputSystem;
/*
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

*/

public class PlayerInteractionMirror : MonoBehaviour
{
    public Transform holdPoint;
    private GameObject heldMirror;

    // Références aux actions définies dans votre Input Action Asset
    public InputActionProperty interactAction;
    public InputActionProperty rotateAction;

    void OnEnable()
    {
        // Abonnement aux événements d'entrée
        interactAction.action.performed += OnInteract;
        rotateAction.action.performed += OnRotate;
    }

    void OnDisable()
    {
        interactAction.action.performed -= OnInteract;
        rotateAction.action.performed -= OnRotate;
    }

    private void OnInteract(InputAction.CallbackContext context)
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

    private void OnRotate(InputAction.CallbackContext context)
    {
        if (heldMirror != null)
        {
            heldMirror.GetComponent<MirrorPlacer>().Rotate();
        }
    }

    void TryPickMirror()
    {
        // Note: En VR, préférez un Raycast depuis la main plutôt qu'un OverlapSphere 
        // autour du corps pour plus de précision.
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