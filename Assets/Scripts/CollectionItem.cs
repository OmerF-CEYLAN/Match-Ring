using UnityEngine;
using UnityEngine.UI;

public class CollectionItem : MonoBehaviour
{
    [SerializeField] Image itemImage,outerImage;
    [SerializeField] GameObject lockObj;

    int index;

    public void Initialize(int itemIndex, Sprite sprite)
    {
        index = itemIndex;
        itemImage.sprite = sprite;
        outerImage.sprite = sprite;
        Refresh();
    }

    public void SetSprite(Sprite sprite)
    {
        itemImage.sprite = sprite;
    }

    public void LockItem()
    {
        lockObj.SetActive(true);
    }

    public void UnlockItem()
    {
        lockObj.SetActive(false);
    }

    public void Refresh()
    {
        if (CollectionSaveManager.Instance.IsUnlocked(index))
        {
            UnlockItem();
            itemImage.color = CollectionSaveManager.Instance.GetColor(index);
        }
        else
        {
            LockItem();
            itemImage.color = Color.white;
        }
    }
}
