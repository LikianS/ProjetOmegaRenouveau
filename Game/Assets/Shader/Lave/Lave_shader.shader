Shader "Custom/ToonLavaComplete" {
    Properties {
        [Header(Colors)]
        _DeepColor ("Deep Color (Red)", Color) = (0.7, 0.1, 0, 1)
        _OutlineColor ("Outline Color (White)", Color) = (1, 1, 0.8, 1)
        _SurfaceColor ("Surface Color (Orange)", Color) = (1, 0.6, 0.0, 1)
        _FoamColor ("Impact Foam Color", Color) = (1, 1, 0.8, 1)
        _ExplosionColor ("Explosion Color", Color) = (1, 0.9, 0.5, 1) // AJOUT EXPLOSION

        [Header(Movement)]
        _FlowDirection ("Flow Direction (X, Y)", Vector) = (0, 1, 0, 0)
        _Speed ("Flow Speed", Float) = 0.5
        _NoiseScale ("Pattern Scale", Float) = 0.5
        
        [Header(Explosions)] // AJOUT EXPLOSION
        _ExplosionScale ("Explosion Grid Size", Float) = 3.0
        _ExplosionSpeed ("Explosion Speed", Float) = 2.0
        _ExplosionFrequency ("Explosion Frequency", Range(0,1)) = 0.7
        _ExplosionHeight ("Explosion Height Impact", Float) = 0.5

        [Header(Wave)]
        _WaveHeight ("Global Wave Height", Float) = 0.2
        _WaveSpeed ("Global Wave Speed", Float) = 1.0

        [Header(Style)]
        _Threshold ("Lava Fill Amount", Range(0,1)) = 0.55
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.05
        _Smoothness ("Edge Softness", Range(0.001, 0.1)) = 0.01
        _ImpactSize ("Impact Width", Range(0, 1)) = 0.3
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float explosionVal : TEXCOORD3; // Pour passer l'info explosion au pixel shader
            };

            sampler2D _CameraDepthTexture;
            // Variables fusionnées
            float4 _DeepColor, _SurfaceColor, _FoamColor, _OutlineColor, _ExplosionColor;
            float2 _FlowDirection;
            float _Speed, _NoiseScale, _Threshold, _Smoothness, _ImpactSize, _OutlineWidth;
            float _WaveHeight, _WaveSpeed;
            float _ExplosionScale, _ExplosionSpeed, _ExplosionFrequency, _ExplosionHeight;

            // -- OUTILS MATHS (Pour les explosions) --
            float2 hash2(float2 p) {
                return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
            }
            float rand(float2 p) {
                return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453);
            }

            // -- BRUIT SIMPLEX (Pour la lave) --
            float2 hash( float2 p ) {
                p = float2( dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)) );
                return -1.0 + 2.0*frac(sin(p)*43758.5453123);
            }

            float noise( in float2 p ) {
                const float K1 = 0.366025404; 
                const float K2 = 0.211324865; 
                float2 i = floor( p + (p.x+p.y)*K1 );
                float2 a = p - i + (i.x+i.y)*K2;
                float2 o = (a.x>a.y) ? float2(1.0,0.0) : float2(0.0,1.0);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0*K2;
                float3 h = max( 0.5-float3(dot(a,a), dot(b,b), dot(c,c) ), 0.0 );
                float3 n = h*h*h*h*float3( dot(a,hash(i+0.0)), dot(b,hash(i+o)), dot(c,hash(i+1.0)));
                return dot( n, float3(70.0, 70.0, 70.0) );
            }

            // -- LOGIQUE EXPLOSION --
            float explosionLayer(float2 uv) {
                float2 gridID = floor(uv);
                float2 gridUV = frac(uv);
                float totalExplosion = 0;

                for(int y=-1; y<=1; y++) {
                    for(int x=-1; x<=1; x++) {
                        float2 neighbor = float2(x, y);
                        float2 id = gridID + neighbor;
                        float2 p = hash2(id); 
                        float timeOffset = p.x * 10.0;
                        float t = frac(_Time.y * _ExplosionSpeed * 0.5 + timeOffset);
                        float cycleRand = rand(id + floor(_Time.y * _ExplosionSpeed * 0.5));
                        
                        if(cycleRand > _ExplosionFrequency) {
                            float2 center = neighbor + p; 
                            float dist = length(center - gridUV);
                            float radius = t * 1.5; 
                            float thickness = 0.1 * (1.0 - t); 
                            float ring = smoothstep(radius, radius - thickness, dist);
                            float hole = smoothstep(radius - thickness * 2.0, radius - thickness * 3.0, dist);
                            ring *= (1.0 - hole);
                            ring *= smoothstep(1.0, 0.5, t);
                            totalExplosion += ring;
                        }
                    }
                }
                return totalExplosion;
            }

            v2f vert (appdata v) {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // 1. Calcul de l'explosion (Hauteur)
                float2 explodeUV = o.worldPos.xz * _ExplosionScale * 0.2;
                float boom = explosionLayer(explodeUV);
                o.explosionVal = boom; // On envoie l'info au Pixel Shader

                // 2. Calcul des vagues (Hauteur)
                float wave = sin(v.vertex.x * 2.0 + _Time.y * _WaveSpeed) * cos(v.vertex.z * 1.5 + _Time.y * _WaveSpeed * 0.8);
                
                // 3. Application de la hauteur totale (Vague + Explosion)
                v.vertex.y += wave * _WaveHeight + (boom * _ExplosionHeight);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // --- A. FLUX ET BRUIT DE LAVE ---
                float2 flow = normalize(_FlowDirection) * _Time.y * _Speed;
                float n1 = noise(i.worldPos.xz * _NoiseScale + flow);
                float n2 = noise(i.worldPos.xz * (_NoiseScale * 1.5) - flow * 0.5);
                float finalNoise = n1 * 0.7 + n2 * 0.3;
                finalNoise = finalNoise * 0.5 + 0.5;

                // --- B. COULEURS DE BASE (Avec Outline) ---
                // Masque contour
                float borderMask = smoothstep(_Threshold - _OutlineWidth, (_Threshold - _OutlineWidth) + _Smoothness, finalNoise);
                // Masque surface
                float surfaceMask = smoothstep(_Threshold, _Threshold + _Smoothness, finalNoise);

                float3 col = _DeepColor.rgb; // 1. Fond
                col = lerp(col, _OutlineColor.rgb, borderMask); // 2. Ajout Contour Blanc
                col = lerp(col, _SurfaceColor.rgb, surfaceMask); // 3. Ajout Surface Orange

                // --- C. AJOUT EXPLOSION (Par dessus la lave) ---
                float2 explodeUV = i.worldPos.xz * _ExplosionScale * 0.2;
                float boom = explosionLayer(explodeUV); // Recalcul pour netteté
                col = lerp(col, _ExplosionColor.rgb, boom);

                // --- D. IMPACT OBJETS (Le plus fort, tout à la fin) ---
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                float partZ = i.screenPos.w;
                float diff = sceneZ - partZ;

                if(diff > 0 && diff < _ImpactSize) {
                    col = _FoamColor.rgb; 
                }

                return fixed4(col, 1.0);
            }
            ENDHLSL
        }
    }
}