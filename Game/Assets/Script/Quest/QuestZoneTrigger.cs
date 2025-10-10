using UnityEngine;

public class QuestZoneTrigger : MonoBehaviour
{
    [Header("Paramètres de la Zone de Quête")]
    public QuestDataScriptable questData;
    public QuestAction action;
    public int questStepId = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HandleQuest();
        }
    }

    private void HandleQuest()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }
        switch (action)
        {
            case QuestAction.Start:
                QuestManager.Instance.AddQuest(questData);
                break;

            case QuestAction.Update:
                QuestManager.Instance.UpdateQuestStep(questData, questStepId);
                break;

            case QuestAction.Complete:
                QuestManager.Instance.CompleteQuest(questData);
                break;
        }
        SaveData saveData = SaveManager.LoadGame();
        if (saveData == null)
        {
            saveData = new SaveData();
        }
        SaveManager.SaveGame(saveData);

        Destroy(gameObject);
    }
}

public enum QuestAction
{
    Start,
    Update,
    Complete
}
