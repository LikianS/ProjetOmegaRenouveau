using UnityEngine;

[CreateAssetMenu(fileName = "New Biome", menuName = "WorldGen/BiomeProfile")]
public class BiomeProfile : ScriptableObject
{
    public enum BiomeType { Fire, Water, Earth, Air }
    public BiomeType type;

    // --- NOUVEAUTÉ : ENUM POUR LE GAMEPLAY ---
    // Cela permet d'éviter de se tromper entre 0, 1 et 2
    public enum GameplayType { Enemy, Puzzle, Collectible }

    [Header("Assets Spéciaux")]
    public GameObject dungeonPrefab;
    public GameObject liquidSurfacePrefab;
    public GameObject voidZonePrefab;

    [Header("Couleurs")]
    public Color baseGroundColor = Color.white;
    public Color dungeonZoneColor = Color.black;
    public Color eventZoneColor = Color.yellow;

    [Header("Topologie")]
    public float heightMultiplier = 5f;
    public float noiseFrequency = 0.1f;

    [Header("Végétation & Props")]
    public GameObject[] trees;
    public GameObject[] bushes;
    public GameObject[] twigs;
    public GameObject[] rocks;
    [Range(0, 1)] public float vegetationDensity = 0.1f;

    [Header("Gameplay")]
    public GameObject[] enemies;      // Type Enemy
    public GameObject[] puzzles;      // Type Puzzle
    public GameObject[] collectibles; // Type Collectible
    
    [Header("Spawn Configuration")]
    [Range(0, 1)] public float enemyDensity = 0.05f; // Densité de spawn des ennemis
    public float minEnemyDistance = 10f; // Distance minimale entre ennemis
    public float minVillageDistance = 30f; // Distance minimale du village pour spawn

    // --- HELPER PROPS (On garde int pour l'instant pour la végétation) ---
    public GameObject GetRandomProp(int type) {
        GameObject[] list = (type == 0) ? trees : (type == 1) ? bushes : (type == 2) ? twigs : rocks;
        if (list == null || list.Length == 0) return null;
        return list[Random.Range(0, list.Length)];
    }

    // --- HELPER GAMEPLAY (Modifié avec l'Enum) ---
    public GameObject GetRandomGameplay(GameplayType type) {
        GameObject[] list = null;

        // On sélectionne la bonne liste proprement
        switch (type)
        {
            case GameplayType.Enemy:
                list = enemies;
                break;
            case GameplayType.Puzzle:
                list = puzzles;
                break;
            case GameplayType.Collectible:
                list = collectibles;
                break;
        }

        // Sécurité : Si la liste est vide, on ne fait rien (pas d'erreur)
        if (list == null || list.Length == 0) return null;

        return list[Random.Range(0, list.Length)];
    }
}