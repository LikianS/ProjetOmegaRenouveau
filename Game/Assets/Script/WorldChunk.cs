using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class WorldChunk : MonoBehaviour
{
    private int size;
    private Vector2 offset;
    private int seed;
    private float villageRadius;
    private float worldLimitRadius; 
    private BiomeProfile[] biomes; 
    private Dictionary<BiomeProfile.BiomeType, Vector3> dungeonPositions;

    private Color barrierColor = new Color(0.15f, 0.15f, 0.2f); 
    private float barrierWidth = 0.35f; 

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Color> colors = new List<Color>();

    private Dictionary<GameObject, List<CombineInstance>> propsToCombine = new Dictionary<GameObject, List<CombineInstance>>();

    private List<Vector3> enemySpawnPoints = new List<Vector3>();

    private NavMeshDataInstance navMeshDataInstance;

    private bool hasLavaLakeInChunk = false;
    private bool hasWaterInChunk = false;
    private bool isDungeonChunk = false;
    private float lavaSurfaceHeight = -2.5f;
    private const float airVoidFloorHeight = -20.0f;

    public void Initialize(int size, Vector2 offset, int seed, BiomeProfile[] biomes, float villageRad, Dictionary<BiomeProfile.BiomeType, Vector3> dPos)
    {
        this.size = size;
        this.offset = offset;
        this.seed = seed;
        this.biomes = biomes;
        this.villageRadius = villageRad;
        this.dungeonPositions = dPos;

        float maxDungeonDist = 0;
        foreach(var d in dPos.Values) {
            float dist = Vector3.Distance(Vector3.zero, d);
            if(dist > maxDungeonDist) maxDungeonDist = dist;
        }
        this.worldLimitRadius = maxDungeonDist + 15.0f;

        Vector2 chunkCenter = offset + new Vector2(size/2f, size/2f);
        foreach(var d in dPos.Values)
        {
            if (Vector2.Distance(chunkCenter, new Vector2(d.x, d.z)) < (size * 0.8f)) isDungeonChunk = true; 
        }

        EnsureRequiredComponents();

        GenerateTerrain();
        GenerateProps(); 
        SpawnLiquidSurface();
        StartCoroutine(SpawnEnemiesDelayed());
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

                float centerX = gX + 0.5f; float centerZ = gZ + 0.5f;
                BiomeProfile mainBiome = GetDominantBiome(centerX, centerZ);
                float distCenter = Vector2.Distance(Vector2.zero, new Vector2(centerX, centerZ));
                bool isEventRaw = IsEventZone(centerX, centerZ);

                float angle = Mathf.Atan2(centerZ, centerX);
                float barrierVal = Mathf.Abs(Mathf.Sin(2 * angle)); 
                bool isAnyBarrier = (barrierVal < barrierWidth || distCenter >= worldLimitRadius);

                float minDistDung = 9999f;
                foreach(var d in dungeonPositions.Values) {
                    float dDist = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(d.x, d.z));
                    if(dDist < minDistDung) minDistDung = dDist;
                }

                float avgH = (h00 + h11) / 2f;
                bool isVisualLava = (avgH < lavaSurfaceHeight - 0.5f && mainBiome.type == BiomeProfile.BiomeType.Fire);
                bool isMazeWallEarth = (mainBiome.type == BiomeProfile.BiomeType.Earth && avgH > 5.0f);
                bool isAirVoid = (mainBiome.type == BiomeProfile.BiomeType.Air && avgH < 5.0f);
                if (isAirVoid && mainBiome.voidZonePrefab != null)
                {
                    SpawnAirVoidZoneAt(mainBiome.voidZonePrefab, new Vector3(x + 0.5f, airVoidFloorHeight, z + 0.5f));
                }

                bool isValidEvent = isEventRaw && !isVisualLava && !isMazeWallEarth && !isAirVoid && !isAnyBarrier && minDistDung > 30;

                Color c = mainBiome.baseGroundColor;

                if (minDistDung < 25.0f) 
                {
                    c = mainBiome.dungeonZoneColor * 0.8f;
                }
                else if (isAnyBarrier) 
                    c = barrierColor;
                else if (isVisualLava && !isDungeonChunk) 
                    c = new Color(0.1f, 0, 0);
                else if (isValidEvent) 
                    c = mainBiome.eventZoneColor;
                else if (isAirVoid)
                    c = new Color(0.6f, 0.7f, 0.8f);
                else if (mainBiome.type == BiomeProfile.BiomeType.Earth && !isMazeWallEarth) 
                    c = c * 0.7f;

                if (minDistDung >= 25.0f && minDistDung < 50.0f)
                {
                    float pixelNoise = Mathf.PerlinNoise(gX * 0.8f + seed * 3, gZ * 0.8f + seed * 3);
                    float contaminationChance = 1.0f - ((minDistDung - 25.0f) / 25.0f);
                    if (pixelNoise < contaminationChance * 0.4f)
                    {
                        c = mainBiome.dungeonZoneColor * 0.8f;
                    }
                }
                if (isEventRaw && !isValidEvent && minDistDung > 30)
                {
                    float eventCenterNoise = Mathf.PerlinNoise(centerX * 0.025f + seed + 200, centerZ * 0.025f + seed + 200);
                    
                    if (eventCenterNoise > 0.7f && eventCenterNoise < 0.82f)
                    {
                        float pixelNoise = Mathf.PerlinNoise(gX * 0.9f + seed * 5, gZ * 0.9f + seed * 5);
                        float proximity = (eventCenterNoise - 0.7f) / 0.12f;
                        if (pixelNoise < proximity * 0.5f)
                        {
                            c = mainBiome.eventZoneColor;
                        }
                    }
                }

                colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);

                Vector3 centerPos = new Vector3(x + 0.5f, avgH, z + 0.5f); 
                CheckDungeonSpawn(gX, gZ, centerPos);

                bool isUnderwater = false;
                if (mainBiome.type == BiomeProfile.BiomeType.Water)
                {
                    float islandNoise = Mathf.PerlinNoise(centerX * 0.035f + seed * 4, centerZ * 0.035f + seed * 4);
                    isUnderwater = (islandNoise <= 0.55f);
                }

                if (!isVisualLava && !isAnyBarrier && !isMazeWallEarth && !isAirVoid && !isUnderwater && minDistDung > 20f)
                {
                    if (isValidEvent) 
                    {
                        if(Random.value < 0.15f) {
                            GameObject prefabToSpawn = mainBiome.GetRandomGameplay(BiomeProfile.GameplayType.Collectible);
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
        
        BakeNavMesh();
    }
    
    void BakeNavMesh()
    {
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            NavMeshBuildSource source = new NavMeshBuildSource();
            source.shape = NavMeshBuildSourceShape.Mesh;
            source.sourceObject = meshFilter.mesh;
            source.transform = transform.localToWorldMatrix;
            source.area = 0;
            sources.Add(source);
        }
        WorldChunk[] allChunks = FindObjectsOfType<WorldChunk>();
        foreach (WorldChunk neighborChunk in allChunks)
        {
            if (neighborChunk == this) continue;
            
            float distance = Vector3.Distance(transform.position, neighborChunk.transform.position);
            if (distance <= size * 1.5f)
            {
                MeshFilter neighborMesh = neighborChunk.GetComponent<MeshFilter>();
                if (neighborMesh != null && neighborMesh.mesh != null)
                {
                    NavMeshBuildSource neighborSource = new NavMeshBuildSource();
                    neighborSource.shape = NavMeshBuildSourceShape.Mesh;
                    neighborSource.sourceObject = neighborMesh.mesh;
                    neighborSource.transform = neighborChunk.transform.localToWorldMatrix;
                    neighborSource.area = 0;
                    sources.Add(neighborSource);
                }
            }
        }
        
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.5f;
        settings.agentRadius = 0.5f;
        settings.agentHeight = 2.0f;
        settings.agentClimb = 0.4f;
        settings.agentSlope = 45f;
        settings.ledgeDropHeight = 0;
        settings.maxJumpAcrossDistance = 0;
        
        float overlapSize = 5.0f;
        Bounds chunkBounds = new Bounds(
            transform.position + new Vector3(size / 2f, 0, size / 2f),
            new Vector3(size + overlapSize * 2, 50f, size + overlapSize * 2)
        );
        
        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
            settings,
            sources,
            chunkBounds,
            Vector3.zero,
            Quaternion.identity
        );
        
        if (navMeshData != null)
        {
            navMeshDataInstance = NavMesh.AddNavMeshData(navMeshData);
        }
        else
        {
        }
    }
    
    void OnDestroy()
    {
        if (navMeshDataInstance.valid)
        {
            NavMesh.RemoveNavMeshData(navMeshDataInstance);
        }
    }

float GetPreciseHeight(float gX, float gZ, out bool isLavaZone, out bool isWaterZone)
    {
        isLavaZone = false;
        isWaterZone = false;
        Vector2 pos = new Vector2(gX, gZ);
        float distCenter = Vector2.Distance(Vector2.zero, pos);
        float angle = Mathf.Atan2(gZ, gX);

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

        BiomeProfile currentBiome = GetDominantBiome(gX, gZ);
        float barrierVal = Mathf.Abs(Mathf.Sin(2 * angle));
        bool isUnderSeparator = (barrierVal < barrierWidth * 1.05f && distCenter > villageRadius && distCenter < worldLimitRadius);
        bool isUnderWorldWall = (distCenter >= worldLimitRadius - 1.0f);
        bool isUnderAnyWall = isUnderSeparator || isUnderWorldWall;

        float finalHeight = mixedHeight; 

        
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
        
        else if (currentBiome.type == BiomeProfile.BiomeType.Air && distCenter > villageRadius + 5f && !isUnderAnyWall)
        {
            float baseLevel = 13.0f;
            float voidHeight = airVoidFloorHeight;

            float rampLength = 40.0f; 
            float rampStart = villageRadius + 5f;
            bool isOnRamp = (distCenter < rampStart + rampLength);
            
            float warpFreq = 0.06f; float warpAmp = 7.0f; 
            float mX = gX + Mathf.PerlinNoise(gX*warpFreq, gZ*warpFreq)*warpAmp;
            float mZ = gZ + Mathf.PerlinNoise(gX*warpFreq+50, gZ*warpFreq+50)*warpAmp;
            
            float cellSize = 10.0f; 
            int cX = Mathf.FloorToInt(mX/cellSize); 
            int cZ = Mathf.FloorToInt(mZ/cellSize);
            
            float hStruct = Mathf.PerlinNoise(cX*0.8f + seed, cZ*0.8f + seed);
            float hTier = Mathf.PerlinNoise(cX*1.2f + seed*2, cZ*1.2f + seed*2);

            bool isPlatform = false;
            
            if (hStruct > 0.4f && hStruct < 0.75f) isPlatform = true;
            if (hStruct > 0.85f) isPlatform = true;

            bool nearDungeon = (minDistToDungeon < 45.0f);
            if (nearDungeon) isPlatform = true;

            float platformHeight = baseLevel;
            
            if (!isOnRamp && distCenter > rampStart + rampLength + 10f && !nearDungeon)
            {
                if (hTier < 0.33f) platformHeight = baseLevel - 6.0f;
                else if (hTier > 0.66f) platformHeight = baseLevel + 6.0f;
            }
            
            float surfaceDetail = Mathf.PerlinNoise(gX*0.3f, gZ*0.3f) * 1.5f;

            if (isOnRamp)
            {
                float groundStart = 0; 
                float t = (distCenter - rampStart) / rampLength;
                float blend = Mathf.SmoothStep(0, 1, t);
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
            float seaFloorDepth = -9.0f;
            float islandLevel = 1.5f;
            float waterLevel = -0.4f;
            
            float rampLength = 50.0f;
            float rampStart = villageRadius;
            bool isOnRamp = (distCenter < rampStart + rampLength);
            
            float islandNoise = Mathf.PerlinNoise(gX * 0.035f + seed * 4, gZ * 0.035f + seed * 4);
            float detailNoise = Mathf.PerlinNoise(gX * 0.15f, gZ * 0.15f);

            float targetWaterHeight;
            
            if (islandNoise > 0.55f)
            {
                float heightFactor = (islandNoise - 0.55f) / 0.45f; 
                targetWaterHeight = islandLevel + (heightFactor * 6.0f) + (detailNoise * 2.0f);
            }
            else
            {
                isWaterZone = true;
                targetWaterHeight = seaFloorDepth + (detailNoise * 1.5f);
            }

            if (isOnRamp)
            {
                float groundStart = 0.5f;
                float t = (distCenter - rampStart) / rampLength;
                float blend = Mathf.Pow(Mathf.SmoothStep(0, 1, t), 1.8f);
                finalHeight = Mathf.Lerp(groundStart, targetWaterHeight, blend);
            }
            else
            {
                finalHeight = targetWaterHeight;
            }

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
        

        float rNoise = Mathf.PerlinNoise(gX*0.15f+seed*8, gZ*0.15f+seed*8);
        float rockTex = (0.4f + 0.6f*(rNoise*rNoise));
        if (distCenter > villageRadius + 5f && distCenter < worldLimitRadius && barrierVal < barrierWidth) {
            finalHeight = Mathf.Max(finalHeight, (1f-Mathf.Pow(barrierVal/barrierWidth,2))*40f*rockTex);
        }
        if (distCenter >= worldLimitRadius) {
            finalHeight = Mathf.Max(finalHeight, Mathf.Clamp((distCenter-worldLimitRadius)*4f,0,50)*rockTex + 5f);
        }

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
        
        float slopeLength = 15.0f; 
        if (distCenter >= startBorder && distCenter < (startBorder + slopeLength))
        {
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

    void SpawnNaturalContent(float gX, float gZ, Vector3 basePos, BiomeProfile biome)
    {
        float noise = Mathf.PerlinNoise(gX * 0.05f + seed, gZ * 0.05f + seed);

        if (noise > 0.45f)
        {
             if (Random.value < biome.vegetationDensity)
                AddPropToCombineList(biome.GetRandomProp(0), basePos + new Vector3(0.5f,0,0.5f), Random.Range(0, 360), Random.Range(0.8f, 1.5f));
        }
        else if (Random.value < 0.9f)
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

    void SpawnAirVoidZoneAt(GameObject prefab, Vector3 localPos)
    {
        GameObject zone = Instantiate(prefab, transform);
        zone.transform.localPosition = localPos;
        zone.transform.localScale = Vector3.one;
    }

    void SpawnEnemies()
    {
        if (isDungeonChunk) return;
        
        enemySpawnPoints.Clear();
        int spawnedCount = 0;
        int totalChecked = 0;
        int failedBiome = 0;
        int failedDistance = 0;
        int failedWalkable = 0;
        int failedRandom = 0;
        int failedNavMesh = 0;
        
        for (int x = 0; x < size; x += 3)
        {
            for (int z = 0; z < size; z += 3)
            {
                totalChecked++;
                float gX = offset.x + x;
                float gZ = offset.y + z;
                
                float distFromVillage = Vector2.Distance(Vector2.zero, new Vector2(gX, gZ));
                
                BiomeProfile biome = GetDominantBiome(gX, gZ);
                if (biome == null || biome.enemies == null || biome.enemies.Length == 0) 
                {
                    failedBiome++;
                    continue;
                }
                if (distFromVillage < biome.minVillageDistance) 
                {
                    failedDistance++;
                    continue;
                }
                
                bool isLava, isWater;
                float height = GetPreciseHeight(gX, gZ, out isLava, out isWater);
                
                if (!IsWalkableForEnemy(gX, gZ, height, isLava, isWater, biome)) 
                {
                    failedWalkable++;
                    continue;
                }
                
                Vector3 spawnWorldPos = transform.position + new Vector3(x, height + 0.5f, z);
                Vector3 spawnLocalPos = new Vector3(x, height + 0.5f, z);
                
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(spawnWorldPos, out hit, 5.0f, NavMesh.AllAreas))
                {
                    failedNavMesh++;
                    continue;
                }
                
                Vector3 validatedWorldPos = hit.position;
                Vector3 validatedLocalPos = validatedWorldPos - transform.position;
                
                if (IsTooCloseToOtherSpawns(validatedLocalPos, biome.minEnemyDistance)) continue;
                
                enemySpawnPoints.Add(validatedLocalPos);
                SpawnEnemyAtPosition(validatedWorldPos, biome, true);
                spawnedCount++;
            }
        }
    }
    
    System.Collections.IEnumerator SpawnEnemiesDelayed()
    {
        yield return null;
        yield return null;
        SpawnEnemies();
    }
    
    System.Collections.IEnumerator EnableNavMeshAgentDelayed(GameObject enemy)
    {
        if (enemy == null) yield break;
        yield return new WaitForSeconds(0.2f);
        
        if (enemy == null) yield break;
        
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            UnityEngine.AI.NavMeshHit hit;
            Vector3 worldPos = enemy.transform.position;
            
            if (UnityEngine.AI.NavMesh.SamplePosition(worldPos, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                enemy.transform.position = hit.position;
                agent.enabled = true;
                agent.Warp(hit.position);
            }
            else
            {
                Destroy(enemy);
            }
        }
    }
    
    bool IsWalkableForEnemy(float gX, float gZ, float height, bool isLava, bool isWater, BiomeProfile biome)
    {
        float distCenter = Mathf.Sqrt(gX * gX + gZ * gZ);
        float angle = Mathf.Atan2(gZ, gX);
        float barrierVal = Mathf.Abs(Mathf.Sin(2f * angle));
        bool onObsidianWall = (barrierVal < barrierWidth * 1.05f && distCenter > villageRadius && distCenter < worldLimitRadius)
                              || distCenter >= worldLimitRadius - 0.5f;
        if (onObsidianWall)
            return false;

        if (biome.type == BiomeProfile.BiomeType.Fire && height < lavaSurfaceHeight - 0.5f)
            return false;

        if (isWater)
            return false;

        if (biome.type == BiomeProfile.BiomeType.Water && height < 1.0f)
            return false;

        if (biome.type == BiomeProfile.BiomeType.Earth && height > 5.0f)
            return false;

        if (biome.type == BiomeProfile.BiomeType.Air && height < 5.0f)
            return false;

        return true;
    }
    
    bool IsTooCloseToOtherSpawns(Vector3 pos, float minDist)
    {
        foreach (Vector3 existingSpawn in enemySpawnPoints)
        {
            if (Vector3.Distance(pos, existingSpawn) < minDist)
                return true;
        }
        return false;
    }
    
    void SpawnEnemyAtPosition(Vector3 spawnPos, BiomeProfile biome, bool isWorldPos = false)
    {
        if (biome.enemies == null || biome.enemies.Length == 0) return;
        
        GameObject enemyPrefab = biome.GetRandomGameplay(BiomeProfile.GameplayType.Enemy);
        if (enemyPrefab == null) return;
        
        GameObject enemy = Instantiate(enemyPrefab, transform);
        
        if (isWorldPos)
        {
            enemy.transform.position = spawnPos;
        }
        else
        {
            enemy.transform.localPosition = spawnPos;
        }
        
        enemy.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        enemy.name = $"Enemy_{biome.type}_{enemySpawnPoints.Count}";
        
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        ActivateBiomeParticles(enemy, biome.type);
        
        StartCoroutine(EnableNavMeshAgentDelayed(enemy));
    }
    
    void ActivateBiomeParticles(GameObject enemy, BiomeProfile.BiomeType biomeType)
    {
        ParticleSystem[] allParticles = enemy.GetComponentsInChildren<ParticleSystem>(true);
        
        if (allParticles.Length == 0) return;
        
        string targetParticleName = biomeType.ToString();
        
        foreach (ParticleSystem ps in allParticles)
        {
            if (ps.gameObject.name.Contains(targetParticleName))
            {
                ps.gameObject.SetActive(true);
                ps.Play();
            }
            else
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
            }
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
            m.RecalculateBounds();
            mf.mesh = m;

            if (entry.Key.GetComponent<Collider>() != null)
            {
                MeshCollider mc = holder.AddComponent<MeshCollider>();
                mc.sharedMesh = m;
            }
        }
    }
}