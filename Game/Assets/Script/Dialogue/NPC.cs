using System;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName;
    public string shopType; 
    public List<DialogueGroup> dialogueGroups = new List<DialogueGroup>();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.0f);
    }
}

[Serializable]
public class DialogueGroup
{
    public DialogueData dialogueData;
    public List<QuestCondition> conditions = new List<QuestCondition>();
    public bool isActive = true;
    public int requiredAchievementPoints = 0;
}