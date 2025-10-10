using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Quest
{
    public string questName;
    public string description;
    public List<QuestStep> steps = new List<QuestStep>();
    public bool isCompleted = false;
    public int goldReward;
    public int achievementPointsReward;
    public List<StatReward> statRewards = new List<StatReward>();
}

[System.Serializable]
public class QuestStep
{
    public int stepId;
    public string stepDescription;
    public QuestStepType stepType;
    public bool isStarted = false;
    public bool isCompleted = false;
    public NPC targetNPC;
    public int targetKillCount;
    public int currentKillCount;
    public EnemyType targetEnemyType;

    public enum QuestStepType
    {
        TalkToNPC,
        KillMonsters,
        EscortNPC
    }
}

[System.Serializable]
public class StatReward
{
    public string statName;
    public float value;
}
