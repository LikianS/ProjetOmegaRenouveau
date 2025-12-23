using UnityEngine;

public class ClickablePart : MonoBehaviour, IInteractable
{
    public enum PartType { Crest, Weapon }
    public PartType partType;

    private ArmorSlot armorSlot;
    public PuzzleManager puzzleManager;

    void Start()
    {
        armorSlot = GetComponentInParent<ArmorSlot>();
    }

    public void Interact(InteractionType type)
    {
        if (armorSlot == null) return;

        if (type == InteractionType.Crest && partType == PartType.Crest)
        {
            armorSlot.NextCrest();
            Debug.Log("Blason changé");
        }
        else if (type == InteractionType.Weapon && partType == PartType.Weapon)
        {
            armorSlot.NextWeapon();
            Debug.Log("Arme changée");
        }
        else
        {
            return;
        }

        puzzleManager.CheckSolution();
    }
}
