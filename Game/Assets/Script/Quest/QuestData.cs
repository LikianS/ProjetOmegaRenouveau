using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
public class QuestDataScriptable : ScriptableObject
{
    public string questName;
    public string description;
    public List<QuestStep> steps = new List<QuestStep>();
    public int goldReward;
    public int achievementPointsReward;
    public List<StatReward> statRewards = new List<StatReward>();
    public bool isCompleted;
    public bool isStarted;

    public QuestStep GetCurrentStep()
    {
        foreach (var step in steps)
        {
            if (step.isStarted && !step.isCompleted)
            {
                return step;
            }
        }
        return null;
    }
}
