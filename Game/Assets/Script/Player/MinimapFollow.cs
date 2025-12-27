using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [Header("Configuration")]
    public Transform player;      // Glisse ton joueur ici
    public float height = 20f;    // Hauteur de la caméra au-dessus du sol

    [Header("Options")]
    public bool rotateWithPlayer = false; // Est-ce que la carte doit tourner ?

    void LateUpdate()
    {
        if (player == null) return;

        // On crée une nouvelle position basée sur le joueur
        Vector3 newPosition = player.position;
        
        // On force la hauteur (Y) pour que la caméra ne monte/descende pas avec les sauts
        newPosition.y = height;

        // On applique la position
        transform.position = newPosition;

        if (rotateWithPlayer)
        {
            // La carte tourne selon l'axe Y du joueur
            transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        }
        else
        {
            // Le Nord reste toujours en haut
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}