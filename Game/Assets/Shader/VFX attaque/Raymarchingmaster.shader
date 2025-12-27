Shader "Custom/UltimateRaymarcherSafe"
{
    Properties
    {
        [Header(General Settings)]
        _Color ("Main Color", Color) = (1,1,1,1)
        _Gloss ("Glossiness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Container Integration)]
        _EdgeFade ("Edge Softness", Range(0.001, 0.2)) = 0.05 // Pour fondre les bords
        
        [Header(Effect Control)]
        [KeywordEnum(Plasma, Fractal, Singularity, Whip, Storm, Wind, Stalactites, Magma, Ice, Ferro)] 
        _EffectID ("Effect Type", Float) = 0
        
        _Param ("Evolution (Time/Shape)", Range(0, 10)) = 1.0
        _Scale ("Scale / Density", Range(0.1, 5)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        
        // 1. Cull Off pour voir l'intérieur
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // --- VARIABLES ---
            struct appdata { float4 vertex : POSITION; };
            struct v2f { 
                float4 pos : SV_POSITION; 
                float3 rayOrigin : TEXCOORD0; 
                float3 rayDir : TEXCOORD1; 
            };

            float4 _Color;
            float _Gloss, _Metallic, _EffectID, _Param, _Scale, _EdgeFade;

            // --- BIBLIOTHÈQUE MATHS (SDF & NOISE) ---
            float smin(float a, float b, float k) {
                float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }
            float sdBox(float3 p, float3 b) { float3 q = abs(p) - b; return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0); }

            float hash(float n) { return frac(sin(n)*43758.5453); }
            float noise(float3 x) {
                float3 p = floor(x); float3 f = frac(x);
                f = f*f*(3.0-2.0*f);
                float n = p.x + p.y*57.0 + 113.0*p.z;
                return lerp(lerp(lerp(hash(n+0.0), hash(n+1.0),f.x), lerp(hash(n+57.0), hash(n+58.0),f.x),f.y),
                          lerp(lerp(hash(n+113.0), hash(n+114.0),f.x), lerp(hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
            }
            float3 rotate(float3 p, float3 axis, float angle) {
                axis = normalize(axis);
                float s = sin(angle); float c = cos(angle);
                return p * c + cross(axis, p) * s + axis * dot(axis, p) * (1 - c);
            }

            // --- MAPPING (Adapté pour tenir dans un cube de taille 1) ---
            float map(float3 p) {
                float d = 10.0;
                float time = _Time.y * _Param;
                
                // On garde les effets centrés (Espace local)
                
                // 0: Plasma Beam
                if (_EffectID < 0.5) { 
                    float deform = noise(p * 3.0 + float3(0,0,time*5.0)); 
                    d = length(p.xy) - (0.1 + deform * 0.1 * _Scale); // Réduit rayon à 0.1
                }
                // 1: Fractal Spikes
                else if (_EffectID < 1.5) {
                    float3 q = p;
                    for(int i=0; i<3; i++) {
                        q = abs(q) - 0.15 * _Scale; // Taille réduite pour le cube
                        q.xz = float2(q.x*0.5 - q.z*0.866, q.x*0.866 + q.z*0.5);
                        q *= 1.5;
                    }
                    d = sdBox(q, float3(0.02, 0.5, 0.02)) / 3.0;
                }
                // 2: Singularity
                else if (_EffectID < 2.5) {
                    d = length(p) - 0.35 * _Scale; // Rayon max 0.35 (tient dans 0.5)
                    d += noise(p * 5.0 + time) * 0.05; 
                }
                // 3: Organic Whip
                else if (_EffectID < 3.5) {
                    float3 q = p;
                    q.x += sin(q.z * 4.0 + time * 3.0) * 0.2; // Amplitude réduite
                    float width = clamp(0.15 - q.z * 0.1, 0.0, 0.3);
                    d = length(q.xy) - width * _Scale;
                }
                // 4: Storm Orb
                else if (_EffectID < 4.5) {
                    float sphere = length(p) - 0.4 * _Scale;
                    float interior = noise(p * 4.0 + rotate(p, float3(0,1,0), time*2.0)) * 0.5;
                    d = max(sphere, -interior + 0.1); 
                }
                // 5: Wind Blade
                else if (_EffectID < 5.5) {
                    float3 q = p;
                    q.x += q.z * q.z * 0.5; 
                    d = sdBox(q, float3(0.02, 0.2, 0.45) * _Scale); 
                    d -= noise(q * 10.0 + float3(0,0,time*10.0)) * 0.01;
                }
                // 6: Stalactites
                else if (_EffectID < 6.5) {
                    float ceiling = 0.5 - p.y; // Plafond à 0.5 (Haut du cube)
                    float3 q = p; q.xz = frac(p.xz * 4.0) - 0.5; 
                    float spike = length(q.xz) - (0.2 * (p.y + 0.5));
                    d = smin(ceiling, spike, 0.1);
                }
                // 7: Magma Fissure
                else if (_EffectID < 7.5) {
                    float ground = p.y + 0.3; // Sol un peu plus bas
                    float crack = noise(p * 4.0 + float3(0.1, 0, 0.1)) * 0.2 * _Scale;
                    d = ground + crack;
                }
                // 8: Ice Lance
                else if (_EffectID < 8.5) {
                      float3 q = p;
                      q.z += time * 2.0; 
                      q = abs(q) - 0.05;
                      d = sdBox(q, float3(0.05, 0.05, 0.4));
                      d += noise(p*10.0)*0.01; 
                }
                // 9: Ferrofluid
                else {
                    float sol = p.y + 0.4;
                    float3 q = p; 
                    q.xz = fmod(abs(q.xz)*2.0, 1.0) - 0.5;
                    float wave = sin(time*2.0 + p.x)*0.5 + 0.5;
                    float spikes = length(q) - 0.05 * wave * _Scale;
                    d = smin(sol, spikes, 0.2);
                }

                return d;
            }

            // --- CALCUL NORMALE ---
            float3 calcNormal(float3 p) {
                const float2 e = float2(0.001, 0);
                return normalize(float3(map(p+e.xyy)-map(p-e.xyy), map(p+e.yxy)-map(p-e.yxy), map(p+e.yyx)-map(p-e.yyx)));
            }
            
            // --- INTERSECTION BOITE ---
            bool RayBoxIntersection(float3 ro, float3 rd, out float tNear, out float tFar) {
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

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // On passe en espace OBJET pour que le SDF suive le cube
                float3 camPosObj = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                o.rayOrigin = camPosObj;
                o.rayDir = normalize(v.vertex.xyz - camPosObj);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 ro = i.rayOrigin;
                float3 rd = i.rayDir;

                // 1. Limites du Cube
                float tNear, tFar;
                if (!RayBoxIntersection(ro, rd, tNear, tFar)) discard;

                // 2. Point de départ (Gère caméra dedans/dehors)
                float t = max(0.0, tNear);
                float maxDist = tFar;

                // 3. Raymarching
                float3 p = 0;
                bool hit = false;
                
                for(int k=0; k<64; k++) {
                    if(t > maxDist) break; // Sortie du cube
                    
                    p = ro + rd * t;
                    float d = map(p);
                    
                    if(d < 0.001) { hit = true; break; }
                    t += d;
                }

                if(!hit) discard;

                // 4. Couleur & Lumière
                float3 n = calcNormal(p);
                // Lumière directionnelle simple (simulée localement)
                float3 lightDir = normalize(float3(0.5, 1.0, -0.5));
                float3 viewDir = -rd;

                float diff = max(0, dot(n, lightDir));
                float fresnel = pow(1.0 - max(0, dot(n, viewDir)), 3.0);
                float spec = pow(max(0, dot(reflect(-lightDir, n), viewDir)), 10.0) * _Metallic;

                float3 finalColor = _Color.rgb * (diff * 0.5 + 0.5);
                finalColor += fresnel * _Gloss * _Color.rgb;
                finalColor += spec;

                // Cas Spécial Singularité (Noir au centre)
                if (_EffectID > 1.5 && _EffectID < 2.5) {
                    finalColor = lerp(float3(0,0,0), _Color.rgb, fresnel);
                }

                // 5. ANTI-PRINT (Fade aux bords du cube)
                float3 distFromCenter = abs(p);
                float distToWall = 0.5 - max(distFromCenter.x, max(distFromCenter.y, distFromCenter.z));
                float alpha = smoothstep(0.0, _EdgeFade, distToWall);

                if (alpha < 0.01) discard;

                return float4(finalColor, alpha);
            }
            ENDCG
        }
    }
}