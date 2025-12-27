using UnityEngine;
using UnityEditor;

public class RoundCloudGen : MonoBehaviour
{
    [MenuItem("Tools/Generate Round Cloud Texture")]
    static void CreateTexture()
    {
        int size = 64; 
        Texture3D texture = new Texture3D(size, size, size, TextureFormat.R8, false);
        texture.wrapMode = TextureWrapMode.Repeat; // IMPORTANT : Repeat par défaut
        texture.filterMode = FilterMode.Bilinear;

        Color[] cols = new Color[size * size * size];
        int idx = 0;
        float scale = 4.0f; // Taille des boules

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Coordonnées UVW
                    float u = (float)x / size * scale;
                    float v = (float)y / size * scale;
                    float w = (float)z / size * scale;

                    // On génère des boules (Worley Noise inversé)
                    float dist = GetWorley(u, v, w);
                    
                    // Inversion : 1 au centre de la boule, 0 au bord
                    float val = 1.0f - dist;
                    val = Mathf.Clamp01(val);
                    
                    // On applique une courbe pour rendre la boule "bombeé"
                    val = val * val; 

                    cols[idx] = new Color(val, val, val, 1);
                    idx++;
                }
            }
        }

        texture.SetPixels(cols);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, "Assets/RoundCloudTex.asset");
        Debug.Log("Texture 'RoundCloudTex' générée ! Vérifie qu'elle est en Repeat !");
    }

    static float GetWorley(float x, float y, float z)
    {
        Vector3 p = new Vector3(x, y, z);
        Vector3 id = new Vector3(Mathf.Floor(x), Mathf.Floor(y), Mathf.Floor(z));
        float minDist = 1.0f;

        for (int k = -1; k <= 1; k++) {
            for (int j = -1; j <= 1; j++) {
                for (int i = -1; i <= 1; i++) {
                    Vector3 offset = new Vector3(i, j, k);
                    Vector3 h = Hash33(id + offset);
                    // On anime pas, on veut des boules fixes bien placées
                    Vector3 pDiff = (offset + h) - (p - id);
                    float d = Vector3.Dot(pDiff, pDiff);
                    if (d < minDist) minDist = d;
                }
            }
        }
        return Mathf.Sqrt(minDist);
    }

    static Vector3 Hash33(Vector3 p)
    {
        p = new Vector3(
            Vector3.Dot(p, new Vector3(127.1f, 311.7f, 74.7f)),
            Vector3.Dot(p, new Vector3(269.5f, 183.3f, 246.1f)),
            Vector3.Dot(p, new Vector3(113.5f, 271.9f, 124.6f))
        );
        return new Vector3(
            Mathf.Sin(p.x) * 43758.5453f % 1,
            Mathf.Sin(p.y) * 43758.5453f % 1,
            Mathf.Sin(p.z) * 43758.5453f % 1
        );
    }
}