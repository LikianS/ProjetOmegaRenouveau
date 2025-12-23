using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public ArmorSlot[] armorSlots; 
    public string[] correctCrests;
    public string[] correctWeapons; 

    public GameObject successEffect; 
    public DoorController[] doors;


    public void CheckSolution()
    {
        bool allCorrect = true;  

       
        for (int i = 0; i < armorSlots.Length; i++)
        {

            
            if (armorSlots[i].GetCurrentCrest() != correctCrests[i] || armorSlots[i].GetCurrentWeapon() != correctWeapons[i])
            {
                //Debug.Log("Erreur dans l'armure " + armorSlots[i]); 
                allCorrect = false;  
            }
        }

        
        if (allCorrect)
        {
            //Debug.Log("Énigme résolue !");
            foreach (DoorController door in doors)
            {
                door.OpenDoor();
            }

            
            if (successEffect != null)
            {
                successEffect.SetActive(true);
            }
        }
        else
        {
            //Debug.Log("Solution incorrecte, réessayez.");
        }
    }

}
