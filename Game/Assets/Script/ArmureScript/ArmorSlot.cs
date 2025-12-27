using UnityEngine;

public class ArmorSlot : MonoBehaviour
{
    [Header("Blason")]
    public GameObject crestObject;
    public Material[] crestMaterials;
    private int crestIndex = 0;

    [Header("Armes")]
    public GameObject[] weaponPrefabs;   
    public WeaponType[] weaponTypes;     
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
        weaponIndex = (weaponIndex + 1) % weaponPrefabs.Length;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        
        if (crestObject != null)
            crestObject.GetComponent<Renderer>().material = crestMaterials[crestIndex];

        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            weaponPrefabs[i].SetActive(i == weaponIndex);
        }
    }

    public string GetCurrentCrest()
    {
        return crestMaterials[crestIndex].name;
    }

    public WeaponType GetCurrentWeapon()
    {
        return weaponTypes[weaponIndex];
    }
}
