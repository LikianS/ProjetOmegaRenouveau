using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DungeonPostProcessing : MonoBehaviour
{
    public Volume volume;
    private ColorAdjustments colorAdj;

    void Start()
    {
        volume.profile.TryGet<ColorAdjustments>(out colorAdj);

        if (!volume.profile.TryGet<ColorAdjustments>(out colorAdj))
        {
            Debug.LogError("ColorAdjustments introuvable !");
        }
        else
        {
            Debug.Log("ColorAdjustments trouvé !");
            GlobalEmotionManager.Instance.OnEmotionChanged += OnEmotionChanged;
            GlobalEmotionManager.Instance.SetDungeonEmotion(Emotion.Colere);
        }
    }

    void OnEmotionChanged(Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.Colere: colorAdj.colorFilter.value = new Color(1f, 1f, 1f); ; break;
            case Emotion.Tristesse: colorAdj.colorFilter.value = new Color(0.3f, 0.5f, 0.8f); break;
            case Emotion.Joie: colorAdj.colorFilter.value = new Color(1f, 0.95f, 0.6f); break;
            case Emotion.Stress: colorAdj.colorFilter.value = Color.gray; break;
            case Emotion.None: colorAdj.colorFilter.value = Color.white; break;
        }
    }
}
