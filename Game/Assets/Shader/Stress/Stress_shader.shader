Shader "Hidden/GlitchEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0, 1)) = 0
        _NoiseAmount ("Static Noise", Range(0, 1)) = 0.5
        _TearingSpeed ("Tearing Speed", Float) = 20.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Intensity;
            float _NoiseAmount;
            float _TearingSpeed;

            // Fonction aléatoire simple
            float rand(float2 co){
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Si l'intensité est nulle, on rend l'image normale
                if(_Intensity <= 0.001) return tex2D(_MainTex, i.uv);

                float2 uv = i.uv;
                float time = _Time.y * _TearingSpeed;

                // --- 1. TEARING (Déchirure horizontale) ---
                
                // On crée des blocs verticaux aléatoires
                // Plus le chiffre 10.0 est grand, plus les bandes sont fines
                float splitY = floor(uv.y * 10.0 + time * 0.5); 
                
                // On génère une valeur aléatoire pour chaque bande
                float stripNoise = rand(float2(splitY, _Time.y));
                
                // Seuil : est-ce que cette bande doit être décalée ?
                // Plus l'intensité est forte, plus de bandes seront décalées
                if(stripNoise < _Intensity) 
                {
                    // Décalage brutal sur l'axe X
                    float shift = (rand(float2(time, splitY)) - 0.5) * _Intensity * 0.2;
                    uv.x += shift;
                }

                // --- 2. COULEUR DE BASE ---
                fixed4 col = tex2D(_MainTex, uv);

                // --- 3. BROUILLÉ (Static Noise / Neige) ---
                // Bruit pixel par pixel qui change à chaque frame
                float staticNoise = rand(i.uv + float2(_Time.y, _Time.w));
                
                // On mélange le bruit si nécessaire (mode "Additif" ou "Mix")
                // Ici on ajoute du gris par dessus
                float noiseVisibility = _Intensity * _NoiseAmount;
                
                if (staticNoise < noiseVisibility) {
                    // On assombrit ou éclaircit aléatoirement pour faire "sale"
                    col.rgb += (staticNoise - 0.5) * 0.5;
                }

                // --- 4. EFFET DE BANDE RGB (Optionnel : rend le glitch plus "agressif") ---
                // On sépare légèrement le canal rouge lors des gros glitchs
                if(stripNoise < _Intensity * 0.5) {
                    col.r = tex2D(_MainTex, uv + float2(0.02 * _Intensity, 0)).r;
                }

                return col;
            }
            ENDCG
        }
    }
}