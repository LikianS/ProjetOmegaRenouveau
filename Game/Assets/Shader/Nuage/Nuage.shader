Shader "Custom/VolumetricCloudSafe"
{
    Properties
    {
        [Header(Main Settings)]
        _MainColor ("Lit Color", Color) = (1, 0.95, 0.9, 1)
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.35, 0.45, 1)
        _Density ("Global Density", Range(0, 5)) = 1.5
        _Absorption ("Light Absorption", Range(0, 10)) = 3.5
        
        [Header(Shape)]
        _Scale ("Base Noise Scale", Float) = 3.0
        _DetailScale ("Detail Noise Scale", Float) = 6.0
        _DetailStrength ("Detail Erosion", Range(0, 1)) = 0.5
        
        [Header(Animation)]
        _Speed ("Wind Speed", Vector) = (0.2, 0.05, 0.0, 0)
        _GroundFade ("Ground Fade", Range(0, 1)) = 0.2
        _LightDir ("Light Direction", Vector) = (0.5, 1.0, 0.2, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { 
                float4 vertex : SV_POSITION; 
                float3 localPos : TEXCOORD0; 
                float3 viewDir : TEXCOORD1; 
            };

            float4 _MainColor, _ShadowColor;
            float _Density, _Absorption, _Scale, _DetailScale, _DetailStrength;
            float3 _Speed, _LightDir;
            float _GroundFade;

            // --- Fonctions Noise Simplifiées ---
            float hash(float3 p) {
                p  = frac( p*0.3183099+.1 );
                p *= 17.0;
                return frac( p.x*p.y*p.z*(p.x+p.y+p.z) );
            }

            float noise(float3 x) {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f*f*(3.0-2.0*f);
                return lerp(lerp(lerp( hash(i+float3(0,0,0)), hash(i+float3(1,0,0)),f.x),
                                 lerp( hash(i+float3(0,1,0)), hash(i+float3(1,1,0)),f.x),f.y),
                            lerp(lerp( hash(i+float3(0,0,1)), hash(i+float3(1,0,1)),f.x),
                                 lerp( hash(i+float3(0,1,1)), hash(i+float3(1,1,1)),f.x),f.y),f.z);
            }

            // FBM Optimisé (2 octaves au lieu de 3)
            float fbm(float3 p) {
                float f = 0.0; 
                f += 0.5 * noise(p); p *= 2.02;
                f += 0.25 * noise(p); 
                return f;
            }

            float map(float3 p) {
                // Bords doux
                float edgeFade = smoothstep(0.5, 0.4, abs(p.x)) * smoothstep(0.5, 0.4, abs(p.z));
                
                // Forme
                float3 q = p * _Scale + _Speed * _Time.y;
                float baseCloud = fbm(q);
                float detail = noise(p * _DetailScale + _Speed * _Time.y);
                float d = baseCloud - (detail * _DetailStrength) - 0.2; // -0.2 pour aérer
                
                // Sol
                float groundFade = smoothstep(0.0, _GroundFade, p.y + 0.5);
                float ceilFade = smoothstep(0.0, 0.1, 0.5 - p.y);
                
                if (d < 0.0) return 0.0;
                return d * _Density * groundFade * ceilFade * edgeFade;
            }

            float getLight(float3 p, float3 lightDir) {
                // Un seul sample pour l'ombre (très rapide)
                float dens = map(p + lightDir * 0.1);
                return exp(-dens * _Absorption);
            }

            float2 boxIntersection(float3 ro, float3 rd) {
                float3 m = 1.0 / rd;
                float3 n = m * ro;
                float3 k = abs(m) * 0.5;
                float3 t1 = -n - k;
                float3 t2 = -n + k;
                float tN = max(max(t1.x, t1.y), t1.z);
                float tF = min(min(t2.x, t2.y), t2.z);
                if(tN > tF || tF < 0.0) return float2(-1.0, -1.0);
                return float2(tN, tF);
            }

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                o.viewDir = v.vertex.xyz - mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 rd = normalize(i.viewDir);
                
                float2 bounds = boxIntersection(ro, rd);
                if (bounds.x < 0.0 && bounds.y < 0.0) discard;
                
                float t = max(0.0, bounds.x);
                float tMax = bounds.y;
                
                // OPTIMISATION : Pas plus grand pour un cube unitaire
                float stepSize = 0.05; 
                float4 colAcc = float4(0,0,0,0);
                float3 lightDir = normalize(_LightDir);
                
                // Boucle simple sans unroll, limite 40 pas
                for(int k=0; k<40; k++) {
                    if(t > tMax || colAcc.a >= 0.95) break;
                    
                    float3 p = ro + rd * t;
                    
                    // Vérif simple
                    if(abs(p.x) < 0.5 && abs(p.y) < 0.5 && abs(p.z) < 0.5) {
                        float dens = map(p);
                        if(dens > 0.01) {
                            float lightVal = getLight(p, lightDir);
                            float3 cloudColor = lerp(_ShadowColor.rgb, _MainColor.rgb, lightVal);
                            float a = 1.0 - exp(-dens * stepSize * 15.0);
                            
                            colAcc.rgb += cloudColor * a * (1.0 - colAcc.a);
                            colAcc.a += a * (1.0 - colAcc.a);
                        }
                    }
                    t += stepSize;
                }
                
                if(colAcc.a < 0.01) discard;
                return colAcc;
            }
            ENDCG
        }
    }
}