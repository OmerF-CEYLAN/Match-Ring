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

    [SerializeField] Sprite[] sprites;

    int lastSelectedIndex = -1;

    [SerializeField] Transform spawnRight, spawnLeft, spawnTop, spawnBottom;

    [SerializeField] float initialSpeed, speed, speedUpRate, toleranceRadius;
    [SerializeField] float toleranceFraction = 0.1f;

    private void OnEnable()
    {
        itemClickedBinding = new EventBinding<ItemClickedEvent>(OnItemClicked);
        perfectMatchBinding = new EventBinding<PerfectMatchEvent>(ShrinkOnPerfectMatch);

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

    void CalculateScore()
    {
        float difference = Vector3.Distance(staticItem.transform.position, dynamicItem.transform.position);
        float differenceRate = difference / toleranceRadius * 100f;
        SendResult(differenceRate);
    }

    void ShrinkOnPerfectMatch()
    {
        dynamicItem.SetSize(staticItem.transform.localScale);
        dynamicItem.SetPosition(staticItem.transform.position);
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