using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionArmor: MonoBehaviour
{
    public ArmorSlot currentArmorSlot;
    public PuzzleManager currentPuzzleManager;
    void Update()
    {

        if (Gamepad.current.leftTrigger.wasPressedThisFrame)
        {
            if (currentArmorSlot != null)
            {
                currentArmorSlot.NextWeapon();
            }
               


            currentPuzzleManager.CheckSolution();
        }

        if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            if (currentArmorSlot != null)
            { 
                currentArmorSlot.NextCrest();
            }

            currentPuzzleManager.CheckSolution();
        }

    }


}
