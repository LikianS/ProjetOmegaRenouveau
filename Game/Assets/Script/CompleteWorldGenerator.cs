using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CompleteWorldGenerator : MonoBehaviour
{
    [Header("Settings Monde")]
    public int worldRadius = 60;
    public float villageRadius = 15f;
    public int seed;
    public bool useRandomSeed = true;

    [Header("Profils de Biomes")]
    public BiomeProfile biomeFire;
    public BiomeProfile biomeWater;
    public BiomeProfile biomeEarth;
    public BiomeProfile biomeAir;

    [Header("Donjons")]
    public GameObject dungeonPrefab;

    // Listes pour construire le Mesh
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Color> colors = new List<Color>();
    
    // Conteneur pour les props (pour ne pas polluer la hiérarchie)
    Transform propContainer;

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (useRandomSeed) seed = Random.Range(0, 100000);
        Random.InitState(seed);

        // 1. Nettoyage
        if(propContainer) Destroy(propContainer.gameObject);
        propContainer = new GameObject("Props_Container").transform;
        propContainer.parent = transform;

        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        // 2. Boucle de génération (On ne crée pas d'objets, on calcule des points)
        int vertIndex = 0;

        for (int x = -worldRadius; x < worldRadius; x++)
        {
            for (int z = -worldRadius; z < worldRadius; z++)
            {
                // Position centrale de la tuile
                Vector3 pos = new Vector3(x + 0.5f, 0, z + 0.5f);
                float dist = Vector3.Distance(Vector3.zero, pos);

                // Trous (Village ou Limite monde)
                if (dist < villageRadius || dist > worldRadius) continue;

                // --- LOGIQUE BIOME (Même qu'avant) ---
                float angle = Mathf.Atan2(z, x);
                angle += Mathf.PerlinNoise(x * 0.1f + seed, z * 0.1f + seed) * 0.5f;
                BiomeProfile biome = GetBiome(angle);

                // --- LOGIQUE EVENT / HAUTEUR ---
                float eventNoise = Mathf.PerlinNoise(x * 0.15f + seed + 500, z * 0.15f + seed + 500);
                bool isDungeonZone = dist > worldRadius * 0.85f;
                bool isEventZone = eventNoise > 0.7f && !isDungeonZone;

                // Hauteur simple (Perlin)
                float y = Mathf.PerlinNoise(x * 0.1f + seed, z * 0.1f + seed) * 2f;
                if (biome == biomeWater) y = -0.5f; // Eau plus basse

                // --- CONSTRUCTION DU MESH (Le carré) ---
                // On ajoute 4 sommets pour faire un carré
                Vector3 p0 = new Vector3(x, y, z);
                Vector3 p1 = new Vector3(x, y, z + 1);
                Vector3 p2 = new Vector3(x + 1, y, z + 1);
                Vector3 p3 = new Vector3(x + 1, y, z);

                vertices.Add(p0); vertices.Add(p1); vertices.Add(p2); vertices.Add(p3);

                // On ajoute les 2 triangles qui forment le carré
                triangles.Add(vertIndex + 0);
                triangles.Add(vertIndex + 1);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 0);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 3);

                vertIndex += 4;

                // --- COULEURS ---
                Color c = biome.baseGroundColor;
                if (isDungeonZone) c = biome.dungeonZoneColor;
                else if (isEventZone) c = biome.eventZoneColor;
                
                // On applique la couleur aux 4 sommets
                colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);

                // --- PROPS (Arbres, etc.) ---
                // On instancie les props car il y en a beaucoup moins que de tuiles de sol
                if (!isDungeonZone)
                {
                    SpawnProps(new Vector3(x + 0.5f, y, z + 0.5f), biome, isEventZone);
                }
            }
        }

        // 3. FINALISATION DU MESH
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray(); // Applique les couleurs
        mesh.RecalculateNormals(); // Pour que la lumière réagisse bien

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh; // Physique optimisée
        
        // 4. SPAWN DONJONS (Logic inchangée)
        SpawnDungeons();
        
    }

    void SpawnProps(Vector3 pos, BiomeProfile biome, bool isEvent)
    {
        if (Random.value > biome.vegetationDensity) return;

        GameObject prefab = null;
        if(isEvent && Random.value < 0.1f) prefab = biome.GetRandomGameplay(0); // Ennemi
        else if (!isEvent) prefab = biome.GetRandomProp(0); // Arbre

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), propContainer);
            // Petit scale aléatoire
            obj.transform.localScale *= Random.Range(0.8f, 1.2f);
        }
    }
    
    void SpawnDungeons()
    {
        // ... (Reprends ta fonction SpawnDungeon d'avant ici) ...
        // Astuce : utilise 'propContainer' comme parent pour garder la scène propre
    }

    BiomeProfile GetBiome(float angle)
    {
        if (angle >= -Mathf.PI && angle < -Mathf.PI / 2) return biomeWater;
        if (angle >= -Mathf.PI / 2 && angle < 0) return biomeFire;
        if (angle >= 0 && angle < Mathf.PI / 2) return biomeEarth;
        return biomeAir;
    }
}