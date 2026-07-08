using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    [SerializeField] GameObject contentObj;

    [SerializeField] MonoBehaviour symbolReaderSource;
    ISymbolReader symbolReader;

    [SerializeField] GameObject collectionItemPrefab;

    List<CollectionItem> items = new();

    void Start()
    {
        symbolReader = symbolReaderSource as ISymbolReader;

        SetUpAllItemSprites();
    }

    void SetUpAllItemSprites()
    {
        Sprite[] sprites = symbolReader.GetAllSprites();

        CollectionSaveManager.Instance.Initialize(sprites.Length);

        for (int i = 0; i < sprites.Length; i++)
        {
            GameObject obj = Instantiate(collectionItemPrefab, contentObj.transform);

            CollectionItem item = obj.GetComponent<CollectionItem>();

            item.Initialize(i, sprites[i]);

            items.Add(item);
        }
    }

    public void RefreshAll()
    {
        foreach (var item in items)
            item.Refresh();
    }
}
