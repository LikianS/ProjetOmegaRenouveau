using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public int startDialogueId;
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

[Serializable]
public class DialogueLine
{
    public int id;
    public string speakerName;
    public string dialogueText;
    public bool isPlayerSpeaking;
    public int nextDialogueId = -1; 
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    public bool opensShop = false;
    public string shopType = "";
    public bool opensQuest = false;
    public QuestDataScriptable quest;
    public bool updatesQuestStep = false;
    public int questStepId;

    public List<QuestCondition> requiredQuests = new List<QuestCondition>();
    public int fallbackDialogueId = -1;

    public bool isActive = true;
    public int disableDialogueId = -1;
    public List<int> disableDialogueGroups = new List<int>();

}

[Serializable]
public class DialogueChoice
{
    public string choiceText;
    public int nextDialogueId;
    public bool opensShop = false;
    public string shopType = "";
}

[Serializable]
public class QuestCondition
{
    public QuestDataScriptable quest; 
    public QuestConditionType conditionType;
    public int requiredStepId = -1;

    public enum QuestConditionType
    {
        Completed,
        InProgress,
        NotStarted,
        IsStarted
    }
}