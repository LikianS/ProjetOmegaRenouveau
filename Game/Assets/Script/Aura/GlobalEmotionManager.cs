using UnityEngine;
using System;

public class GlobalEmotionManager : MonoBehaviour
{
    public static GlobalEmotionManager Instance;

    public Emotion currentDungeonEmotion;

    public event Action<Emotion> OnEmotionChanged;

    void Awake()
    {
        Debug.Log("GlobalEmotionManager Awake"); 
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public void SetDungeonEmotion(Emotion newEmotion)
    {
        currentDungeonEmotion = newEmotion;
        OnEmotionChanged?.Invoke(newEmotion);
        Debug.Log("Émotion du donjon : " + newEmotion);
    }
}
