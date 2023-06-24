using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class SaveFile
{
    [Serializable]
    public class SaveData
    {
        public string[] Progress;
        public string Version;
        public string LastPlayedLevel;
    }
    private static readonly string saveFilePath = Application.persistentDataPath + "/progress.txt";

    public static void SaveProgress(List<string> progress, string lastPlayedLevel)
    {
        SaveData data = new SaveData();
        data.Progress = progress.ToArray();
        data.Version = "v" + Application.version;
        data.LastPlayedLevel = lastPlayedLevel;
        string encodedProgress = JsonUtility.ToJson(data);
        try
        {
            File.WriteAllText(saveFilePath, encodedProgress);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveProgress: error occured while saving: {e}");
        }
    }
    public static void LoadProgress(out List<string> progress, out string lastPlayedLevel)
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string encodedProgress = File.ReadAllText(saveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(encodedProgress);
                progress = new List<string>(data.Progress);
                lastPlayedLevel = data.LastPlayedLevel;
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"LoadProgress: error occured while loading progress: {e}");
            }
        }
        progress = new List<string>();
        lastPlayedLevel = string.Empty;
        return;
    }
}
