Shader "Custom/ToonWaterComplete" {
    Properties {
        [Header(Colors)]
        _ShallowColor ("Shallow Color (Light Blue)", Color) = (0.3, 0.7, 1, 0.5)
        _DeepColor ("Deep Color (Dark Blue)", Color) = (0.0, 0.2, 0.5, 0.9)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)

        [Header(Depth and Transparency)]
        _DepthFactor ("Depth Factor", Range(0.01, 2)) = 1.0
        _FoamSize ("Foam Shore Size", Range(0, 1)) = 0.5
        _SurfaceNoiseCutoff ("Surface Foam Amount", Range(0, 1)) = 0.75

        [Header(Movement)]
        _Speed ("Flow Speed", Float) = 0.8
        _NoiseScale ("Ripple Scale", Float) = 5.0
        
        [Header(Waves)]
        _WaveHeight ("Wave Height", Float) = 0.3
        _WaveFrequency ("Wave Frequency", Float) = 1.0
        _WaveSpeed ("Wave Speed", Float) = 1.5
    }
    SubShader {
        // Configuration pour la transparence
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Pas d'écriture dans le Z-Buffer pour la transparence, mais on teste la profondeur
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 screenPos : TEXCOORD0; // Pour calculer la profondeur
                float3 worldPos : TEXCOORD1;
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture); // Texture de profondeur automatique d'Unity

            float4 _ShallowColor, _DeepColor, _FoamColor;
            float _DepthFactor, _FoamSize, _SurfaceNoiseCutoff;
            float _Speed, _NoiseScale;
            float _WaveHeight, _WaveFrequency, _WaveSpeed;

            // --- FONCTIONS DE BRUIT (Simplex Noise) ---
            float2 hash( float2 p ) {
                p = float2( dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)) );
                return -1.0 + 2.0*frac(sin(p)*43758.5453123);
            }

            float noise( in float2 p ) {
                const float K1 = 0.366025404; const float K2 = 0.211324865;
                float2 i = floor( p + (p.x+p.y)*K1 );
                float2 a = p - i + (i.x+i.y)*K2;
                float2 o = (a.x>a.y) ? float2(1.0,0.0) : float2(0.0,1.0);
                float2 b = a - o + K2; float2 c = a - 1.0 + 2.0*K2;
                float3 h = max( 0.5-float3(dot(a,a), dot(b,b), dot(c,c) ), 0.0 );
                float3 n = h*h*h*h*float3( dot(a,hash(i+0.0)), dot(b,hash(i+o)), dot(c,hash(i+1.0)));
                return dot( n, float3(70.0, 70.0, 70.0) );
            }

            v2f vert (appdata v) {
                v2f o;
                
                // 1. Déplacement des sommets (Vagues physiques)
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // Formule de vague un peu plus "pointue" que la lave pour l'eau
                float wave = sin(worldPos.x * _WaveFrequency + _Time.y * _WaveSpeed) 
                           * cos(worldPos.z * _WaveFrequency * 0.8 + _Time.y * _WaveSpeed * 1.2);
                
                v.vertex.y += wave * _WaveHeight;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = worldPos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // --- 1. CALCUL DE LA PROFONDEUR (Shoreline) ---
                // On lit la profondeur de la scène derrière l'eau
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                // On lit la profondeur du pixel de l'eau
                float partZ = i.screenPos.w;
                // La différence nous dit si on est proche du sol (bord de l'eau)
                float depthDiff = sceneZ - partZ;

                // --- 2. BRUIT DE SURFACE (Ripples) ---
                float2 movement = float2(_Time.y * _Speed, _Time.y * _Speed * 0.5);
                float n1 = noise(i.worldPos.xz * _NoiseScale + movement);
                float n2 = noise(i.worldPos.xz * _NoiseScale * 0.8 - movement * 1.2); // Deuxième couche inverse
                float ripples = n1 * 0.5 + n2 * 0.5; // Mélange

                // --- 3. COULEUR DE BASE (Profondeur) ---
                // Si la profondeur est grande, on tend vers DeepColor, sinon ShallowColor
                // saturate() empêche les valeurs négatives
                float waterDepthFactor = saturate(depthDiff / _DepthFactor);
                float4 waterCol = lerp(_ShallowColor, _DeepColor, waterDepthFactor);

                // --- 4. ECUME (FOAM) ---
                
                // A. Ecume de rivage (Shoreline) : si depthDiff est petit
                float shoreFoam = step(depthDiff, _FoamSize); 
                
                // B. Ecume de surface (Haut des vagues / Texture)
                // On ajoute de l'écume là où le bruit est fort
                float surfaceFoam = step(_SurfaceNoiseCutoff, ripples);

                // Combiner l'écume
                float totalFoam = saturate(shoreFoam + surfaceFoam);

                // --- 5. RESULTAT FINAL ---
                float4 finalColor = lerp(waterCol, _FoamColor, totalFoam);
                
                // On force l'alpha de l'écume à 1 (opaque) même si l'eau est transparente
                finalColor.a = max(waterCol.a, totalFoam); 

                return finalColor;
            }
            ENDHLSL
        }
    }
}