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
        if (!CanStartQuest(quest)) return;

        Debug.Log($"Ajout de la qu�te : {quest.questName}");

        ResetQuestSteps(quest);
        StartFirstQuestStep(quest);

        quest.isStarted = true;
        activeQuests.Add(quest);

        SaveQuestProgress();
        UpdateQuestUI();
        ShowQuestNotification($"Nouvelle qu�te : {quest.questName}");
    }

    public void UpdateQuestStep(QuestDataScriptable quest, int stepId)
    {
        if (!CanUpdateQuest(quest)) return;

        int stepIndex = FindQuestStepIndex(quest, stepId);

        if (stepIndex >= 0 && !quest.steps[stepIndex].isCompleted)
        {
            QuestStep step = quest.steps[stepIndex];
            Debug.Log($"Mise � jour de l'�tape : {step.stepDescription} pour la qu�te : {quest.questName}");
            step.isCompleted = true;

            StartNextQuestStep(quest, stepIndex);

            if (AreAllQuestStepsCompleted(quest))
            {
                CompleteQuest(quest);
            }
            SaveQuestProgress();
        }
        else
        {
            Debug.LogWarning($"�tape introuvable ou d�j� compl�t�e : ID {stepId} dans la qu�te {quest.questName}");
        }
    }


    public void CompleteQuest(QuestDataScriptable quest)
    {
        Debug.Log($"Qu�te termin�e : {quest.questName}");
        activeQuests.Remove(quest);

        playerStats.baseStats.gold += quest.goldReward;
        foreach (var reward in quest.statRewards)
        {
            playerStats.IncreaseStat(reward.statName, reward.value);
        }

        rewardText.text = $"R�compenses :\nOr : {quest.goldReward}\nPoints d'achievement : {quest.achievementPointsReward}";
        foreach (var reward in quest.statRewards)
        {
            rewardText.text += $"\n{reward.statName} : +{reward.value}";
        }
        rewardUI.SetActive(true);
        UpdateRewardText(quest);

        ShowQuestNotification($"Qu�te termin�e : {quest.questName}");

        DialogueSystem dialogueSystem = FindAnyObjectByType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            dialogueSystem.EndDialogue(); 
        }

        quest.isCompleted = true;

        StartCoroutine(HideRewardUIAfterDelay(5f));

        UpdateQuestUI();
        SaveQuestProgress();
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
        return "Termin�";
    }

    private void ShowQuestNotification(string message)
    {
        notificationText.text = message;
        if (SoundManager.Instance != null)
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

    private bool CanStartQuest(QuestDataScriptable quest)
    {
        if (quest.isCompleted)
        {
            Debug.LogWarning($"La qu�te '{quest.questName}' a d�j� �t� termin�e.");
            return false;
        }

        if (quest.isStarted)
        {
            Debug.LogWarning($"La qu�te '{quest.questName}' est d�j� en cours.");
            return false;
        }

        return true;
    }

    private bool CanUpdateQuest(QuestDataScriptable quest)
    {
        if (quest.isCompleted)
        {
            Debug.LogWarning($"La qu�te '{quest.questName}' est d�j� termin�e.");
            return false;
        }

        return true;
    }

    private void ResetQuestSteps(QuestDataScriptable quest)
    {
        foreach (var step in quest.steps)
        {
            step.isStarted = false;
            step.isCompleted = false;
        }
    }

    private void StartFirstQuestStep(QuestDataScriptable quest)
    {
        if (quest.steps.Count > 0)
        {
            quest.steps[0].isStarted = true;
        }
    }

    private int FindQuestStepIndex(QuestDataScriptable quest, int stepId)
    {
        for (int i = 0; i < quest.steps.Count; i++)
        {
            if (quest.steps[i].stepId == stepId)
            {
                return i;
            }
        }

        return -1;
    }

    private void StartNextQuestStep(QuestDataScriptable quest, int completedStepIndex)
    {
        int nextStepIndex = completedStepIndex + 1;
        if (nextStepIndex < quest.steps.Count)
        {
            quest.steps[nextStepIndex].isStarted = true;
        }
    }

    private bool AreAllQuestStepsCompleted(QuestDataScriptable quest)
    {
        foreach (var step in quest.steps)
        {
            if (!step.isCompleted)
                return false;
        }

        return true;
    }

    private void UpdateRewardText(QuestDataScriptable quest)
    {
        rewardText.text = $"R�compenses :\nOr : {quest.goldReward}\nPoints d'achievement : {quest.achievementPointsReward}";
        foreach (var reward in quest.statRewards)
        {
            rewardText.text += $"\n{reward.statName} : +{reward.value}";
        }
    }

    private void SaveQuestProgress()
    {
        SaveData saveData = SaveManager.LoadGame();
        SaveQuestData(saveData);
        SaveManager.SaveGame(saveData);
    }

}
