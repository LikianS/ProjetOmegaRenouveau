using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class QuestManager : MonoBehaviour
{
    public List<QuestDataScriptable> activeQuests = new List<QuestDataScriptable>();
    public GameObject questUI;
    public Transform questListContainer;
    public GameObject questItemPrefab;
    public GameObject rewardUI;
    public TextMeshProUGUI rewardText;
    public GameObject questNotificationPanel;
    public TextMeshProUGUI notificationText;

    private PlayerStats playerStats;
    private AudioSource audioSource;
    public Animator notificationAnimator;
    public static QuestManager Instance { get; private set; }

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        questUI.SetActive(true);
        rewardUI.SetActive(false);
        questNotificationPanel.SetActive(false);

        SaveData saveData = SaveManager.LoadGame();

        if (saveData == null)
        {
            saveData = new SaveData();
        }

        LoadQuestData(saveData);

        playerStats.UpdatePlayerStatsUI();
    }



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        notificationText = questNotificationPanel.GetComponentInChildren<TextMeshProUGUI>();
        notificationAnimator = questNotificationPanel.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    public void AddQuest(QuestDataScriptable quest)
    {
        if (quest.isCompleted)
        {
            Debug.LogWarning($"La quête '{quest.questName}' a déjà été terminée.");
            return;
        }

        if (quest.isStarted)
        {
            Debug.LogWarning($"La quête '{quest.questName}' est déjà en cours.");
            return;
        }

        Debug.Log($"Ajout de la quête : {quest.questName}");

        foreach (var step in quest.steps)
        {
            step.isStarted = false;
            step.isCompleted = false;
        }

        if (quest.steps.Count > 0)
        {
            quest.steps[0].isStarted = true;
        }

        quest.isStarted = true;
        activeQuests.Add(quest);

        SaveData saveData = SaveManager.LoadGame();
        SaveQuestData(saveData);
        SaveManager.SaveGame(saveData);
        UpdateQuestUI();
        ShowQuestNotification($"Nouvelle quête : {quest.questName}");
    }

    public void UpdateQuestStep(QuestDataScriptable quest, int stepId)
    {
        if (quest.isCompleted)
        {
            Debug.LogWarning($"La quête '{quest.questName}' est déjà terminée.");
            return;
        }

        QuestStep step = quest.steps.Find(s => s.stepId == stepId);
        if (step != null && !step.isCompleted)
        {
            Debug.Log($"Mise à jour de l'étape : {step.stepDescription} pour la quête : {quest.questName}");
            step.isCompleted = true;

            int nextStepIndex = quest.steps.IndexOf(step) + 1;
            if (nextStepIndex < quest.steps.Count)
            {
                quest.steps[nextStepIndex].isStarted = true;
            }
            if (quest.steps.TrueForAll(s => s.isCompleted))
            {
                CompleteQuest(quest);
            }
            SaveData saveData = SaveManager.LoadGame();
            SaveQuestData(saveData);
            SaveManager.SaveGame(saveData);
        }
        else
        {
            Debug.LogWarning($"Étape introuvable ou déjà complétée : ID {stepId} dans la quête {quest.questName}");
        }
    }


    public void CompleteQuest(QuestDataScriptable quest)
    {
        Debug.Log($"Quête terminée : {quest.questName}");
        activeQuests.Remove(quest);

        playerStats.baseStats.gold += quest.goldReward;
        foreach (var reward in quest.statRewards)
        {
            playerStats.IncreaseStat(reward.statName, reward.value);
        }

        rewardText.text = $"Récompenses :\nOr : {quest.goldReward}\nPoints d'achievement : {quest.achievementPointsReward}";
        foreach (var reward in quest.statRewards)
        {
            rewardText.text += $"\n{reward.statName} : +{reward.value}";
        }
        rewardUI.SetActive(true);

        ShowQuestNotification($"Quête terminée : {quest.questName}");

        DialogueSystem dialogueSystem = FindAnyObjectByType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            dialogueSystem.EndDialogue(); 
        }

        quest.isCompleted = true;

        StartCoroutine(HideRewardUIAfterDelay(5f)); 

        UpdateQuestUI();

        SaveData saveData = SaveManager.LoadGame();
        SaveQuestData(saveData);
        SaveManager.SaveGame(saveData);
    }

    private IEnumerator HideRewardUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rewardUI.SetActive(false); 
    }

    public void UpdateQuestUI()
    {
        if (questListContainer == null)
            return;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in questListContainer)
        {
            if (child != null)
                children.Add(child);
        }

        foreach (Transform child in children)
        {
            if (child != null && child.gameObject != null)
                Destroy(child.gameObject);
        }

        foreach (var quest in activeQuests)
        {
            if (questItemPrefab == null || questListContainer == null)
                continue;

            GameObject questItem = Instantiate(questItemPrefab, questListContainer);
            TextMeshProUGUI questText = questItem.GetComponentInChildren<TextMeshProUGUI>();

            string stepDescription = GetCurrentStepDescription(quest);

            if (questText != null)
                questText.text = $"{quest.questName} - {stepDescription}";
        }
    }


    private string GetCurrentStepDescription(QuestDataScriptable quest)
    {
        foreach (var step in quest.steps)
        {
            if (!step.isCompleted)
            {
                if (step.stepType == QuestStep.QuestStepType.KillMonsters)
                {
                    return $"{step.stepDescription} ({step.currentKillCount}/{step.targetKillCount})";
                }
                else
                {
                    return step.stepDescription;
                }
            }
        }
        return "Terminé";
    }

    private void ShowQuestNotification(string message)
    {
        notificationText.text = message;
        SoundManager.Instance.PlayEffect(SoundManager.Instance.dialogueSound);
        notificationAnimator.SetTrigger("Show");
        questNotificationPanel.SetActive(true);
    }

    public void SaveQuestData(SaveData saveData)
    {
        saveData.quests.Clear();
        QuestDataScriptable[] allQuests = Resources.LoadAll<QuestDataScriptable>("Quete");
        foreach (var quest in allQuests)
        {
            QuestSaveData questSave = new QuestSaveData
            {
                questName = quest.questName,
                isStarted = quest.isStarted,
                isCompleted = quest.isCompleted,
                stepsCompleted = new List<bool>(),
                stepsStarted = new List<bool>()
            };

            foreach (var step in quest.steps)
            {
                questSave.stepsCompleted.Add(step.isCompleted);
                questSave.stepsStarted.Add(step.isStarted);
                questSave.stepsCurrentKillCount.Add(step.currentKillCount);
            }

            saveData.quests.Add(questSave);
        }
    }

    public void LoadQuestData(SaveData saveData)
    {
        activeQuests.Clear();

        QuestDataScriptable[] allQuests = Resources.LoadAll<QuestDataScriptable>("Quete");

        Dictionary<string, QuestDataScriptable> questDictionary = new Dictionary<string, QuestDataScriptable>();
        foreach (var quest in allQuests)
        {
            questDictionary[quest.questName] = quest;
        }

        foreach (var questSave in saveData.quests)
        {
            if (questDictionary.TryGetValue(questSave.questName, out QuestDataScriptable quest))
            {
                quest.isStarted = questSave.isStarted;
                quest.isCompleted = questSave.isCompleted;

                for (int i = 0; i < quest.steps.Count; i++)
                {
                    if (i < questSave.stepsCompleted.Count)
                    {
                        quest.steps[i].isCompleted = questSave.stepsCompleted[i];
                    }
                    if (i < questSave.stepsStarted.Count)
                    {
                        quest.steps[i].isStarted = questSave.stepsStarted[i];
                    }
                    if (i < questSave.stepsCurrentKillCount.Count)
                    {
                        quest.steps[i].currentKillCount = questSave.stepsCurrentKillCount[i];
                    }
                }
                if (quest.isStarted && !quest.isCompleted)
                {
                    activeQuests.Add(quest);
                }
            }
        }

        UpdateQuestUI();
    }

    public List<QuestData> GetQuestData()
    {
        List<QuestData> questDataList = new List<QuestData>();
        foreach (var quest in activeQuests)
        {
            questDataList.Add(new QuestData
            {
                questName = quest.questName,
                isCompleted = quest.isCompleted
            });
        }
        return questDataList;
    }
    public void RegisterKill(EnemyType enemyType)
    {
        foreach (var quest in activeQuests)
        {
            foreach (var step in quest.steps)
            {
                if (step.stepType == QuestStep.QuestStepType.KillMonsters && step.isStarted && !step.isCompleted)
                {
                    if (step.targetEnemyType == enemyType)
                    {
                        step.currentKillCount++;

                        if (step.currentKillCount >= step.targetKillCount)
                        {
                            UpdateQuestStep(quest, step.stepId);
                        }
                    }
                }
            }
        }
    }

}
