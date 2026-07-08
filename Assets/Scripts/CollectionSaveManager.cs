using System;
using UnityEngine;

public class CollectionSaveManager : MonoBehaviour
{
    public static CollectionSaveManager Instance { get; private set; }

    const string SaveKey = "CollectionSave";

    [Serializable]
    public class CollectionItemData
    {
        public bool unlocked;
        public Color color;
    }

    [Serializable]
    class SaveData
    {
        public CollectionItemData[] items;
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
        if (saveData.items == null || saveData.items.Length != itemCount)
        {
            saveData.items = new CollectionItemData[itemCount];

            for (int i = 0; i < itemCount; i++)
            {
                saveData.items[i] = new CollectionItemData
                {
                    unlocked = false,
                    color = Color.white
                };
            }

            Save();
        }
    }

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= saveData.items.Length)
            return false;

        return saveData.items[index].unlocked;
    }

    public void Unlock(int index, Color color)
    {
        if (index < 0 || index >= saveData.items.Length)
            return;

        if (saveData.items[index].unlocked)
            return;

        saveData.items[index].unlocked = true;
        saveData.items[index].color = color;

        Save();
    }
    public Color GetColor(int index)
    {
        if (index < 0 || index >= saveData.items.Length)
            return Color.white;

        return saveData.items[index].color;
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