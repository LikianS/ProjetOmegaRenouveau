using UnityEngine;

public class ArmorTrigger : MonoBehaviour
{
    public ArmorSlot armorSlot;

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractionArmor player = other.GetComponentInParent<PlayerInteractionArmor>();
        if (player != null)
            player.currentArmorSlot = armorSlot;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractionArmor player = other.GetComponentInParent<PlayerInteractionArmor>();
        if (player != null && player.currentArmorSlot == armorSlot)
            player.currentArmorSlot = null;
    }
}
