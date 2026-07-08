using UnityEngine;

public interface ISymbolReader
{
    public Sprite GetRandomSprite();
    public Sprite[] GetAllSprites();

    public int GetLastSelectedIndex();
}

public class SymbolRepository : MonoBehaviour,ISymbolReader
{
    [SerializeField] Sprite[] sprites;

    int lastSelectedIndex = -1;

    public Sprite GetRandomSprite()
    {
        int selectedIndex = UnityEngine.Random.Range(0, sprites.Length);

        if (sprites.Length > 1)
        {
            while (selectedIndex == lastSelectedIndex)
            {
                selectedIndex = UnityEngine.Random.Range(0, sprites.Length);
            }
        }

        lastSelectedIndex = selectedIndex;

        return sprites[selectedIndex];
    }

    public Sprite[] GetAllSprites()
    {
        return sprites;
    }

    public int GetLastSelectedIndex()
    {
        return lastSelectedIndex;
    }
}
