using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HoldMiniGame : MonoBehaviour, IMiniGameMode
{
    [Header("Hold Mini Game Settings")]
    [SerializeField] Vector3 minRingSize, maxRingSize;

    [SerializeField]
    GameObject staticRing;

    [SerializeField]
    RingVisualController dynamicRing;

    EventBinding<RingReleasedEvent> ringReleasedBinding;
    EventBinding<PerfectMatchEvent> perfectMatchBinding;


    [SerializeField] Sprite[] sprites;

    int lastSelectedIndex = -1;

    private void OnEnable()
    {
        ringReleasedBinding = new EventBinding<RingReleasedEvent>(OnRingReleased);
        perfectMatchBinding = new EventBinding<PerfectMatchEvent>(ShrinkOnPerfectMatch);

        EventBus<RingReleasedEvent>.Subscribe(ringReleasedBinding);
        EventBus<PerfectMatchEvent>.Subscribe(perfectMatchBinding);
    }

    private void OnDisable()
    {
        EventBus<RingReleasedEvent>.Unsubscribe(ringReleasedBinding);
        EventBus<PerfectMatchEvent>.Unsubscribe(perfectMatchBinding);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        dynamicRing.minSize = minRingSize;
        dynamicRing.maxSize = maxRingSize;
    }

    void CalculateScore()
    {
        float difference = Math.Abs(staticRing.transform.localScale.x - dynamicRing.transform.localScale.x);

        float differenceRate = difference / staticRing.transform.localScale.x * 100f;

        SendResult(differenceRate);
    }

    void ShrinkOnPerfectMatch()
    {
        dynamicRing.SetSize(staticRing.transform.localScale);
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

        staticRing.transform.localScale = new Vector3(randSize, randSize, 1);
    }

    public void BeginRound()
    {
        dynamicRing.isActive = true;
        dynamicRing.ResetSize();
        SetRandomSprite();
        SetStaticRingSize();
        SetRingColor();
    }

    public void EndRound()
    {
        dynamicRing.isActive = false;
        CalculateScore();
    }

    void SetRingColor()
    {
        Image dynamicImage = dynamicRing.GetComponent<Image>();

        Color color1 = UnityEngine.Random.ColorHSV(
            0f, 1f,
            0.7f, 1f,
            0.8f, 1f
        );

        dynamicImage.color = color1;
    }

    void SetRandomSprite()
    {
        Image dynamicRingImage = dynamicRing.GetComponent<Image>();
        Image staticRingImage = staticRing.GetComponent<Image>();

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
