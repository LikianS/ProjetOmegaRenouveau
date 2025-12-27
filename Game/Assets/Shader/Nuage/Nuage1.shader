Shader "Custom/URP_VolumetricCloud_ShadowFix"
{
    Properties
    {
        [Header(Main Settings)]
        _BaseColor ("Cloud Color", Color) = (1,1,1,1)
        _SunColor ("Sun Light Color", Color) = (1, 0.9, 0.7, 1)
        _ShadowColor ("Deep Shadow / Ambient", Color) = (0.3, 0.4, 0.6, 1)
        
        [Header(Density and Shape)]
        _Density ("Global Density", Range(0, 5)) = 1.5
        _Coverage ("Cloud Coverage", Range(0, 1)) = 0.4
        _Absorption ("Light Absorption", Range(0, 20)) = 5.0
        
        [Header(Noise Settings)]
        _Scale ("Noise Scale", Float) = 3.0
        _Detail ("Detail Scale", Float) = 8.0
        _Speed ("Wind Speed", Vector) = (0.5, 0.1, 0, 0)
        
        [Header(Atmosphere and Phase)]
        _PhaseParam ("Sun Scattering (Phase)", Range(-1, 1)) = 0.6
        _Steps ("Quality Steps", Int) = 64
        
        _SunDir ("Sun Direction (Auto)", Vector) = (0, -1, 0, 0)
        
        [Header(Shadow Casting)]
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.15 
        _ShadowOpacity ("Shadow Opacity Multiplier", Float) = 2.0
        
        [Header(Container)]
        _EdgeFade ("Container Softness", Range(0, 0.5)) = 0.1
    }

    SubShader
    {
        // TRUC : On ment à Unity en disant "Opaque" pour qu'il force le calcul d'ombre, 
        // mais on garde la Queue Transparent pour le rendu visuel.
        Tags { "RenderType"="Opaque" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _SunColor, _ShadowColor;
            float _Density, _Coverage, _Absorption, _Scale, _Detail, _PhaseParam, _EdgeFade, _ShadowThreshold, _ShadowOpacity;
            float3 _Speed, _SunDir;
            int _Steps;
        CBUFFER_END

        // --- NOISE ---
        float hash(float n) { return frac(sin(n)*43758.5453); }
        float noise(float3 x) {
            float3 p = floor(x); float3 f = frac(x);
            f = f*f*(3.0-2.0*f);
            float n = p.x + p.y*57.0 + 113.0*p.z;
            return lerp(lerp(lerp(hash(n+0.0), hash(n+1.0),f.x), lerp(hash(n+57.0), hash(n+58.0),f.x),f.y),
                        lerp(lerp(hash(n+113.0), hash(n+114.0),f.x), lerp(hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
        }

        float GetDensity(float3 p) {
            // Sécurité pour ne pas sampler hors du cube
            if(abs(p.x) > 0.5 || abs(p.y) > 0.5 || abs(p.z) > 0.5) return 0;

            float3 distToEdge = 0.5 - abs(p);
            float edgeMask = smoothstep(0.0, _EdgeFade, min(distToEdge.x, min(distToEdge.y, distToEdge.z)));
            float3 offset = _Time.y * _Speed;
            float baseShape = noise(p * _Scale + offset);
            float detail = noise(p * _Detail - offset * 2.0) * 0.5;
            float finalNoise = baseShape - detail * 0.3;
            float d = smoothstep(1.0 - _Coverage, 1.0 - _Coverage + 0.2, finalNoise);
            return d * _Density * edgeMask;
        }

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
        ENDHLSL

        // --- PASS 1 : RENDU VISUEL ---
        Pass
        {
            Name "VolumetricCloud"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
            };

            float HGPhase(float costh, float g) {
                return (1.0 - g * g) / (4.0 * 3.1415 * pow(1.0 + g * g - 2.0 * g * costh, 1.5));
            }

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 camPosObj = TransformWorldToObject(_WorldSpaceCameraPos);
                output.rayOrigin = camPosObj;
                output.rayDir = normalize(input.positionOS.xyz - camPosObj);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 ro = input.rayOrigin;
                float3 rd = input.rayDir;
                float tNear, tFar;
                if(!RayBoxIntersection(ro, rd, tNear, tFar)) discard;

                float t = max(0.0, tNear);
                float stepSize = (tFar - tNear) / float(_Steps);
                float3 p = ro + rd * t;
                float totalDensity = 0.0;
                float3 finalColor = 0.0;
                float transmittance = 1.0;
                float3 lightDir = normalize(_SunDir); // Vers le soleil
                float cosAngle = dot(rd, lightDir);
                float phaseVal = HGPhase(cosAngle, _PhaseParam);

                [loop]
                for(int k=0; k<_Steps; k++) {
                    if(totalDensity > 1.0 || transmittance < 0.01 || t > tFar) break;
                    float den = GetDensity(p);
                    if(den > 0.001) {
                        float lightTransmittance = 1.0;
                        float3 lightRayPos = p;
                        float lightStep = 0.05; 
                        [unroll]
                        for(int j=0; j<4; j++) {
                            lightRayPos += lightDir * lightStep;
                            if(abs(lightRayPos.x)>0.5 || abs(lightRayPos.y)>0.5 || abs(lightRayPos.z)>0.5) break;
                            lightTransmittance *= exp(-GetDensity(lightRayPos) * _Absorption * 0.5);
                        }
                        float3 incomingLight = _SunColor.rgb * lightTransmittance * phaseVal * 5.0;
                        float3 ambient = _ShadowColor.rgb * (1.0 - lightTransmittance);
                        float3 cloudColor = _BaseColor.rgb * (incomingLight + ambient);
                        float prevTransmittance = transmittance;
                        transmittance *= exp(-den * stepSize * _Absorption);
                        finalColor += cloudColor * (prevTransmittance - transmittance);
                        totalDensity += den * stepSize;
                    }
                    p += rd * stepSize;
                    t += stepSize;
                }
                return half4(finalColor, 1.0 - transmittance);
            }
            ENDHLSL
        }

        // --- PASS 2 : SHADOW CASTER CORRIGÉE ---
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { 
                float4 positionOS : POSITION; 
                float3 normalOS : NORMAL;
            };
            struct Varyings { 
                float4 positionCS : SV_POSITION; 
                float3 posOS : TEXCOORD0; // On garde la position objet
            };

            Varyings vert(Attributes input) {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // --- MODIFICATION ICI ---
                // On supprime ApplyShadowBias. On envoie la position brute.
                // Cela force l'ombre à coller exactement au cube, sans décalage.
                output.positionCS = TransformWorldToHClip(positionWS);
                
                output.posOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // NOUVELLE LOGIQUE :
                // On ne raymarch pas depuis la caméra.
                // On part du point actuel sur le cube (input.posOS) et on avance le long de la lumière.
                // Si on traverse de la densité, on garde l'ombre.
                
                float3 p = input.posOS;
                
                // _SunDir pointe VERS le soleil. 
                // Pour l'ombre, on veut savoir "est-ce qu'il y a du nuage derrière ce point ?"
                // Donc on marche à l'opposé du soleil (ou dans le même sens, tant qu'on traverse le cube).
                // Ici, on va marcher "dans le nuage" à partir de la surface.
                
                // Pour couvrir tout le volume, on utilise _SunDir inversé pour traverser le cube
                float3 rayDir = -normalize(_SunDir); 
                
                // On s'assure qu'on est dans le cube
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3( 0.5,  0.5,  0.5);
                
                // Petit Raymarch simplifié pour l'ombre
                float accumulatedDensity = 0.0;
                float stepSize = 0.05; // Pas plus gros pour perf
                int shadowSteps = 10;
                
                [loop]
                for(int k=0; k<shadowSteps; k++) {
                    // Si on sort du cube, stop
                    if(abs(p.x)>0.5 || abs(p.y)>0.5 || abs(p.z)>0.5) break;

                    float den = GetDensity(p);
                    accumulatedDensity += den;
                    
                    p += rayDir * stepSize;
                }
                
                // Si la densité totale traversée est trop faible -> Pas d'ombre (Transparent)
                // Sinon -> Ombre (Opaque)
                clip((accumulatedDensity * _ShadowOpacity) - _ShadowThreshold);

                return 0;
            }
            ENDHLSL
        }
    }
}