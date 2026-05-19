using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;

public class DialogueSystem : MonoBehaviour
{
    [Header("References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject choicesPanel;
    public Transform choicesContainer;
    public GameObject choicePrefab;
    public Transform playerTransform;

    [Header("Settings")]
    public float typingSpeed = 0.03f;
    public float minDistance = 2.0f;
    public string interactButtonText = "A";
    [SerializeField, Min(0.05f)]
    private float npcCheckInterval = 0.2f;

    private Camera mainCamera;
    private PlayerController playerController;
    private DualAnimPlayerController dualAnimPlayerController;
    private NPC currentNPC;
    private DialogueData currentDialogue;
    private DialogueLine currentLine;
    private bool isInDialogue = false;
    private bool isTyping = false;
    private int currentChoiceIndex = 0;
    private List<GameObject> choiceObjects = new List<GameObject>();
    private PlayerInput playerInput;
    private ShopManager shopManager;
    private QuestManager questManager;
    private PlayerStats playerStats;
    private readonly Dictionary<int, DialogueLine> dialogueLinesById = new Dictionary<int, DialogueLine>();
    private Coroutine typingCoroutine;
    private Coroutine highlightChoiceCoroutine;
    private Action pendingOnDialogueEnd;

    [Header("Selection Indicator")]
    public GameObject selectionArrow;
    private float inputCooldown = 0.2f;
    private float lastInputTime = 0f;

    public Color normalColor = Color.white;

    private float choiceAppearTime = 0f;
    private float minChoiceDisplayTime = 0.15f;
    private bool justDisplayedChoices = false;
    private float npcCheckTimer;


    private void Start()
    {
        mainCamera = Camera.main;
        playerController = FindAnyObjectByType<PlayerController>();
        dualAnimPlayerController = FindAnyObjectByType<DualAnimPlayerController>();
        playerInput = FindAnyObjectByType<PlayerInput>();
        shopManager = FindAnyObjectByType<ShopManager>();
        questManager = FindAnyObjectByType<QuestManager>();
        playerStats = FindAnyObjectByType<PlayerStats>();
        InteractionManager.Instance.HideInteraction();
        dialoguePanel.SetActive(false);
        choicesPanel.SetActive(false);
    }


    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EnsureRuntimeReferences();
            if (shopManager != null && shopManager.shopPanel.activeSelf)
                return;

            if (currentNPC != null && !isInDialogue)
            {
                StartDialogue();
            }
            else if (isInDialogue)
            {
                if (isTyping)
                {
                    StopTypingCoroutine();
                    dialogueText.text = currentLine.dialogueText;
                    isTyping = false;

                    if (currentLine.choices.Count > 0)
                    {
                        ShowChoices();
                    }
                }
                else if (choicesPanel.activeSelf)
                {
                    if (justDisplayedChoices)
                    {
                        justDisplayedChoices = false;
                        return;
                    }
                    if (Time.unscaledTime - choiceAppearTime < minChoiceDisplayTime)
                        return;
                    SelectChoice(currentChoiceIndex);
                }
                else if (currentLine.choices.Count == 0)
                {
                    ProgressDialogue();
                }
            }
        }
    }

    private void ProgressDialogue()
    {
        EnsureRuntimeReferences();

        if (currentLine.opensShop)
        {
            if (shopManager != null)
            {
                EndDialogue();
                shopManager.OpenShop(currentLine.shopType);
                return;
            }
            else
            {
                EndDialogue();
            }
        }

        if (currentLine.opensQuest)
        {
            if (currentLine.quest is QuestDataScriptable questData)
            {
                questManager.AddQuest(questData);
            }
        }

        if (currentLine.updatesQuestStep)
        {
            if (currentLine.quest is QuestDataScriptable questData)
            {
                questManager.UpdateQuestStep(questData, currentLine.questStepId);
            }
        }

        if (currentLine.nextDialogueId >= 0)
        {
            ShowDialogueLine(currentLine.nextDialogueId);
        }
        else
        {
            EndDialogue();
        }
    }


    private void Update()
    {
        if (!isInDialogue)
        {
            npcCheckTimer -= Time.deltaTime;
            if (npcCheckTimer > 0f) return;
            npcCheckTimer = npcCheckInterval;
            CheckForNearbyNPC();
        }
    }

    private void CheckForNearbyNPC()
    {
        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, minDistance);
        NPC nearestNPC = null;
        float nearestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            NPC npc = collider.GetComponent<NPC>();
            if (npc != null)
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNPC = npc;
                }
            }
        }

        if (nearestNPC != null)
        {
            if (currentNPC != nearestNPC)
            {
                if (currentNPC != null)
                {
                    InteractionManager.Instance.HideInteraction();
                }

                currentNPC = nearestNPC;
                InteractionManager.Instance.ShowInteraction($"Appuyer sur {interactButtonText} pour parler");
                InteractionManager.Instance.PositionInteractionUI(currentNPC.transform.position + Vector3.up * 2.0f);
            }
        }
        else
        {
            if (currentNPC != null)
            {
                InteractionManager.Instance.HideInteraction();
                currentNPC = null;
            }
        }
    }

    private void StartDialogue()
    {
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Dialogue");

        if (currentNPC == null) return;

        DialogueData validDialogue = GetValidDialogueData(currentNPC);
        if (validDialogue == null)
        {
            return;
        }

        currentDialogue = validDialogue;
        BuildDialogueLineLookup(currentDialogue);
        isInDialogue = true;
        pendingOnDialogueEnd = null;

        if (playerController != null)
            playerController.SetDialogueMode(isInDialogue);
        if (dualAnimPlayerController != null)
            dualAnimPlayerController.SetDialogueMode(isInDialogue);

        InteractionManager.Instance.HideInteraction();
        ShowDialogueLine(currentDialogue.startDialogueId);
    }

    private void ShowDialogueLine(int dialogueId)
    {
        if (!TryGetDialogueLine(dialogueId, out currentLine))
        {
            EndDialogue();
            return;
        }

        if (currentLine.disableDialogueGroups != null && currentLine.disableDialogueGroups.Count > 0)
        {
            foreach (int groupIndex in currentLine.disableDialogueGroups)
            {
                SetDialogueGroupActive(currentNPC, groupIndex, false);
            }
        }

        if (!currentLine.isActive)
        {
            if (currentLine.fallbackDialogueId != -1)
            {
                ShowDialogueLine(currentLine.fallbackDialogueId); 
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        if (currentLine.disableDialogueId != -1)
        {
            SetDialogueActive(currentLine.disableDialogueId, false);
        }

        if (!AreConditionsMet(currentLine))
        {
            if (currentLine.fallbackDialogueId != -1)
            {
                ShowDialogueLine(currentLine.fallbackDialogueId);
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        if (currentLine.isPlayerSpeaking || currentNPC == null)
        {
            PositionUIElement(dialoguePanel, playerTransform.position + Vector3.up * 2.2f);
        }
        else
        {
            PositionUIElement(dialoguePanel, currentNPC.transform.position + Vector3.up * 2.2f);
        }

        dialoguePanel.SetActive(true);
        speakerNameText.text = currentLine.speakerName;

        StopTypingCoroutine();
        typingCoroutine = StartCoroutine(TypeDialogue(currentLine.dialogueText));
    }
    public void SetDialogueActive(int dialogueId, bool isActive)
    {
        if (!TryGetDialogueLine(dialogueId, out DialogueLine line))
            return;

        if (line != null)
        {
            line.isActive = isActive;
        }
    }

    private IEnumerator TypeDialogue(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            SoundManager.Instance.PlayPlayerDialogueLetter(); 
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        if (currentLine.choices.Count > 0)
        {
            ShowChoices();
        }

        typingCoroutine = null;
    }

    private void ShowChoices()
    {
        foreach (var choice in choiceObjects)
        {
            Destroy(choice);
        }
        choiceObjects.Clear();
        selectionArrow.SetActive(false);

        for (int i = 0; i < currentLine.choices.Count; i++)
        {
            GameObject choiceObj = Instantiate(choicePrefab, choicesContainer);
            TextMeshProUGUI choiceText = choiceObj.GetComponentInChildren<TextMeshProUGUI>();
            choiceText.text = currentLine.choices[i].choiceText;

            int choiceIndex = i;
            Button button = choiceObj.GetComponent<Button>();
            button.onClick.AddListener(() => SelectChoice(choiceIndex));

            choiceObjects.Add(choiceObj);
        }

        choicesPanel.SetActive(true);
        currentChoiceIndex = 0;
        HighlightChoice(currentChoiceIndex);

        if (highlightChoiceCoroutine != null)
        {
            StopCoroutine(highlightChoiceCoroutine);
            highlightChoiceCoroutine = null;
        }
        highlightChoiceCoroutine = StartCoroutine(HighlightChoiceNextFrame(currentChoiceIndex));

        PositionUIElement(choicesPanel, playerTransform.position + Vector3.up * 2.5f);

        choiceAppearTime = Time.unscaledTime;
        justDisplayedChoices = true; // <-- Ajout�

    }


    private IEnumerator HighlightChoiceNextFrame(int index)
    {
        yield return null;
        HighlightChoice(index);
        highlightChoiceCoroutine = null;
    }

    private void HighlightChoice(int index)
    {
        for (int i = 0; i < choiceObjects.Count; i++)
        {
            choiceObjects[i].GetComponent<Image>().color = normalColor;
        }

        if (selectionArrow != null && index >= 0 && index < choiceObjects.Count)
        {
            selectionArrow.SetActive(true);
            RectTransform optionRect = choiceObjects[index].GetComponent<RectTransform>();
            RectTransform arrowRect = selectionArrow.GetComponent<RectTransform>();

            arrowRect.position = new Vector3(
                optionRect.position.x - (optionRect.sizeDelta.x / 2) - (arrowRect.sizeDelta.x / 2),
                optionRect.position.y,
                optionRect.position.z
            );
        }
    }

    private void SelectChoice(int index)
    {
        choicesPanel.SetActive(false);

        DialogueChoice choice = currentLine.choices[index];
        SoundManager.Instance.PlayUIClick();

        if (choice.opensShop)
        {
            EnsureRuntimeReferences();
            if (shopManager != null)
            {
                EndDialogue();
                shopManager.OpenShop(choice.shopType);
                return;
            }
        }

        if (choice.nextDialogueId >= 0)
        {
            ShowDialogueLine(choice.nextDialogueId);
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        StopTypingCoroutine();
        if (highlightChoiceCoroutine != null)
        {
            StopCoroutine(highlightChoiceCoroutine);
            highlightChoiceCoroutine = null;
        }

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");
        dialoguePanel.SetActive(false);
        choicesPanel.SetActive(false);

        if (selectionArrow != null)
            selectionArrow.SetActive(false);

        isInDialogue = false;
        currentLine = null;
        if (playerController != null)
            playerController.SetDialogueMode(false);
        if (dualAnimPlayerController != null)
            dualAnimPlayerController.SetDialogueMode(false);

        if (pendingOnDialogueEnd != null)
        {
            var callback = pendingOnDialogueEnd;
            pendingOnDialogueEnd = null;
            callback.Invoke();
        }

    }

    private void PositionUIElement(GameObject uiElement, Vector3 worldPosition)
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        RectTransform rectTransform = uiElement.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.position = screenPos;
        }
    }

    private bool AreConditionsMet(DialogueLine line)
    {
        EnsureRuntimeReferences();
        QuestManager questManager = QuestManager.Instance != null ? QuestManager.Instance : this.questManager;
        if (questManager == null)
        {
            return false;
        }

        foreach (var condition in line.requiredQuests)
        {
            if (condition.quest == null)
            {
                return false;
            }

            switch (condition.conditionType)
            {
                case QuestCondition.QuestConditionType.Completed:
                    if (!condition.quest.isCompleted)
                    {
                        return false;
                    }
                    break;

                case QuestCondition.QuestConditionType.InProgress:
                    if (!condition.quest.isStarted || condition.quest.isCompleted)
                    {
                        return false;
                    }
                    break;

                case QuestCondition.QuestConditionType.NotStarted:
                    if (condition.quest.isStarted)
                    {
                        return false;
                    }
                    break;

                case QuestCondition.QuestConditionType.IsStarted:
                    if (!condition.quest.isStarted)
                    {
                        return false;
                    }
                    break;

                default:
                    break;
            }

            if (condition.requiredStepId != -1)
            {
                QuestStep step = null;
                for (int i = 0; i < condition.quest.steps.Count; i++)
                {
                    if (condition.quest.steps[i].stepId == condition.requiredStepId)
                    {
                        step = condition.quest.steps[i];
                        break;
                    }
                }
                if (step == null || !step.isCompleted)
                {
                    return false;
                }
            }
        }
        return true;
    }


    private DialogueData GetValidDialogueData(NPC npc)
    {
        EnsureRuntimeReferences();
        QuestManager questManager = QuestManager.Instance != null ? QuestManager.Instance : this.questManager;
        PlayerStats playerStats = this.playerStats;
        if (questManager == null) return null;
        if (playerStats == null) return null;

        foreach (var group in npc.dialogueGroups)
        {
            if (!group.isActive) continue;

            if (playerStats.baseStats.achievement < group.requiredAchievementPoints)
            {
                continue;
            }

            bool conditionsMet = true;

            foreach (var condition in group.conditions)
            {
                if (condition.quest == null)
                {
                    conditionsMet = false;
                    break;
                }

                QuestDataScriptable quest = condition.quest;

                switch (condition.conditionType)
                {
                    case QuestCondition.QuestConditionType.Completed:
                        if (!quest.isCompleted)
                        {
                            conditionsMet = false;
                        }
                        break;

                    case QuestCondition.QuestConditionType.InProgress:
                        if (!quest.isStarted || quest.isCompleted)
                        {
                            conditionsMet = false;
                        }
                        else if (condition.requiredStepId != -1)
                        {
                            QuestStep currentStep = quest.GetCurrentStep();
                            if (currentStep == null || currentStep.stepId != condition.requiredStepId)
                            {
                                conditionsMet = false;
                            }
                        }
                        break;

                    case QuestCondition.QuestConditionType.NotStarted:
                        if (quest.isStarted)
                        {
                            conditionsMet = false;
                        }
                        break;

                    case QuestCondition.QuestConditionType.IsStarted:
                        if (!quest.isStarted)
                        {
                            conditionsMet = false;
                        }
                        break;

                    default:
                        break;
                }

                if (!conditionsMet) break;
            }

            if (conditionsMet) return group.dialogueData;
        }

        return null;
    }

    public void SetDialogueGroupActive(NPC npc, int groupIndex, bool isActive)
    {
        if (npc == null || groupIndex < 0 || groupIndex >= npc.dialogueGroups.Count) return;

        npc.dialogueGroups[groupIndex].isActive = isActive;
    }

    public void StartDialogueFromData(DialogueData data, System.Action onEnd = null)
    {
        if (data == null) return;

        currentDialogue = data;
        BuildDialogueLineLookup(currentDialogue);
        currentLine = null;
        isInDialogue = true;
        InteractionManager.Instance.HideInteraction();

        if (!TryGetDialogueLine(currentDialogue.startDialogueId, out DialogueLine startLine))
        {
            Debug.LogError($"Aucune ligne de dialogue avec l'ID {currentDialogue.startDialogueId} dans {currentDialogue.name}");
            EndDialogue();
            return;
        }

        pendingOnDialogueEnd = onEnd;
        ShowDialogueLine(startLine.id);
    }

    private void EnsureRuntimeReferences()
    {
        if (shopManager == null)
            shopManager = FindAnyObjectByType<ShopManager>();
        if (questManager == null)
            questManager = FindAnyObjectByType<QuestManager>();
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
        if (dualAnimPlayerController == null)
            dualAnimPlayerController = FindAnyObjectByType<DualAnimPlayerController>();
        if (playerInput == null)
            playerInput = FindAnyObjectByType<PlayerInput>();
    }

    private void BuildDialogueLineLookup(DialogueData data)
    {
        dialogueLinesById.Clear();
        if (data == null || data.dialogueLines == null)
            return;

        for (int i = 0; i < data.dialogueLines.Count; i++)
        {
            DialogueLine line = data.dialogueLines[i];
            if (line != null)
            {
                dialogueLinesById[line.id] = line;
            }
        }
    }

    private bool TryGetDialogueLine(int dialogueId, out DialogueLine line)
    {
        if (dialogueLinesById.Count == 0 && currentDialogue != null)
        {
            BuildDialogueLineLookup(currentDialogue);
        }

        return dialogueLinesById.TryGetValue(dialogueId, out line);
    }

    private void StopTypingCoroutine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    public void OnDialogueNext(InputAction.CallbackContext context)
    {
        if (!isInDialogue || choicesPanel.activeSelf)
            return;

        if (context.performed)
        {
            if (isTyping)
            {
                StopTypingCoroutine();
                dialogueText.text = currentLine.dialogueText;
                isTyping = false;

                if (currentLine.choices.Count > 0)
                {
                    ShowChoices();
                }
            }
            else
            {
                ProgressDialogue();
            }
        }
    }


    public void OnDialogueChoice(InputAction.CallbackContext context)
    {
        if (!isInDialogue || !choicesPanel.activeSelf)
            return;

        if (Time.time - lastInputTime < inputCooldown)
            return;

        Vector2 input = context.ReadValue<Vector2>();
        if (input.y > 0.7f)
        {
            currentChoiceIndex = (currentChoiceIndex - 1 + choiceObjects.Count) % choiceObjects.Count;
            HighlightChoice(currentChoiceIndex);
            lastInputTime = Time.time;
            SoundManager.Instance.PlayUIMove();
        }
        else if (input.y < -0.7f)
        {
            currentChoiceIndex = (currentChoiceIndex + 1) % choiceObjects.Count;
            HighlightChoice(currentChoiceIndex);
            lastInputTime = Time.time;
            SoundManager.Instance.PlayUIMove();
        }
    }

    public void OnDialogueSelect(InputAction.CallbackContext context)
    {
        if (!isInDialogue || !choicesPanel.activeSelf)
            return;

        if (context.performed)
        {
            SelectChoice(currentChoiceIndex);
        }
    }


}
