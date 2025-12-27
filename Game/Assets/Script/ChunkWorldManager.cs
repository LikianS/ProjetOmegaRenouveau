using UnityEngine;
using System.Collections.Generic;

public class ChunkWorldManager : MonoBehaviour
{
    [Header("Réglages Monde")]
    public int worldRadiusInChunks = 6;
    public int chunkSize = 16;
    public float villageRadius = 25f;
    public int seed;
    
    [Header("Assets")]
    public Material groundMaterial; 
    // public GameObject dungeonPrefab; // SUPPRIMÉ (est maintenant dans les Biomes)
    
    [Header("Biomes")]
    public BiomeProfile biomeWater;
    public BiomeProfile biomeFire;
    public BiomeProfile biomeEarth;
    public BiomeProfile biomeAir;

    private Dictionary<BiomeProfile.BiomeType, Vector3> dungeonPositions = new Dictionary<BiomeProfile.BiomeType, Vector3>();

    void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        foreach(Transform child in transform) Destroy(child.gameObject);
        if (seed == 0) seed = Random.Range(0, 10000);
        Random.InitState(seed);

        BiomeProfile[] biomes = new BiomeProfile[] { biomeWater, biomeFire, biomeEarth, biomeAir };
        CalculateDungeonPositions();

        for (int x = -worldRadiusInChunks; x <= worldRadiusInChunks; x++)
        {
            for (int z = -worldRadiusInChunks; z <= worldRadiusInChunks; z++)
            {
                CreateChunk(x, z, biomes);
            }
        }
    }

    void CalculateDungeonPositions()
    {
        dungeonPositions.Clear();
        float dist = (worldRadiusInChunks * chunkSize) * 0.8f; 
        
        AddDungeonPos(BiomeProfile.BiomeType.Water, -Mathf.PI * 0.25f, dist); 
        AddDungeonPos(BiomeProfile.BiomeType.Fire, -Mathf.PI * 0.75f, dist);  
        AddDungeonPos(BiomeProfile.BiomeType.Earth, Mathf.PI * 0.75f, dist);  
        AddDungeonPos(BiomeProfile.BiomeType.Air, Mathf.PI * 0.25f, dist);    
    }

    void AddDungeonPos(BiomeProfile.BiomeType type, float angle, float dist)
    {
        float x = Mathf.Cos(angle) * dist;
        float z = Mathf.Sin(angle) * dist;
        dungeonPositions.Add(type, new Vector3(x, 0, z)); 
    }

    void CreateChunk(int x, int z, BiomeProfile[] biomes)
    {
        GameObject chunkObj = new GameObject($"Chunk_{x}_{z}");
        chunkObj.transform.parent = this.transform;
        
        Vector2 posOffset = new Vector2(x * chunkSize, z * chunkSize);
        chunkObj.transform.position = new Vector3(posOffset.x, 0, posOffset.y);

        chunkObj.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObj.AddComponent<MeshRenderer>();
        mr.material = groundMaterial;
        chunkObj.AddComponent<MeshCollider>();

        WorldChunk chunkScript = chunkObj.AddComponent<WorldChunk>();
        
        // On ne passe plus le prefab global ici
        chunkScript.Initialize(chunkSize, posOffset, seed, biomes, villageRadius, dungeonPositions);
    }

    void OnDrawGizmos()
    {
        if (dungeonPositions != null)
        {
            foreach (var entry in dungeonPositions)
            {
                Gizmos.color = Color.red;
                // Dessine une sphère à la position du donjon
                Vector3 target = entry.Value;
                Gizmos.DrawWireSphere(target, 10f); 
                // Dessine une ligne depuis le centre du monde (0,0)
                Gizmos.DrawLine(Vector3.zero, target);
                
                // Affiche le nom (visible si tu actives les Gizmos 3D)
                // UnityEditor.Handles.Label(target + Vector3.up * 10, entry.Key.ToString());
            }
        }
    }
}