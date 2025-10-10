using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string saveFilePath => Path.Combine(Application.persistentDataPath, "saveData.json");
    public static void SaveGame(SaveData saveData)
    {
        saveData.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string json = JsonUtility.ToJson(saveData, true);

        try
        {
            File.WriteAllText(saveFilePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erreur lors de la sauvegarde du jeu : " + ex.Message);
        }
    }
    public static SaveData LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);

                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("Jeu chargé depuis le fichier : " + saveFilePath);
                return saveData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Erreur lors du chargement du jeu : " + ex.Message);
                return null;
            }
        }
        else
        {
            Debug.LogWarning("Aucun fichier de sauvegarde trouvé à l'emplacement : " + saveFilePath);
            return null;
        }
    }
}
