Shader "Custom/ToonElementsSafe"
{
    Properties
    {
        [Header(Three Tone Colors)]
        _CoreColor ("Mid Color (Yellow/Orange)", Color) = (1, 0.8, 0.2, 1)
        _OuterColor ("Shadow Color (Red)", Color) = (0.8, 0.1, 0, 1)
        _HotColor ("Hot Spot Color (White/Pale Yellow)", Color) = (1, 1, 0.9, 1)
        
        [Header(Shape Settings)]
        [KeywordEnum(Fire, Ice, Earth, Wind)] _Element ("Element", Float) = 0
        _Scale ("Global Scale", Float) = 1.0
        _Speed ("Animation Speed", Float) = 1.0
        _Distortion ("Noise/Turbulence", Range(0, 5)) = 1.0
        
        [Header(Container Integration)]
        _EdgeFade ("Edge Softness", Range(0.01, 0.2)) = 0.05 // Pour éviter le clipping aux murs

        [Header(Limits and Taper)]
        _HeightLimit ("Height Limit (Fire/Wind)", Range(0.1, 0.5)) = 0.45
        _Taper ("Pointiness (Fire)", Range(0.5, 5)) = 2.0
        
        [Header(Toon Lighting)]
        _Cutoff ("Shadow Threshold", Range(0.1, 0.9)) = 0.4
        _HotSpotSize ("Hot Spot Size", Range(0.5, 1.0)) = 0.8
        _RimPower ("Rim Sharpness", Range(0.1, 5)) = 3.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        // CRUCIAL : Cull Off pour voir l'intérieur
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ELEMENT_FIRE _ELEMENT_ICE _ELEMENT_EARTH _ELEMENT_WIND
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 rayOrigin : TEXCOORD0; // Local Space
                float3 rayDir : TEXCOORD1;    // Local Space
            };

            float4 _CoreColor, _OuterColor, _HotColor;
            float _Scale, _Speed, _Distortion, _HeightLimit, _Taper, _Cutoff, _HotSpotSize, _RimPower;
            float _EdgeFade;

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

            // --- MATHS & SDF ---
            float2 rot(float2 p, float a) { float c=cos(a), s=sin(a); return float2(c*p.x-s*p.y, s*p.x+c*p.y); }
            float hash(float n) { return frac(sin(n)*43758.5453); }
            float noise(float3 x) {
                float3 p = floor(x); float3 f = frac(x);
                f = f*f*(3.0-2.0*f);
                float n = p.x + p.y*57.0 + 113.0*p.z;
                return lerp(lerp(lerp(hash(n+0.0), hash(n+1.0),f.x), lerp(hash(n+57.0), hash(n+58.0),f.x),f.y),
                          lerp(lerp(hash(n+113.0), hash(n+114.0),f.x), lerp(hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
            }
            float smin(float a, float b, float k) { float h = clamp(0.5+0.5*(b-a)/k, 0.0, 1.0); return lerp(b, a, h)-k*h*(1.0-h); }
            float sdBox(float3 p, float3 b) { float3 q = abs(p)-b; return length(max(q,0.0))+min(max(q.x,max(q.y,q.z)),0.0); }

            float GetDist(float3 p) {
                float d = 0;
                float3 pRaw = p;

                #if defined(_ELEMENT_FIRE)
                    p.y += 0.3;
                    float heightFactor = clamp(p.y * _Taper * 0.8, 0.0, 3.0);
                    float radius = (0.4 * _Scale) / (1.0 + heightFactor*heightFactor); 
                    float baseShape = length(p.xz) - radius;
                    float3 noisePos = pRaw * 3.0;
                    noisePos.y -= _Time.y * _Speed * 3.0;
                    float erosionStrength = smoothstep(-0.2, 0.5, pRaw.y) * _Distortion;
                    float n = noise(noisePos);
                    d = baseShape + n * 0.3 * erosionStrength;
                    float verticalLimits = max(pRaw.y - _HeightLimit, -pRaw.y - 0.4);
                    d = max(d, verticalLimits);

                #elif defined(_ELEMENT_ICE)
                    float3 pIce = pRaw; pIce.xz=abs(pIce.xz); pIce.xz=rot(pIce.xz,0.5); pIce.xz=abs(pIce.xz);
                    pIce-=float3(0.1,-0.5,0.1)*_Scale; pIce.xy=rot(pIce.xy,0.3*_Distortion);
                    d=sdBox(pIce,float3(0.08,1.5,0.08)*_Scale); d=max(d,-pRaw.y-0.5);

                #elif defined(_ELEMENT_EARTH)
                    float nGeo=noise(pRaw*2.0); float core=length(pRaw)-(_Scale*0.4)+nGeo*0.1;
                    float3 pDe=(pRaw); pDe.xz=rot(pDe.xz,_Time.y*_Speed*0.2); pDe.xy=rot(pDe.xy,_Time.y*_Speed*0.1);
                    float nDe=noise(pDe*5.0); float ck=length(pRaw)-0.7*_Scale; ck+=nDe*0.8*_Distortion;
                    d=smin(core,ck,0.1);

                #elif defined(_ELEMENT_WIND)
                    float3 pW=pRaw; float ang=-_Time.y*_Speed*6.0+pW.y*3.0; pW.xz=rot(pW.xz,ang);
                    float rad=0.1+(pW.y+0.2)*(pW.y+0.2)*0.5*_Scale; float cyl=length(pW.xz)-rad;
                    float nW=noise(float3(pW.x*5.0,pW.y*2.0-_Time.y*8.0,pW.z*5.0)); d=cyl+nW*0.03*_Distortion;
                    d=max(d, abs(pRaw.y)-_HeightLimit);
                #endif
                return d;
            }

            float3 GetNormal(float3 p) {
                float2 e=float2(0.005,0); return normalize(GetDist(p)-float3(GetDist(p-e.xyy),GetDist(p-e.yxy),GetDist(p-e.yyx)));
            }

            v2f vert (appdata v) {
                v2f o; 
                o.pos=UnityObjectToClipPos(v.vertex);
                // Calcul en espace OBJET (Local)
                float3 camPosObj = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                o.rayOrigin = camPosObj;
                o.rayDir = normalize(v.vertex.xyz - camPosObj);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 ro = i.rayOrigin;
                float3 rd = i.rayDir;

                // 1. Définir la zone de raymarching (La boite)
                float tNear, tFar;
                if (!RayBoxIntersection(ro, rd, tNear, tFar)) discard;

                // 2. Gestion "Je suis dans la boite"
                float t = max(0.0, tNear);
                float maxDist = tFar;

                // 3. Raymarching
                float dO = t;
                bool hit = false;
                float3 p = 0;

                // Boucle optimisée
                for(int k=0; k<70; k++) {
                    // Si on dépasse la sortie de la boite, on arrête
                    if(dO > maxDist) break;

                    p = ro + rd * dO;
                    float dS = GetDist(p);
                    
                    // On a touché la forme
                    if(dS < 0.002) {
                        hit = true;
                        break;
                    }
                    dO += dS;
                }

                if(!hit) discard;

                // --- 4. CALCUL COULEUR (Une fois qu'on a touché) ---
                float3 n = GetNormal(p);
                float3 lightDir = normalize(float3(0.5, 1.0, -0.3)); 
                float diff = dot(n, lightDir);

                // Toon Ramp
                float toon1 = smoothstep(_Cutoff - 0.05, _Cutoff + 0.05, diff);
                float3 baseCol = lerp(_OuterColor.rgb, _CoreColor.rgb, toon1);
                float toon2 = smoothstep(_HotSpotSize - 0.05, _HotSpotSize + 0.05, diff);
                float3 finalCol = lerp(baseCol, _HotColor.rgb, toon2);

                // Rim
                float rim = 1.0 - saturate(dot(n, -rd));
                rim = pow(rim, _RimPower);
                rim = smoothstep(0.5, 0.7, rim);
                
                #if defined(_ELEMENT_ICE) || defined(_ELEMENT_FIRE)
                    finalCol += _HotColor.rgb * rim;
                #else
                    finalCol += _CoreColor.rgb * rim * 0.5;
                #endif

                // --- 5. GESTION DES BORDS (Anti-Print) ---
                // On calcule si le point d'impact est proche d'un mur du cube
                // Le cube fait de -0.5 à 0.5.
                float3 distFromCenter = abs(p);
                // Distance au mur le plus proche
                float distToWall = 0.5 - max(distFromCenter.x, max(distFromCenter.y, distFromCenter.z));
                // On fade l'alpha si on est à moins de _EdgeFade du mur
                float edgeAlpha = smoothstep(0.0, _EdgeFade, distToWall);

                // Alpha final
                float alpha = 1.0;
                #if defined(_ELEMENT_WIND)
                    alpha = 0.5 + rim * 0.5;
                #endif
                
                // On applique le fade des bords
                alpha *= edgeAlpha;

                // Si c'est presque invisible, on discard pour éviter l'écriture Z inutile
                if(alpha < 0.01) discard;

                return fixed4(finalCol, alpha);
            }
            ENDCG
        }
    }
}