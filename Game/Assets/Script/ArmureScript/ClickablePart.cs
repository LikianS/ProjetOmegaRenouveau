using UnityEngine;

public class ClickablePart : MonoBehaviour
{
    public enum PartType { Crest, Weapon }
    public PartType partType;

    private ArmorSlot armorSlot;
    public PuzzleManager puzzleManager;

    void Start()
    {
        armorSlot = GetComponentInParent<ArmorSlot>();
    }

    void OnMouseDown()
    {
        if (armorSlot == null) return;

        if (partType == PartType.Crest)
        {
            armorSlot.NextCrest();
            Debug.Log("Clic sur Blason : " + armorSlot.GetCurrentCrest());
        }
        else if (partType == PartType.Weapon)
        {
            armorSlot.NextWeapon();
            Debug.Log("Clic sur Arme : " + armorSlot.GetCurrentWeapon());
        }


        if (puzzleManager != null)
        {
            puzzleManager.CheckSolution();
        }
    }

}
