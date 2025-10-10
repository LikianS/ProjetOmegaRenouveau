#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DialogueDataEditor : EditorWindow
{
    private DialogueData dialogueData;
    private Vector2 scrollPosition;
    private bool showChoices = false;
    private int newDialogueId = 0;

    [MenuItem("Tools/Dialogue Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogueDataEditor>("Dialogue Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Dialogue Data Editor", EditorStyles.boldLabel);

        dialogueData = (DialogueData)EditorGUILayout.ObjectField("Dialogue Data", dialogueData, typeof(DialogueData), false);

        if (dialogueData == null)
        {
            if (GUILayout.Button("Create New Dialogue Data"))
            {
                CreateNewDialogueData();
            }
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space();

        dialogueData.startDialogueId = EditorGUILayout.IntField("Start Dialogue ID", dialogueData.startDialogueId);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Add New Dialogue Line", EditorStyles.boldLabel);

        newDialogueId = EditorGUILayout.IntField("ID", newDialogueId);

        if (GUILayout.Button("Add Dialogue Line"))
        {
            bool idExists = dialogueData.dialogueLines.Exists(line => line.id == newDialogueId);

            if (!idExists)
            {
                DialogueLine newLine = new DialogueLine
                {
                    id = newDialogueId,
                    speakerName = "Speaker",
                    dialogueText = "New dialogue text",
                    isPlayerSpeaking = false,
                    nextDialogueId = -1
                };

                dialogueData.dialogueLines.Add(newLine);
                newDialogueId++;

                EditorUtility.SetDirty(dialogueData);
            }
            else
            {
                EditorUtility.DisplayDialog("Duplicate ID", "A dialogue line with this ID already exists!", "OK");
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Dialogue Lines", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        List<DialogueLine> linesToRemove = new List<DialogueLine>();

        for (int i = 0; i < dialogueData.dialogueLines.Count; i++)
        {
            DialogueLine line = dialogueData.dialogueLines[i];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID: " + line.id, EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                linesToRemove.Add(line);
                continue;
            }
            EditorGUILayout.EndHorizontal();

            line.speakerName = EditorGUILayout.TextField("Speaker Name", line.speakerName);
            line.isPlayerSpeaking = EditorGUILayout.Toggle("Is Player Speaking", line.isPlayerSpeaking);
            line.dialogueText = EditorGUILayout.TextArea(line.dialogueText, GUILayout.Height(60));

            line.opensShop = EditorGUILayout.Toggle("Opens Shop", line.opensShop);
            if (line.opensShop)
            {
                line.shopType = EditorGUILayout.TextField("Shop Type", line.shopType);
                line.nextDialogueId = -1;
            }
            else
            {
                line.nextDialogueId = EditorGUILayout.IntField("Next Dialogue ID", line.nextDialogueId);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Disable Dialogue Groups", EditorStyles.boldLabel);
            for (int j = 0; j < line.disableDialogueGroups.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();
                line.disableDialogueGroups[j] = EditorGUILayout.IntField($"Group {j + 1}", line.disableDialogueGroups[j]);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    line.disableDialogueGroups.RemoveAt(j);
                    j--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Group to Disable"))
            {
                line.disableDialogueGroups.Add(-1); 
            }

            EditorGUILayout.Space();

            showChoices = EditorGUILayout.Foldout(showChoices, "Choices");

            if (showChoices)
            {
                List<DialogueChoice> choicesToRemove = new List<DialogueChoice>();

                for (int j = 0; j < line.choices.Count; j++)
                {
                    DialogueChoice choice = line.choices[j];

                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Choice " + (j + 1), EditorStyles.boldLabel);

                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        choicesToRemove.Add(choice);
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();

                    choice.choiceText = EditorGUILayout.TextField("Choice Text", choice.choiceText);

                    choice.opensShop = EditorGUILayout.Toggle("Opens Shop", choice.opensShop);
                    if (choice.opensShop)
                    {
                        choice.shopType = EditorGUILayout.TextField("Shop Type", choice.shopType);
                        choice.nextDialogueId = -1;
                    }
                    else
                    {
                        choice.nextDialogueId = EditorGUILayout.IntField("Next Dialogue ID", choice.nextDialogueId);
                    }

                    EditorGUILayout.EndVertical();
                }

                foreach (var choice in choicesToRemove)
                {
                    line.choices.Remove(choice);
                    EditorUtility.SetDirty(dialogueData);
                }

                if (GUILayout.Button("Add Choice"))
                {
                    DialogueChoice newChoice = new DialogueChoice
                    {
                        choiceText = "New choice",
                        nextDialogueId = -1
                    };

                    line.choices.Add(newChoice);
                    EditorUtility.SetDirty(dialogueData);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        foreach (var line in linesToRemove)
        {
            dialogueData.dialogueLines.Remove(line);
            EditorUtility.SetDirty(dialogueData);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void CreateNewDialogueData()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Data", "NewDialogue", "asset", "Create a new Dialogue Data asset");

        if (string.IsNullOrEmpty(path))
            return;

        DialogueData newData = ScriptableObject.CreateInstance<DialogueData>();
        newData.startDialogueId = 0;
        newData.dialogueLines = new List<DialogueLine>();

        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();

        dialogueData = newData;
    }
}
#endif
