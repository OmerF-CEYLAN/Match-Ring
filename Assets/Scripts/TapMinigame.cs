using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TapMinigame : MonoBehaviour, IMiniGameMode
{
    [Header("Hold Mini Game Settings")]
    [SerializeField] Vector3 staticItemSize;

    [SerializeField]
    GameObject staticItem;

    [SerializeField]
    RingMover dynamicItem;

    EventBinding<ItemClickedEvent> itemClickedBinding;
    EventBinding<PerfectMatchEvent> perfectMatchBinding;

    [SerializeField] MonoBehaviour symbolRepositorySource;
    ISymbolReader symbolReader;

    [SerializeField] Transform spawnRight, spawnLeft, spawnTop, spawnBottom;

    [SerializeField] float initialSpeed, speed, speedUpRate, toleranceRadius,punchScaleAmount;
    [SerializeField] float toleranceFraction = 0.1f;

    private void OnEnable()
    {
        itemClickedBinding = new EventBinding<ItemClickedEvent>(OnItemClicked);
        perfectMatchBinding = new EventBinding<PerfectMatchEvent>(HandlePerfectMatch);

        EventBus<ItemClickedEvent>.Subscribe(itemClickedBinding);
        EventBus<PerfectMatchEvent>.Subscribe(perfectMatchBinding);

        if (dynamicItem != null)
            dynamicItem.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        EventBus<ItemClickedEvent>.Unsubscribe(itemClickedBinding);
        EventBus<PerfectMatchEvent>.Unsubscribe(perfectMatchBinding);

        if (dynamicItem != null)
            dynamicItem.gameObject.SetActive(false);
    }

    private void Start()
    {
        symbolReader = symbolRepositorySource as ISymbolReader;
    }

    void CalculateScore()
    {
        float difference = Vector3.Distance(staticItem.transform.position, dynamicItem.transform.position);
        float differenceRate = difference / toleranceRadius * 100f;
        SendResult(differenceRate);
    }

    void HandlePerfectMatch()
    {
        ShrinkOnPerfectMatch();

        if (TutorialManager.Instance.IsActive)
            return;

        bool isItemUnlocked = CollectionSaveManager.Instance.IsUnlocked(symbolReader.GetLastSelectedIndex());

        if (!isItemUnlocked)
        {
            Color color = dynamicItem.GetComponent<Image>().color;

            CollectionSaveManager.Instance.Unlock(
                symbolReader.GetLastSelectedIndex(),
                color);
        }
    }

    void ShrinkOnPerfectMatch()
    {
        HideStaticItem(true);

        dynamicItem.SetSize(staticItem.transform.localScale);
        dynamicItem.SetPosition(staticItem.transform.position);

        RectTransform rect = dynamicItem.GetComponent<RectTransform>();
        rect.DOPunchScale(Vector3.one * punchScaleAmount, 0.3f);
    }

    void HideStaticItem(bool hide)
    {
        Image staticItemImage = staticItem.GetComponent<Image>();

        Color color1 = staticItemImage.color;
        color1.a = hide ? 0 : 1;

        staticItemImage.color = color1;
    }

    void OnItemClicked()
    {
        EndRound();
    }

    void SendResult(float differenceRate)
    {
        EventBus<MiniGameRoundResultEvent>.Publish(new MiniGameRoundResultEvent { differenceRate = differenceRate });
    }

    void SetItemSizes()
    {
        staticItem.transform.localScale = staticItemSize;
        dynamicItem.SetSize(staticItemSize);
    }

    public void BeginRound()
    {
        HideStaticItem(false);
        dynamicItem.isActive = true;
        dynamicItem.ResetMovement();
        dynamicItem.SetSpeed(initialSpeed + DifficultyManager.Instance.DifficultyLevel * speedUpRate);
        SetRandomSprite();
        SetItemSizes();
        SetRingColor();

        Transform a, b;
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            a = spawnLeft;
            b = spawnRight;
        }
        else
        {
            a = spawnTop;
            b = spawnBottom;
        }

        Vector3 pointA, pointB;
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            pointA = a.position;
            pointB = b.position;
        }
        else
        {
            pointA = b.position;
            pointB = a.position;
        }

        staticItem.transform.position = Vector3.Lerp(pointA, pointB, 0.5f);
        dynamicItem.StartMoving(pointA, pointB);

        toleranceRadius = Vector3.Distance(pointA, pointB) * toleranceFraction;
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

        Sprite selectedSprite = symbolReader.GetRandomSprite();

        dynamicRingImage.sprite = selectedSprite;
        staticRingImage.sprite = dynamicRingImage.sprite;
    }
}