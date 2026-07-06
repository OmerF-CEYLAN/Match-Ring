using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HoldMiniGame : MonoBehaviour, IMiniGameMode
{
    [Header("Hold Mini Game Settings")]
    [SerializeField] Vector3 minRingSize, maxRingSize;

    [SerializeField]
    GameObject staticItem;

    [SerializeField]
    RingVisualController dynamicItem;

    EventBinding<RingReleasedEvent> ringReleasedBinding;
    EventBinding<PerfectMatchEvent> perfectMatchBinding;

    [SerializeField] Sprite[] sprites;

    int lastSelectedIndex = -1;

    [SerializeField] float initialSpeed, speed,speedUpRate;


    private void OnEnable()
    {
        ringReleasedBinding = new EventBinding<RingReleasedEvent>(OnRingReleased);
        perfectMatchBinding = new EventBinding<PerfectMatchEvent>(ShrinkOnPerfectMatch);

        EventBus<RingReleasedEvent>.Subscribe(ringReleasedBinding);
        EventBus<PerfectMatchEvent>.Subscribe(perfectMatchBinding);

        if (dynamicItem != null)
            dynamicItem.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        EventBus<RingReleasedEvent>.Unsubscribe(ringReleasedBinding);
        EventBus<PerfectMatchEvent>.Unsubscribe(perfectMatchBinding);

        if (dynamicItem != null)
            dynamicItem.gameObject.SetActive(false);
    }

    private void Start()
    {
        dynamicItem.minSize = minRingSize;
        dynamicItem.maxSize = maxRingSize;
    }

    void CalculateScore()
    {
        float difference = Math.Abs(staticItem.transform.localScale.x - dynamicItem.transform.localScale.x);

        float differenceRate = difference / staticItem.transform.localScale.x * 100f;

        SendResult(differenceRate);
    }

    void ShrinkOnPerfectMatch()
    {
        HideStaticItem(true);
        dynamicItem.SetSize(staticItem.transform.localScale);
    }

    void HideStaticItem(bool hide)
    {
        Image staticItemImage = staticItem.GetComponent<Image>();

        Color color1 = staticItemImage.color;
        color1.a = hide ? 0 : 1;

        staticItemImage.color = color1;
    }

    void OnRingReleased()
    {
        EndRound();
    }

    void SendResult(float differenceRate)
    {
        EventBus<MiniGameRoundResultEvent>.Publish(new MiniGameRoundResultEvent { differenceRate = differenceRate });
    }

    void SetStaticRingSize()
    {
        float randSize = UnityEngine.Random.Range(minRingSize.x + 0.5f, maxRingSize.x - 0.1f);

        staticItem.transform.localScale = new Vector3(randSize, randSize, 1);
    }

    public void BeginRound()
    {
        HideStaticItem(false);
        dynamicItem.isActive = true;
        dynamicItem.ResetSize();
        dynamicItem.SetSpeed(initialSpeed + DifficultyManager.Instance.DifficultyLevel * speedUpRate);
        SetRandomSprite();
        SetStaticRingSize();
        SetRingColor();
    }

    public void EndRound()
    {
        dynamicItem.isActive = false;
        CalculateScore();
    }


    void SetRingColor()
    {
        Image dynamicImage = dynamicItem.GetComponent<Image>();

        Color color1 = UnityEngine.Random.ColorHSV(
            0f, 1f,
            0.7f, 1f,
            0.8f, 1f
        );

        dynamicImage.color = color1;
    }

    void SetRandomSprite()
    {
        Image dynamicRingImage = dynamicItem.GetComponent<Image>();
        Image staticRingImage = staticItem.GetComponent<Image>();

        int selectedIndex = UnityEngine.Random.Range(0, sprites.Length);

        if (sprites.Length > 1)
        {
            while (selectedIndex == lastSelectedIndex)
            {
                selectedIndex = UnityEngine.Random.Range(0, sprites.Length);
            }
        }

        lastSelectedIndex = selectedIndex;

        dynamicRingImage.sprite = sprites[selectedIndex];
        staticRingImage.sprite = dynamicRingImage.sprite;
    }
}
