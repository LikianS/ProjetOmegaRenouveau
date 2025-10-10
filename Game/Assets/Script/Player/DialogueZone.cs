using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueZone : MonoBehaviour
{
    [Header("Dialogue à lancer")]
    public DialogueData dialogueData;

    private bool triggered = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        var player = other.GetComponent<DualAnimPlayerController>();
        if (player != null && dialogueData != null)
        {
            DialogueSystem dialogueSystem = FindAnyObjectByType<DialogueSystem>();
            if (dialogueSystem != null)
            {
                triggered = true;
                dialogueSystem.StartDialogueFromData(dialogueData, OnDialogueEnd);
            }
        }
    }

    private void OnDialogueEnd()
    {
        gameObject.SetActive(false);
    }
}

