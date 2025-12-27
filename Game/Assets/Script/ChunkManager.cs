using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [Header("Configuration Chunks")]
    public Transform player;
    public int chunkSize = 50;           // Taille d'un chunk en mètres
    public int loadRadius = 2;           // Nombre de chunks à charger autour du joueur (2 = 5x5 chunks)
    
    [Header("Paramètres Monde")]
    public float villageRadius = 15f;
    public int seed = 12345;
    public bool useRandomSeed = true;
    
    [Header("Biomes (ORDRE IMPORTANT: Water, Fire, Earth, Air)")]
    public BiomeProfile biomeWater;
    public BiomeProfile biomeFire;
    public BiomeProfile biomeEarth;
    public BiomeProfile biomeAir;
    
    [Header("Rendu")]
    public Material terrainMaterial; // Matériau pour le terrain (doit supporter les Vertex Colors)
    
    [Header("Positions des Donjons")]
    public Vector3 dungeonFire = new Vector3(60, 0, -60);
    public Vector3 dungeonWater = new Vector3(-60, 0, -60);
    public Vector3 dungeonEarth = new Vector3(60, 0, 60);
    public Vector3 dungeonAir = new Vector3(-60, 0, 60);
    
    // Système de chunks
    private Dictionary<Vector2Int, WorldChunk> activeChunks = new Dictionary<Vector2Int, WorldChunk>();
    private Vector2Int lastPlayerChunk;
    private Dictionary<BiomeProfile.BiomeType, Vector3> dungeonPositions;
    private BiomeProfile[] biomesArray;
    
    void Start()
    {
        // Initialisation seed
        if (useRandomSeed) seed = Random.Range(0, 100000);
        Random.InitState(seed);
        
        // Configuration de l'array des biomes dans l'ordre attendu par WorldChunk
        biomesArray = new BiomeProfile[] { biomeWater, biomeFire, biomeEarth, biomeAir };
        
        // Configuration des positions de donjons
        dungeonPositions = new Dictionary<BiomeProfile.BiomeType, Vector3>
        {
            { BiomeProfile.BiomeType.Fire, dungeonFire },
            { BiomeProfile.BiomeType.Water, dungeonWater },
            { BiomeProfile.BiomeType.Earth, dungeonEarth },
            { BiomeProfile.BiomeType.Air, dungeonAir }
        };
        
        // Création du matériau par défaut si non assigné
        if (terrainMaterial == null)
        {            
            // Utilise le shader Particles/Standard Unlit qui supporte nativement les Vertex Colors
            Shader vertexColorShader = Shader.Find("Particles/Standard Unlit");
            
            if (vertexColorShader != null)
            {
                terrainMaterial = new Material(vertexColorShader);
                terrainMaterial.name = "Auto_TerrainVertexColor";
                terrainMaterial.SetFloat("_Mode", 0); // Opaque
                terrainMaterial.SetInt("_ColorMode", 0); // Multiply (utilise vertex colors)
            }
            else
            {
                // Fallback : Sprites/Default fonctionne aussi
                vertexColorShader = Shader.Find("Sprites/Default");
                if (vertexColorShader != null)
                {
                    terrainMaterial = new Material(vertexColorShader);
                }
            }
        }
        
        // Validation des types de biomes
        if (biomeWater.type != BiomeProfile.BiomeType.Water ||
            biomeFire.type != BiomeProfile.BiomeType.Fire ||
            biomeEarth.type != BiomeProfile.BiomeType.Earth ||
            biomeAir.type != BiomeProfile.BiomeType.Air)

        // Chargement initial
        lastPlayerChunk = GetChunkCoord(player.position);
        UpdateChunks();
        }
    
    void Update()
    {
        if (player == null) return;
        
        Vector2Int currentChunk = GetChunkCoord(player.position);
        
        // Si le joueur change de chunk, on met à jour
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }
    
    Vector2Int GetChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
    
    void UpdateChunks()
    {
        // 1. CHARGE les nouveaux chunks proches
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int z = -loadRadius; z <= loadRadius; z++)
            {
                Vector2Int coord = lastPlayerChunk + new Vector2Int(x, z);
                
                // Vérifie si le chunk n'est pas entièrement dans le village
                if (!IsChunkInVillage(coord) && !activeChunks.ContainsKey(coord))
                {
                    LoadChunk(coord);
                }
            }
        }
        
        // 2. DÉCHARGE les chunks trop loin
        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var coord in activeChunks.Keys)
        {
            float distance = Vector2Int.Distance(coord, lastPlayerChunk);
            if (distance > loadRadius + 1) // +1 pour éviter le clignotement
            {
                toUnload.Add(coord);
            }
        }
        
        foreach (var coord in toUnload)
        {
            UnloadChunk(coord);
        }
        
    }
    
    bool IsChunkInVillage(Vector2Int chunkCoord)
    {
        // Calcule le centre du chunk
        Vector3 chunkCenter = new Vector3(
            chunkCoord.x * chunkSize + chunkSize / 2f,
            0,
            chunkCoord.y * chunkSize + chunkSize / 2f
        );
        
        // Calcule la distance du coin le plus éloigné du centre du village
        float maxDistInChunk = Vector2.Distance(Vector2.zero, new Vector2(chunkCenter.x, chunkCenter.z)) + (chunkSize * 0.707f); // 0.707 = diagonale
        
        // Si même le coin le plus éloigné est dans le village, on ne charge pas ce chunk
        return maxDistInChunk < villageRadius;
    }
    
    void LoadChunk(Vector2Int coord)
    {
        // Création de l'objet chunk
        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkObj.transform.parent = transform;
        
        // IMPORTANT : Positionne le chunk à son emplacement mondial
        Vector3 chunkWorldPosition = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunkObj.transform.position = chunkWorldPosition;
        
        // Ajout du composant WorldChunk
        WorldChunk chunk = chunkObj.AddComponent<WorldChunk>();
        
        // Configure le matériau AVANT l'initialisation
        MeshRenderer renderer = chunkObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = terrainMaterial;
        }
        
        // Calcul de l'offset mondial du chunk (pour les calculs internes)
        Vector2 offset = new Vector2(coord.x * chunkSize, coord.y * chunkSize);
        
        // Initialisation du chunk
        chunk.Initialize(
            size: chunkSize,
            offset: offset,
            seed: seed,
            biomes: biomesArray,
            villageRad: villageRadius,
            dPos: dungeonPositions
        );
        
        // Enregistrement
        activeChunks[coord] = chunk;
    }
    
    void UnloadChunk(Vector2Int coord)
    {
        if (activeChunks.TryGetValue(coord, out WorldChunk chunk))
        {
            Destroy(chunk.gameObject);
            activeChunks.Remove(coord);
        }
    }
    
    void OnDrawGizmos()
    {
        if (player == null) return;
        
        Vector2Int playerChunk = GetChunkCoord(player.position);
        
        // Dessine les chunks chargés (contour vert)
        Gizmos.color = Color.green;
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int z = -loadRadius; z <= loadRadius; z++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, z);
                Vector3 center = new Vector3(
                    coord.x * chunkSize + chunkSize / 2f,
                    0,
                    coord.y * chunkSize + chunkSize / 2f
                );
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 1, chunkSize));
            }
        }
        
        // Dessine le chunk actuel du joueur (plus épais)
        Gizmos.color = Color.cyan;
        Vector3 playerChunkCenter = new Vector3(
            playerChunk.x * chunkSize + chunkSize / 2f,
            0,
            playerChunk.y * chunkSize + chunkSize / 2f
        );
        Gizmos.DrawWireCube(playerChunkCenter, new Vector3(chunkSize, 2, chunkSize));
        
        // Dessine le village
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Vector3.zero, villageRadius);
        
        // Dessine les donjons avec leurs noms
        if (dungeonPositions != null)
        {
            foreach (var entry in dungeonPositions)
            {
                switch (entry.Key)
                {
                    case BiomeProfile.BiomeType.Fire:
                        Gizmos.color = Color.red;
                        break;
                    case BiomeProfile.BiomeType.Water:
                        Gizmos.color = Color.blue;
                        break;
                    case BiomeProfile.BiomeType.Earth:
                        Gizmos.color = new Color(0.6f, 0.4f, 0.2f); // Marron
                        break;
                    case BiomeProfile.BiomeType.Air:
                        Gizmos.color = Color.white;
                        break;
                }
                Gizmos.DrawWireSphere(entry.Value, 5f);
                Gizmos.DrawLine(entry.Value, entry.Value + Vector3.up * 10f);
            }
        }
    }
}
