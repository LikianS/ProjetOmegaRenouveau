using UnityEngine;
using UnityEngine.InputSystem;

/*
public class PlayerInteractionArmor: MonoBehaviour
{
    public ArmorSlot currentArmorSlot;
    public PuzzleManager currentPuzzleManager;
    void Update()
    {
        if (Gamepad.current == null)
            return;

        if (Gamepad.current.leftTrigger.wasPressedThisFrame)
        {
            if (currentArmorSlot != null)
            {
                currentArmorSlot.NextWeapon();
            }

            if (currentPuzzleManager != null)
                currentPuzzleManager.CheckSolution();
        }

        if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            if (currentArmorSlot != null)
            { 
                currentArmorSlot.NextCrest();
            }

            if (currentPuzzleManager != null)
                currentPuzzleManager.CheckSolution();
        }

    }


}
*/


public class PlayerInteractionArmor : MonoBehaviour
{
    public ArmorSlot currentArmorSlot;
    public PuzzleManager currentPuzzleManager;

    // On utilise InputActionProperty pour lier vos actions créées dans Unity
    public InputActionProperty interactAction; // Remplace le LeftTrigger
    public InputActionProperty rotateAction;   // Remplace le RightTrigger

    void OnEnable()
    {
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
        if (currentArmorSlot != null)
        {
            currentArmorSlot.NextWeapon();
            // Vérification après l'action
            if (currentPuzzleManager != null)
                currentPuzzleManager.CheckSolution();
        }
    }

    private void OnRotate(InputAction.CallbackContext context)
    {
        if (currentArmorSlot != null)
        {
            currentArmorSlot.NextCrest();
            // Vérification après l'action
            if (currentPuzzleManager != null)
                currentPuzzleManager.CheckSolution();
        }
    }
}