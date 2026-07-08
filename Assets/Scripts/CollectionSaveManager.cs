using System;
using UnityEngine;

public class CollectionSaveManager : MonoBehaviour
{
    public static CollectionSaveManager Instance { get; private set; }

    const string SaveKey = "CollectionSave";

    [Serializable]
    class SaveData
    {
        public bool[] unlocked;
    }

    SaveData saveData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(int itemCount)
    {
        if (saveData.unlocked == null || saveData.unlocked.Length != itemCount)
        {
            saveData.unlocked = new bool[itemCount];
            Save();
        }
    }

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= saveData.unlocked.Length)
            return false;

        return saveData.unlocked[index];
    }

    public void Unlock(int index)
    {
        if (index < 0 || index >= saveData.unlocked.Length)
            return;

        if (saveData.unlocked[index])
            return;

        saveData.unlocked[index] = true;
        Save();
    }

    void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
            saveData = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
        else
            saveData = new SaveData();
    }
}