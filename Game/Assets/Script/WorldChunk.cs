using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class WorldChunk : MonoBehaviour
{
    // --- PARAMÈTRES ---
    private int size;
    private Vector2 offset;
    private int seed;
    private float villageRadius;
    private float worldLimitRadius; 
    private BiomeProfile[] biomes; 
    private Dictionary<BiomeProfile.BiomeType, Vector3> dungeonPositions;

    // --- CONFIGURATION BARRIERE (Obsidienne) ---
    private Color barrierColor = new Color(0.15f, 0.15f, 0.2f); 
    private float barrierWidth = 0.35f; 

    // --- DATA ---
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Color> colors = new List<Color>();
    
    // Pour l'optimisation (Arbres/Rochers statiques)
    private Dictionary<GameObject, List<CombineInstance>> propsToCombine = new Dictionary<GameObject, List<CombineInstance>>();

    // --- FLAGS ---
    private bool hasLavaLakeInChunk = false;
    private bool hasWaterInChunk = false;
    private bool isDungeonChunk = false;
    private float lavaSurfaceHeight = -2.5f; 

    public void Initialize(int size, Vector2 offset, int seed, BiomeProfile[] biomes, float villageRad, Dictionary<BiomeProfile.BiomeType, Vector3> dPos)
    {
        this.size = size;
        this.offset = offset;
        this.seed = seed;
        this.biomes = biomes;
        this.villageRadius = villageRad;
        this.dungeonPositions = dPos;

        // Calcul de la limite du monde
        float maxDungeonDist = 0;
        foreach(var d in dPos.Values) {
            float dist = Vector3.Distance(Vector3.zero, d);
            if(dist > maxDungeonDist) maxDungeonDist = dist;
        }
        this.worldLimitRadius = maxDungeonDist + 15.0f;

        // Détection chunk proche donjon
        Vector2 chunkCenter = offset + new Vector2(size/2f, size/2f);
        foreach(var d in dPos.Values)
        {
            if (Vector2.Distance(chunkCenter, new Vector2(d.x, d.z)) < (size * 0.8f)) isDungeonChunk = true; 
        }

        // Sécurité: s'assurer que les composants requis existent
        EnsureRequiredComponents();

        GenerateTerrain();
        GenerateProps(); 
        SpawnLiquidSurface();
    }

    void GenerateTerrain()
    {
        vertices.Clear(); triangles.Clear(); colors.Clear();
        int vertIndex = 0;
        hasLavaLakeInChunk = false;
        hasWaterInChunk = false;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float gX = offset.x + x;
                float gZ = offset.y + z;

                // --- 1. HAUTEUR ---
                bool l00, l01, l11, l10, w00, w01, w11, w10;
                float h00 = GetPreciseHeight(gX, gZ, out l00, out w00);
                float h01 = GetPreciseHeight(gX, gZ + 1, out l01, out w01);
                float h11 = GetPreciseHeight(gX + 1, gZ + 1, out l11, out w11);
                float h10 = GetPreciseHeight(gX + 1, gZ, out l10, out w10);

                if (l00 || l01 || l11 || l10) hasLavaLakeInChunk = true;
                if (w00 || w01 || w11 || w10) hasWaterInChunk = true;
                if (Vector2.Distance(Vector2.zero, new Vector2(gX, gZ)) < villageRadius) continue;

                vertices.Add(new Vector3(x, h00, z));
                vertices.Add(new Vector3(x, h01, z + 1));
                vertices.Add(new Vector3(x + 1, h11, z + 1));
                vertices.Add(new Vector3(x + 1, h10, z));

                triangles.Add(vertIndex + 0); triangles.Add(vertIndex + 1); triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 0); triangles.Add(vertIndex + 2); triangles.Add(vertIndex + 3);
                vertIndex += 4;

                // --- 2. ANALYSE ---
                float centerX = gX + 0.5f; float centerZ = gZ + 0.5f;
                BiomeProfile mainBiome = GetDominantBiome(centerX, centerZ);
                float distCenter = Vector2.Distance(Vector2.zero, new Vector2(centerX, centerZ));
                bool isEventRaw = IsEventZone(centerX, centerZ);

                // Barrières
                float angle = Mathf.Atan2(centerZ, centerX);
                float barrierVal = Mathf.Abs(Mathf.Sin(2 * angle)); 
                bool isAnyBarrier = (barrierVal < barrierWidth || distCenter >= worldLimitRadius);

                // Donjon
                float minDistDung = 9999f;
                foreach(var d in dungeonPositions.Values) {
                    float dDist = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(d.x, d.z));
                    if(dDist < minDistDung) minDistDung = dDist;
                }

                // --- ZONES A RISQUE ---
                float avgH = (h00 + h11) / 2f;
                
                // Lave (Feu bas)
                bool isVisualLava = (avgH < lavaSurfaceHeight - 0.5f && mainBiome.type == BiomeProfile.BiomeType.Fire);
                
                // Mur Terre (Terre haut)
                bool isMazeWallEarth = (mainBiome.type == BiomeProfile.BiomeType.Earth && avgH > 5.0f);
                
                // Vide Air (Air bas) -> C'est ici qu'on détecte si on est tombé du pont
                bool isAirVoid = (mainBiome.type == BiomeProfile.BiomeType.Air && avgH < 5.0f);

                // --- SUPER FILTRE EVENT ---
                // On ajoute !isAirVoid à la liste des interdits
                bool isValidEvent = isEventRaw && !isVisualLava && !isMazeWallEarth && !isAirVoid && !isAnyBarrier && minDistDung > 30;

                // --- 3. COULEURS ---
                Color c = mainBiome.baseGroundColor;

                // APPLICATION DES COULEURS DE BASE
                if (minDistDung < 25.0f) 
                {
                    c = mainBiome.dungeonZoneColor * 0.8f;
                }
                else if (isAnyBarrier) 
                    c = barrierColor;
                else if (isVisualLava && !isDungeonChunk) 
                    c = new Color(0.1f, 0, 0); // Lave
                else if (isValidEvent) 
                    c = mainBiome.eventZoneColor;
                else if (isAirVoid)
                    c = new Color(0.6f, 0.7f, 0.8f); // Couleur du "Vide"
                else if (mainBiome.type == BiomeProfile.BiomeType.Earth && !isMazeWallEarth) 
                    c = c * 0.7f; // Sol Terre

                // CONTAMINATION PIXELLISÉE DONJON (25m à 50m)
                if (minDistDung >= 25.0f && minDistDung < 50.0f)
                {
                    // Bruit haute fréquence pour les pixels
                    float pixelNoise = Mathf.PerlinNoise(gX * 0.8f + seed * 3, gZ * 0.8f + seed * 3);
                    // Probabilité de contamination (diminue avec la distance)
                    float contaminationChance = 1.0f - ((minDistDung - 25.0f) / 25.0f);
                    
                    // Si le pixel est contaminé
                    if (pixelNoise < contaminationChance * 0.4f) // 0.4 = densité de contamination
                    {
                        c = mainBiome.dungeonZoneColor * 0.8f;
                    }
                }
                
                // CONTAMINATION PIXELLISÉE EVENT (rayon de 20m autour des events)
                if (isEventRaw && !isValidEvent && minDistDung > 30)
                {
                    // Distance au centre de l'event (approximatif via le bruit)
                    float eventCenterNoise = Mathf.PerlinNoise(centerX * 0.025f + seed + 200, centerZ * 0.025f + seed + 200);
                    
                    if (eventCenterNoise > 0.7f && eventCenterNoise < 0.82f) // Zone autour de l'event
                    {
                        // Bruit haute fréquence pour les pixels
                        float pixelNoise = Mathf.PerlinNoise(gX * 0.9f + seed * 5, gZ * 0.9f + seed * 5);
                        // Proximité au centre (0.7-0.82 = 0 à 1)
                        float proximity = (eventCenterNoise - 0.7f) / 0.12f;
                        
                        // Si le pixel est contaminé
                        if (pixelNoise < proximity * 0.5f) // 0.5 = densité
                        {
                            c = mainBiome.eventZoneColor;
                        }
                    }
                }

                colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);

                // --- 4. SPAWN ---
                Vector3 centerPos = new Vector3(x + 0.5f, avgH, z + 0.5f); 
                CheckDungeonSpawn(gX, gZ, centerPos);

                // Détecte si on est dans l'eau (pour le biome Water)
                bool isUnderwater = false;
                if (mainBiome.type == BiomeProfile.BiomeType.Water)
                {
                    // Utilise le même bruit que pour la génération du terrain
                    float islandNoise = Mathf.PerlinNoise(centerX * 0.035f + seed * 4, centerZ * 0.035f + seed * 4);
                    isUnderwater = (islandNoise <= 0.55f); // Si <= 0.55, c'est de l'eau
                }

                // On interdit le spawn dans le vide d'air, la lave, et l'eau
                if (!isVisualLava && !isAnyBarrier && !isMazeWallEarth && !isAirVoid && !isUnderwater && minDistDung > 20f)
                {
                    if (isValidEvent) 
                    {
                        if(Random.value < 0.25f) {
                            GameObject prefabToSpawn = null;
                            if (Random.value < 0.5f) prefabToSpawn = mainBiome.GetRandomGameplay(BiomeProfile.GameplayType.Enemy);
                            else prefabToSpawn = mainBiome.GetRandomGameplay(BiomeProfile.GameplayType.Collectible);

                            if (prefabToSpawn != null) {
                                GameObject instance = Instantiate(prefabToSpawn, transform);
                                instance.transform.localPosition = centerPos; 
                                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            }
                        }
                    }
                    else
                    {
                        SpawnNaturalContent(gX, gZ, new Vector3(x, h00, z), mainBiome);
                    }
                }
            }
        }
        // Fallback: si aucune géométrie n'a été générée (ex: chunk entièrement exclu), créer un quad minimal
        if (triangles.Count == 0 || vertices.Count < 4)
        {
            BiomeProfile centerBiome = GetDominantBiome(offset.x + size / 2f, offset.y + size / 2f);
            Color c = (centerBiome != null) ? centerBiome.baseGroundColor : Color.white;

            vertices.Clear(); colors.Clear(); triangles.Clear();
            vertices.Add(new Vector3(0, 0, 0));
            vertices.Add(new Vector3(size, 0, 0));
            vertices.Add(new Vector3(size, 0, size));
            vertices.Add(new Vector3(0, 0, size));
            colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);
            triangles.Add(0); triangles.Add(1); triangles.Add(2);
            triangles.Add(0); triangles.Add(2); triangles.Add(3);

        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

float GetPreciseHeight(float gX, float gZ, out bool isLavaZone, out bool isWaterZone)
    {
        isLavaZone = false;
        isWaterZone = false;
        Vector2 pos = new Vector2(gX, gZ);
        float distCenter = Vector2.Distance(Vector2.zero, pos);
        float angle = Mathf.Atan2(gZ, gX);

        // --- 1. CALCULS DE BASE ---
        float noise = Mathf.PerlinNoise(gX * 0.1f + seed, gZ * 0.1f + seed);
        float hEarth = noise * biomes[2].heightMultiplier; 
        float hAir = (noise > 0.5f) ? noise * biomes[3].heightMultiplier + 8 : -15;
        float lavaN = Mathf.PerlinNoise(gX * 0.04f + seed + 99, gZ * 0.04f + seed + 99);
        float hFire = Mathf.Pow(noise, 2) * biomes[1].heightMultiplier;
        
        float wWater = GetAngleWeight(angle, -Mathf.PI * 0.75f) + GetAngleWeight(angle, Mathf.PI * 1.25f);
        float wFire = GetAngleWeight(angle, -Mathf.PI * 0.25f);
        float wEarth = GetAngleWeight(angle, Mathf.PI * 0.25f);
        float wAir = GetAngleWeight(angle, Mathf.PI * 0.75f);

        float mixedHeight = (-2f * wWater) + (hFire * wFire) + (hEarth * wEarth) + (hAir * wAir);
        
        // INFO DONJON
        float minDistToDungeon = 9999f;
        Vector2 nearestDungeonPos = Vector2.zero;
        foreach(var d in dungeonPositions.Values) {
            float dDist = Vector2.Distance(pos, new Vector2(d.x, d.z));
            if(dDist < minDistToDungeon) {
                minDistToDungeon = dDist;
                nearestDungeonPos = new Vector2(d.x, d.z);
            }
        }
        
        if (wFire > 0.5f && distCenter > (villageRadius + 15f) && minDistToDungeon > 35f && lavaN < 0.35f) {
            mixedHeight = -5f; isLavaZone = true;
        }

        BiomeProfile dungeonBiome = GetDominantBiome(nearestDungeonPos.x, nearestDungeonPos.y);
        float targetDungeonHeight = 0.5f; 

        // --- 2. BIOME ACTUEL & MURS ---
        BiomeProfile currentBiome = GetDominantBiome(gX, gZ);
        float barrierVal = Mathf.Abs(Mathf.Sin(2 * angle));
        bool isUnderSeparator = (barrierVal < barrierWidth * 1.05f && distCenter > villageRadius && distCenter < worldLimitRadius);
        bool isUnderWorldWall = (distCenter >= worldLimitRadius - 1.0f);
        bool isUnderAnyWall = isUnderSeparator || isUnderWorldWall;

        float finalHeight = mixedHeight; 

        // ====================================================================
        // >>> 3. OVERRIDES PAR LES LABYRINTHES
        // ====================================================================
        
        // --- TERRE (Canyon Classique) ---
        if (currentBiome.type == BiomeProfile.BiomeType.Earth && distCenter > villageRadius + 5f && !isUnderAnyWall)
        {
            float cellSize = 14.0f; float wallHeight = 13.0f;
            float warpFreq = 0.08f; float warpAmp = 6.0f;
            float mX = gX + Mathf.PerlinNoise(gX * warpFreq, gZ * warpFreq) * warpAmp;
            float mZ = gZ + Mathf.PerlinNoise(gX * warpFreq + 50, gZ * warpFreq + 50) * warpAmp;
            int cX = Mathf.FloorToInt(mX/cellSize); int cZ = Mathf.FloorToInt(mZ/cellSize);
            float lX = mX - cX*cellSize; float lZ = mZ - cZ*cellSize;
            float h = Mathf.PerlinNoise(cX*0.93f + seed*0.2f, cZ*0.93f + seed*0.2f);
            
            bool isPath = false;
            if (lZ>cellSize-4f || lX>cellSize-4f) {
                if (h < 0.4f && lZ>cellSize-4f) isPath = true;
                else if (h < 0.8f && h>=0.4f && lX>cellSize-4f) isPath = true;
                else if (h >= 0.8f) isPath = true;
            } else isPath = true;

            if (isPath) {
                float debris = Mathf.PerlinNoise(gX*0.35f, gZ*0.35f);
                finalHeight = (debris > 0.65f) ? debris * 2.5f : Mathf.PerlinNoise(gX*0.2f, gZ*0.2f)*0.8f;
            } else finalHeight = wallHeight + Mathf.PerlinNoise(gX*0.5f, gZ*0.5f);
        }
        
        // --- AIR (Archipel Céleste Complexe) ---
        else if (currentBiome.type == BiomeProfile.BiomeType.Air && distCenter > villageRadius + 5f && !isUnderAnyWall)
        {
            float baseLevel = 13.0f; // Niveau de référence (Moyen)
            float voidHeight = -20.0f;

            // Paramètres Rampe
            float rampLength = 40.0f; 
            float rampStart = villageRadius + 5f;
            bool isOnRamp = (distCenter < rampStart + rampLength);
            
            // Paramètres Labyrinthe
            // On utilise une distorsion plus forte pour des îles plus organiques
            float warpFreq = 0.06f; float warpAmp = 7.0f; 
            float mX = gX + Mathf.PerlinNoise(gX*warpFreq, gZ*warpFreq)*warpAmp;
            float mZ = gZ + Mathf.PerlinNoise(gX*warpFreq+50, gZ*warpFreq+50)*warpAmp;
            
            // Grille plus petite pour plus de détails
            float cellSize = 10.0f; 
            int cX = Mathf.FloorToInt(mX/cellSize); 
            int cZ = Mathf.FloorToInt(mZ/cellSize);
            
            // Bruit de structure (Est-ce qu'il y a un pont ?)
            float hStruct = Mathf.PerlinNoise(cX*0.8f + seed, cZ*0.8f + seed);
            // Bruit de hauteur (Quel étage ?)
            float hTier = Mathf.PerlinNoise(cX*1.2f + seed*2, cZ*1.2f + seed*2);

            bool isPlatform = false;
            
            // Logique de génération plus "Fracturée"
            // On garde des chemins, mais on ajoute des "îles" aléatoires
            if (hStruct > 0.4f && hStruct < 0.75f) isPlatform = true; // Chemin principal
            if (hStruct > 0.85f) isPlatform = true; // Grosses îles isolées

            // Forcer le passage vers le donjon
            bool nearDungeon = (minDistToDungeon < 45.0f);
            if (nearDungeon) isPlatform = true;

            // --- GESTION DES NIVEAUX (TIERS) ---
            float platformHeight = baseLevel;
            
            // Si on est LOIN de la rampe, on autorise les changements de hauteur
            // (Sinon on reste à 13m pour que la rampe arrive bien)
            if (!isOnRamp && distCenter > rampStart + rampLength + 10f && !nearDungeon)
            {
                // 3 Niveaux : -6m, 0m, +6m
                if (hTier < 0.33f) platformHeight = baseLevel - 6.0f;      // Niveau Bas (7m)
                else if (hTier > 0.66f) platformHeight = baseLevel + 6.0f; // Niveau Haut (19m)
                // Sinon Niveau Moyen (13m)
            }
            
            // Détail de surface (bosses sur les îles)
            float surfaceDetail = Mathf.PerlinNoise(gX*0.3f, gZ*0.3f) * 1.5f;

            // --- CALCUL FINAL AIR ---
            if (isOnRamp)
            {
                float groundStart = 0; 
                float t = (distCenter - rampStart) / rampLength;
                float blend = Mathf.SmoothStep(0, 1, t);
                
                // La rampe monte toujours vers le niveau "Moyen" (baseLevel)
                float rampH = Mathf.Lerp(groundStart, baseLevel, blend);
                finalHeight = rampH + surfaceDetail * 0.5f;
            }
            else
            {
                if (isPlatform) 
                    finalHeight = platformHeight + surfaceDetail;
                else 
                    finalHeight = voidHeight;
            }
        }
        else if (currentBiome.type == BiomeProfile.BiomeType.Water && distCenter > villageRadius && !isUnderAnyWall)
        {
            // CONFIGURATION
            float seaFloorDepth = -9.0f;  // Profondeur du fond marin
            float islandLevel = 1.5f;     // Hauteur de base des îles
            float waterLevel = -0.4f;     // Niveau visuel de l'eau (juste pour info)
            
            // RAMPE D'ENTRÉE PROGRESSIVE - Commence DÈS la sortie du village
            float rampLength = 50.0f;     // Longueur de la rampe d'entrée (plus longue pour plus de douceur)
            float rampStart = villageRadius;  // Commence immédiatement après le village
            bool isOnRamp = (distCenter < rampStart + rampLength);
            
            // 1. BRUIT DES ÎLES (Forme des continents)
            // Fréquence basse = Grandes étendues d'eau et îles moyennes
            float islandNoise = Mathf.PerlinNoise(gX * 0.035f + seed * 4, gZ * 0.035f + seed * 4);
            
            // 2. BRUIT DE RELIEF (Détails sur les îles)
            float detailNoise = Mathf.PerlinNoise(gX * 0.15f, gZ * 0.15f);

            // 3. CALCUL DE LA HAUTEUR FINALE DU BIOME
            float targetWaterHeight;
            
            if (islandNoise > 0.55f)
            {
                // C'est une île !
                float heightFactor = (islandNoise - 0.55f) / 0.45f; 
                targetWaterHeight = islandLevel + (heightFactor * 6.0f) + (detailNoise * 2.0f);
            }
            else
            {
                // C'est de l'eau (Fond marin)
                isWaterZone = true;
                targetWaterHeight = seaFloorDepth + (detailNoise * 1.5f);
            }

            // 4. RAMPE D'ENTRÉE (Transition douce depuis le village)
            if (isOnRamp)
            {
                float groundStart = 0.5f;  // Hauteur du village (niveau plat)
                float t = (distCenter - rampStart) / rampLength;
                // Double lissage pour une descente ultra progressive
                float blend = Mathf.Pow(Mathf.SmoothStep(0, 1, t), 1.8f);
                
                // Descente progressive vers le niveau Water
                finalHeight = Mathf.Lerp(groundStart, targetWaterHeight, blend);
            }
            else
            {
                // Hors rampe, on utilise la hauteur normale du biome
                finalHeight = targetWaterHeight;
            }

            // 5. SÉCURITÉ DONJON (Force une île sous le donjon)
            if (minDistToDungeon < 35.0f)
            {
                float t = Mathf.Clamp01((35.0f - minDistToDungeon) / 10.0f);
                finalHeight = Mathf.Lerp(finalHeight, 2.5f + detailNoise, t);
            }
        }
        else
        {
             if (currentBiome.type == BiomeProfile.BiomeType.Air) {
                 if (distCenter < villageRadius + 20f) finalHeight = hEarth; 
                 else targetDungeonHeight = 13.0f;
             }
        }
        

        // --- 4. MONTAGNES ---
        float rNoise = Mathf.PerlinNoise(gX*0.15f+seed*8, gZ*0.15f+seed*8);
        float rockTex = (0.4f + 0.6f*(rNoise*rNoise));
        if (distCenter > villageRadius + 5f && distCenter < worldLimitRadius && barrierVal < barrierWidth) {
            finalHeight = Mathf.Max(finalHeight, (1f-Mathf.Pow(barrierVal/barrierWidth,2))*40f*rockTex);
        }
        if (distCenter >= worldLimitRadius) {
            finalHeight = Mathf.Max(finalHeight, Mathf.Clamp((distCenter-worldLimitRadius)*4f,0,50)*rockTex + 5f);
        }

        // --- 5. LISSAGE DONJON ---
        float flatRadius = 20.0f;      
        float smoothLength = 25.0f;      
        if (minDistToDungeon < (flatRadius + smoothLength))
        {
            if (minDistToDungeon < flatRadius) finalHeight = targetDungeonHeight;
            else {
                float t = (minDistToDungeon - flatRadius) / smoothLength;
                float blendFactor = Mathf.SmoothStep(0, 1, t);
                finalHeight = Mathf.Lerp(targetDungeonHeight, finalHeight, blendFactor);
            }
        }

        float startBorder = villageRadius -1f; 
        
        // La longueur de la pente (sur combien de mètres on descend)
        float slopeLength = 15.0f; 
        
        // Si on est dans la zone de transition (juste après le village)
        if (distCenter >= startBorder && distCenter < (startBorder + slopeLength))
        {
            // t va de 0 (au bord du village) à 1 (15m plus loin)
            float t = (distCenter - startBorder) / slopeLength;
            float blend = Mathf.SmoothStep(0, 1, t);
            finalHeight = Mathf.Lerp(-0.33f, finalHeight, blend);

        }        

        return finalHeight;
    }

    bool IsEventZone(float x, float z)
    {
        float noise = Mathf.PerlinNoise(x * 0.025f + seed + 200, z * 0.025f + seed + 200);
        return noise > 0.82f; 
    }

    // --- SPAWN CONTENU NATUREL (Batching) ---
    void SpawnNaturalContent(float gX, float gZ, Vector3 basePos, BiomeProfile biome)
    {
        float noise = Mathf.PerlinNoise(gX * 0.05f + seed, gZ * 0.05f + seed);

        if (noise > 0.45f) // Forêts
        {
             if (Random.value < biome.vegetationDensity)
                AddPropToCombineList(biome.GetRandomProp(0), basePos + new Vector3(0.5f,0,0.5f), Random.Range(0, 360), Random.Range(0.8f, 1.5f));
        }
        else if (Random.value < 0.9f) // Petits détails
        {
            int density = Random.Range(3, 8); 
            for(int i=0; i < density; i++)
            {
                float offsetX = Random.Range(0.1f, 0.9f);
                float offsetZ = Random.Range(0.1f, 0.9f);
                Vector3 spawnPos = basePos + new Vector3(offsetX, 0, offsetZ);
                AddPropToCombineList(biome.GetRandomProp(2), spawnPos, Random.Range(0, 360), Random.Range(0.7f, 1.1f));
            }
        }
    }

    void SpawnLiquidSurface()
    {
        if (isDungeonChunk) return;
        BiomeProfile centerBiome = GetDominantBiome(offset.x + size/2f, offset.y + size/2f);
        if (hasLavaLakeInChunk && centerBiome.type == BiomeProfile.BiomeType.Fire && centerBiome.liquidSurfacePrefab != null)
        {
            GameObject liquid = Instantiate(centerBiome.liquidSurfacePrefab, transform);
            liquid.transform.localPosition = new Vector3(size / 2f, lavaSurfaceHeight, size / 2f);
            liquid.transform.localScale = new Vector3(size / 10f, 1, size / 10f); 
            liquid.name = "Lava_Surface";
        }
        if (hasWaterInChunk && centerBiome.type == BiomeProfile.BiomeType.Water && centerBiome.liquidSurfacePrefab != null)
        {
            GameObject liquid = Instantiate(centerBiome.liquidSurfacePrefab, transform);
            liquid.transform.localPosition = new Vector3(size / 2f, lavaSurfaceHeight, size / 2f);
            liquid.transform.localScale = new Vector3(size / 10f, 1, size / 10f); 
            liquid.name = "Water_Surface";
        }
    }

    void CheckDungeonSpawn(float gX, float gZ, Vector3 localPos)
    {
        int cX = Mathf.FloorToInt(gX);
        int cZ = Mathf.FloorToInt(gZ);
        foreach (var entry in dungeonPositions)
        {
            if (cX == Mathf.FloorToInt(entry.Value.x) && cZ == Mathf.FloorToInt(entry.Value.z))
            {
                BiomeProfile dBiome = GetBiomeByType(entry.Key);
                if (dBiome != null && dBiome.dungeonPrefab != null)
                {
                    GameObject d = Instantiate(dBiome.dungeonPrefab, transform);
                    d.transform.localPosition = new Vector3(localPos.x, 0.5f, localPos.z);
                    d.transform.LookAt(Vector3.zero);
                }
            }
        }
    }

    // --- UTILITAIRES ---
    void EnsureRequiredComponents()
    {
        if (GetComponent<MeshFilter>() == null) gameObject.AddComponent<MeshFilter>();
        if (GetComponent<MeshRenderer>() == null) gameObject.AddComponent<MeshRenderer>();
        if (GetComponent<MeshCollider>() == null) gameObject.AddComponent<MeshCollider>();
    }

    float GetAngleWeight(float current, float target)
    {
        float diff = Mathf.Abs(Mathf.DeltaAngle(current * Mathf.Rad2Deg, target * Mathf.Rad2Deg) * Mathf.Deg2Rad);
        if (diff > Mathf.PI / 2) return 0;
        return Mathf.Pow(Mathf.Cos(diff), 4);
    }

    BiomeProfile GetDominantBiome(float x, float z)
    {
        float angle = Mathf.Atan2(z, x);
        if (angle >= -Mathf.PI && angle < -Mathf.PI / 2) return biomes[0];
        if (angle >= -Mathf.PI / 2 && angle < 0) return biomes[1];
        if (angle >= 0 && angle < Mathf.PI / 2) return biomes[2];
        return biomes[3];
    }
    
    BiomeProfile GetBiomeByType(BiomeProfile.BiomeType type)
    {
        foreach(var b in biomes) if(b.type == type) return b;
        return null;
    }

    // --- BATCHING & OPTIMISATION ---
    void AddPropToCombineList(GameObject prefab, Vector3 localPos, float rotY, float scale)
    {
        if (prefab == null) return;
        if (!propsToCombine.ContainsKey(prefab)) propsToCombine[prefab] = new List<CombineInstance>();
        Matrix4x4 matrix = Matrix4x4.TRS(localPos, Quaternion.Euler(0, rotY, 0), Vector3.one * scale);
        CombineInstance combine = new CombineInstance();
        combine.mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        combine.transform = matrix;
        propsToCombine[prefab].Add(combine);
    }

    void GenerateProps()
    {
        foreach (KeyValuePair<GameObject, List<CombineInstance>> entry in propsToCombine)
        {
            if (entry.Value.Count == 0) continue;

            GameObject holder = new GameObject(entry.Key.name + "_Batch");
            holder.transform.parent = transform;
            holder.transform.localPosition = Vector3.zero;

            MeshFilter mf = holder.AddComponent<MeshFilter>();
            MeshRenderer mr = holder.AddComponent<MeshRenderer>();
            mr.sharedMaterial = entry.Key.GetComponent<MeshRenderer>().sharedMaterial;

            Mesh m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.CombineMeshes(entry.Value.ToArray(), true, true);
            m.RecalculateBounds(); // IMPORTANT : Recalcule les bounds pour le frustum culling
            mf.mesh = m;

            // --- FILTRAGE INTELLIGENT DES COLLIDERS ---
            if (entry.Key.GetComponent<Collider>() != null)
            {
                MeshCollider mc = holder.AddComponent<MeshCollider>();
                mc.sharedMesh = m;
            }
        }
    }
}