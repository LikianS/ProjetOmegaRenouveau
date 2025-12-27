using UnityEngine;

public class DungeonEmotionInput : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            GlobalEmotionManager.Instance.SetDungeonEmotion(Emotion.Colere);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            GlobalEmotionManager.Instance.SetDungeonEmotion(Emotion.Tristesse);

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            GlobalEmotionManager.Instance.SetDungeonEmotion(Emotion.Stress);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            GlobalEmotionManager.Instance.SetDungeonEmotion(Emotion.Joie);
    }
}
