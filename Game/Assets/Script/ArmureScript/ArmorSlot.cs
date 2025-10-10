using UnityEngine;

public class ArmorSlot : MonoBehaviour
{
    public GameObject crestObject; // Le cercle/blason
    public GameObject weaponObject; // La forme d’arme

    public Material[] crestMaterials; // 1 couleur par blason
    public Material[] weaponMaterials; // 1 couleur par arme

    private int crestIndex = 0;
    private int weaponIndex = 0;

    void Start()
    {
        UpdateVisuals();
    }

    public void NextCrest()
    {
        crestIndex = (crestIndex + 1) % crestMaterials.Length;
        UpdateVisuals();
    }

    public void NextWeapon()
    {
        weaponIndex = (weaponIndex + 1) % weaponMaterials.Length;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (crestObject != null && crestMaterials.Length > 0)
            crestObject.GetComponent<Renderer>().material = crestMaterials[crestIndex];

        if (weaponObject != null && weaponMaterials.Length > 0)
            weaponObject.GetComponent<Renderer>().material = weaponMaterials[weaponIndex];
    }

    public string GetCurrentCrest()
    {
        return crestMaterials[crestIndex].name;
    }

    public string GetCurrentWeapon()
    {
        return weaponMaterials[weaponIndex].name;
    }
}
