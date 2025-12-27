Shader "Custom/ToonSkyCloud_FinalPro"
{
    Properties
    {
        [Header(Colors)]
        _TopColor ("Top Color (White)", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color (Shadow)", Color) = (0.4, 0.45, 0.6, 1)
        _LightBoost ("Light Brightness", Range(1, 5)) = 1.5
        
        [Header(Shape)]
        _Scale ("Cloud Scale", Float) = 15.0
        _VerticalStretch ("Vertical Stretch (Fix Flatness)", Range(0.1, 5.0)) = 0.2
        _Density ("Density Hardness", Range(0, 20)) = 10.0
        _Coverage ("Coverage", Range(0, 1)) = 0.35
        
        [Header(Relief Control)]
        _VerticalFade ("Edge Softness", Range(0.01, 0.5)) = 0.1 
        
        [Header(Animation)]
        _Speed ("Wind Speed", Vector) = (0.2, 0, 0, 0)
        
        [Header(Quality)]
        _Steps ("Steps", Int) = 100
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front 
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ToonCloudPro"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0; 
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor, _BottomColor;
                float _Scale, _Density, _Coverage, _LightBoost, _VerticalFade, _VerticalStretch;
                float3 _Speed;
                int _Steps;
            CBUFFER_END

            float hash(float3 p) {
                p = frac(p * 0.3183099 + .1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            float hash2D(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            float noise(float3 x) {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                               lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                          lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                               lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            float fbm(float3 p) {
                float f = 0.0; float amp = 0.5;
                for(int i = 0; i < 3; i++) { f += amp * noise(p); p *= 2.0; amp *= 0.5; }
                return f;
            }

            float GetDensity(float3 p) {
                if(abs(p.x) > 0.5 || abs(p.y) > 0.5 || abs(p.z) > 0.5) return 0;

                // --- CORRECTION ANTI-PLAT ---
                // On copie p pour le calcul du bruit
                float3 noisePos = p;
                // On multiplie Y par une petite valeur pour "étirer" le bruit verticalement
                noisePos.y *= _VerticalStretch; 
                
                float3 pos = noisePos * _Scale + (_Time.y * _Speed);
                float noiseVal = fbm(pos);
                
                float distFromCenter = abs(p.y);
                float edgeMask = smoothstep(0.5, 0.5 - _VerticalFade, distFromCenter);
                noiseVal *= edgeMask;
                
                float d = noiseVal - (1.0 - _Coverage);
                d = smoothstep(0.0, 0.1, d);
                return d * _Density;
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

            Varyings vert(Attributes input) {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 camPosLocal = TransformWorldToObject(_WorldSpaceCameraPos);
                float3 hitPosLocal = TransformWorldToObject(input.positionWS);
                
                float3 ro = camPosLocal;
                float3 rd = normalize(hitPosLocal - camPosLocal);
                
                float tNear, tFar;
                if(!RayBoxIntersection(ro, rd, tNear, tFar)) discard;
                
                float tStart = max(0.0, tNear);
                float tEnd = tFar;
                float dist = tEnd - tStart;
                if(dist <= 0) discard;

                float stepSize = dist / float(_Steps);
                float dither = hash2D(input.positionCS.xy);
                float t = tStart + dither * stepSize; 

                float3 p = ro + rd * t;
                float4 col = float4(0,0,0,0);
                float transmittance = 1.0; 

                [loop]
                for(int i = 0; i < _Steps; i++) {
                    if(transmittance < 0.01 || t > tEnd) break;

                    float den = GetDensity(p);
                    
                    if(den > 0.001) {
                        float heightFactor = smoothstep(-0.5, 0.5, p.y); 
                        float3 cloudColor = lerp(_BottomColor.rgb, _TopColor.rgb, heightFactor);
                        cloudColor *= _LightBoost;

                        float alpha = 1.0 - exp(-den * 5.0 * stepSize); 
                        col.rgb += cloudColor * alpha * transmittance;
                        transmittance *= (1.0 - alpha);
                    }
                    
                    p += rd * stepSize;
                    t += stepSize;
                }

                col.a = 1.0 - transmittance;
                return col;
            }
            ENDHLSL
        }
    }
}