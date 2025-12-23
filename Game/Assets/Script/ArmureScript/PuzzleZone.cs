using UnityEngine;

public class PuzzleZone : MonoBehaviour
{
    public PuzzleManager puzzleManager;

    private void OnTriggerEnter(Collider other)
    {

        PlayerInteractionArmor player = other.GetComponentInParent<PlayerInteractionArmor>();
        if (player != null)
        {
            
            player.currentPuzzleManager = puzzleManager;
            player.currentArmorSlot = null;
        }
    }


    void OnTriggerExit(Collider other)
    {
        PlayerInteractionArmor player = other.GetComponentInParent<PlayerInteractionArmor>();
        if (player != null)
        {
            if (player.currentPuzzleManager == puzzleManager)
            {
                player.currentPuzzleManager = null;
                player.currentArmorSlot = null;
            }
        }
    }
}
