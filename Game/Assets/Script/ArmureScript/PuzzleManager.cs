using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public ArmorSlot[] armorSlots; // Liste des armures
    public string[] correctCrests; // Solution des blasons
    public string[] correctWeapons; // Solution des armes

    public GameObject successEffect; // Effet à jouer si la solution est correcte
    public DoorController[] doors;


    public void CheckSolution()
    {
        bool allCorrect = true;  // Variable pour vérifier si tout est correct

        // Vérification de chaque armure
        for (int i = 0; i < armorSlots.Length; i++)
        {

            // Vérifier si le blason et l'arme sont corrects
            if (armorSlots[i].GetCurrentCrest() != correctCrests[i] || armorSlots[i].GetCurrentWeapon() != correctWeapons[i])
            {
                Debug.Log("Erreur dans l'armure " + armorSlots[i]); // Log de l'erreur
                allCorrect = false;  // On indique qu'une erreur est trouvée
            }
        }

        // Si tous les éléments sont corrects, solution résolue
        if (allCorrect)
        {
            Debug.Log("Énigme résolue !");
            foreach (DoorController door in doors)
            {
                door.OpenDoor();
            }

            // Déclencher un effet de succès (porte qui s'ouvre, son, etc.)
            if (successEffect != null)
            {
                successEffect.SetActive(true);
            }
        }
        else
        {
            Debug.Log("Solution incorrecte, réessayez.");
        }
    }

}
