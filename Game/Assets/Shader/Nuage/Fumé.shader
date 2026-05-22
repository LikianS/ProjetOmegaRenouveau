Shader "Custom/VolumetricSmokeInside"
{
    Properties
    {
        [Header(General Settings)]
        _NoiseTex ("Texture 3D Noise", 3D) = "white" {}
        _Density ("Global Density", Range(0, 10)) = 2.0
        _StepCount ("Quality (Steps)", Range(10, 100)) = 64
        _Speed ("Animation Speed", Vector) = (0, -0.2, 0, 0)
        _NoiseScale ("Noise Scale", Float) = 1.0

        [Header(Shape)]
        _EdgeFade ("Edge Softness", Range(0.01, 0.5)) = 0.1 // Marge pour ne pas toucher les murs

        [Header(Colors)]
        _ColorTop ("Smoke Color (Top)", Color) = (0.5, 0.5, 0.5, 1)
        _ColorBot ("Smoke Color (Bottom)", Color) = (0.1, 0.1, 0.1, 1)
        
        [Header(Fire and Lava)]
        _FireColor ("Fire Emission Color", Color) = (1, 0.5, 0, 1)
        _FireThreshold ("Fire Height Threshold", Range(0, 1)) = 0.2
        _FireIntensity ("Fire Intensity", Range(0, 10)) = 3.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // CRUCIAL : Cull Off permet de voir l'intérieur du cube quand on rentre dedans
        Cull Off 
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 localPos : TEXCOORD1; // Position du vertex en local
            };

            sampler3D _NoiseTex;
            float _Density;
            int _StepCount;
            float4 _Speed;
            float _NoiseScale;
            float _EdgeFade; // Nouveau paramètre
            float4 _ColorTop;
            float4 _ColorBot;
            float4 _FireColor;
            float _FireThreshold;
            float _FireIntensity;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            // --- Intersection Boite Améliorée ---
            // Gère le cas où la caméra est DANS la boite
            bool RayBoxIntersection(float3 ro, float3 rd, out float tNear, out float tFar)
            {
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3( 0.5,  0.5,  0.5);
                
                float3 invDir = 1.0 / (rd + 1e-5);
                float3 tMin = (boxMin - ro) * invDir;
                float3 tMax = (boxMax - ro) * invDir;
                
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                
                tNear = max(max(t1.x, t1.y), t1.z);
                tFar = min(min(t2.x, t2.y), t2.z);
                
                return tFar >= tNear && tFar > 0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.localPos = v.vertex.xyz; // On passe la position brute du vertex
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calcul Rayon en Espace Objet (Local)
                // Position caméra convertie en espace local du cube
                float3 camPosObj = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                // Direction : du point de vue (caméra) vers le pixel (vertex interpolé)
                float3 rayDir = normalize(i.localPos - camPosObj);
                float3 rayOrigin = camPosObj;
                
                float tNear, tFar;
                
                // Si pas d'intersection, on annule
                if (!RayBoxIntersection(rayOrigin, rayDir, tNear, tFar)) discard;

                // 2. LOGIQUE "DEDANS / DEHORS"
                // Si tNear < 0, c'est que l'entrée est derrière nous -> on est DANS le cube.
                // Donc on commence à marcher à partir de 0 (la caméra).
                float tCurrent = max(0.0, tNear);
                
                // Setup Raymarching
                float distToTravel = tFar - tCurrent;
                if (distToTravel <= 0.0) discard; // Sécurité

                float stepSize = distToTravel / float(_StepCount);
                
                float4 accumulatedColor = float4(0,0,0,0);
                
                // Pré-calcul collision Depth Buffer (Z-Buffer)
                float sceneDepthNonLinear = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                float sceneDepthLinear = LinearEyeDepth(sceneDepthNonLinear);
                // Note: La collision précise profondeur scène vs rayon objet est complexe à faire parfaitement
                // sans repasser en WorldSpace. Ici on garde simple.

                [loop]
                for(int k=0; k < _StepCount; k++)
                {
                    // Calcul position actuelle
                    float3 p = rayOrigin + rayDir * tCurrent;
                    
                    // --- NOUVEAU : ANTI-PRINT SUR LES BORDS ---
                    // On calcule la distance par rapport au centre (0,0,0)
                    // Si une coordonnée s'approche de 0.5 (le mur), on met l'alpha à 0.
                    float3 distFromCenter = abs(p);
                    // On veut que fade soit 1 au centre et 0 sur les bords (0.5)
                    // _EdgeFade contrôle l'épaisseur de la zone de sécurité
                    float edgeMask = 1.0;
                    edgeMask *= smoothstep(0.5, 0.5 - _EdgeFade, distFromCenter.x);
                    edgeMask *= smoothstep(0.5, 0.5 - _EdgeFade, distFromCenter.y);
                    edgeMask *= smoothstep(0.5, 0.5 - _EdgeFade, distFromCenter.z);

                    if(edgeMask > 0.01) 
                    {
                        // Echantillonnage Texture 3D
                        float3 uv = p + 0.5; // Remap -0.5/0.5 -> 0/1
                        float3 noiseUV = (uv * _NoiseScale) + (_Time.y * _Speed.xyz);
                        float dens = tex3D(_NoiseTex, noiseUV).r;
                        
                        // Application de la densité et du masque de bord
                        dens *= _Density * edgeMask;
                        
                        if(dens > 0.01)
                        {
                            // Couleur (Feu / Fumée)
                            float heightInfo = uv.y;
                            float3 colIdx;
                            
                            if(heightInfo < _FireThreshold) {
                                float heat = (_FireThreshold - heightInfo) / _FireThreshold;
                                colIdx = lerp(_ColorBot.rgb, _FireColor.rgb * _FireIntensity, heat * heat);
                            } else {
                                colIdx = lerp(_ColorBot.rgb, _ColorTop.rgb, heightInfo);
                            }

                            // Alpha Blending (Front-to-Back)
                            float a = 1.0 - exp(-dens * stepSize);
                            accumulatedColor.rgb += colIdx * a * (1.0 - accumulatedColor.a);
                            accumulatedColor.a += a * (1.0 - accumulatedColor.a);
                        }
                    }

                    if(accumulatedColor.a >= 0.99) break;
                    tCurrent += stepSize;
                }

                return accumulatedColor;
            }
            ENDCG
        }
    }
}