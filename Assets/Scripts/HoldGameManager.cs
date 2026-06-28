using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HoldGameManager : GameManager
{
    [Header("Hold Game Settings")]
    [SerializeField] private float delayForNextRing = 1.5f;
    [SerializeField] Vector3 minRingSize, maxRingSize;

    [SerializeField]
    GameObject staticRing;

    [SerializeField]
    RingVisualController dynamicRing;

    EventBinding<RingReleasedEvent> ringReleasedBinding;

    [SerializeField] Sprite[] sprites;

    int lastSelectedIndex = -1;

    private void OnEnable()
    {
        ringReleasedBinding = new EventBinding<RingReleasedEvent>(OnRingReleased);
        EventBus<RingReleasedEvent>.Subscribe(ringReleasedBinding);
    }

    private void OnDisable()
    {
        EventBus<RingReleasedEvent>.Unsubscribe(ringReleasedBinding);
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        dynamicRing.minSize = minRingSize;
        dynamicRing.maxSize = maxRingSize;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

        if (!IsPlaying) return;
    }

    IEnumerator NextRingDelay()
    {
        yield return new WaitForSeconds(delayForNextRing);

        SetStaticRingSize();

        dynamicRing.isActive = true;

        dynamicRing.ResetSize();

        SetRandomSprite();

        SetRingColor();
    }

    void CalculateScore()
    {
        float difference = Math.Abs(staticRing.transform.localScale.x - dynamicRing.transform.localScale.x);

        float differenceRate = difference / staticRing.transform.localScale.x * 100f;

        Debug.Log(staticRing.transform.localScale.x + " " + dynamicRing.transform.localScale.x + " " + differenceRate);

        if (differenceRate <= 10f)
        {
            AddScore(3);
            EventBus<ShakeEvent>.Publish(new ShakeEvent());
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 3});
            EventBus<PerfectMatchSFXEvent>.Publish(new PerfectMatchSFXEvent());
            ShrinkOnPerfectMatch();
            StartCoroutine(NextRingDelay());
        }
        else if(differenceRate <= 20f)
        {
            AddScore(1);
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 1 });
            StartCoroutine(NextRingDelay());
        }
        else
        {
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 0 });
            StartCoroutine(TriggerGameOverDelayed());
        }
    }

    void ShrinkOnPerfectMatch()
    {
        dynamicRing.SetSize(staticRing.transform.localScale);
    }

    void OnRingReleased()
    {
        dynamicRing.isActive = false;
        CalculateScore();
    }

    void SetStaticRingSize()
    {
        float randSize = UnityEngine.Random.Range(minRingSize.x + 0.5f, maxRingSize.x - 0.1f);

        staticRing.transform.localScale = new Vector3(randSize, randSize, 1);
    }

    protected override void OnGameStarted_Hook()
    {
        dynamicRing.isActive = true;
        dynamicRing.ResetSize();
        SetRandomSprite();
        SetStaticRingSize();
        SetRingColor();
    }

    protected override void OnRestart_Hook()
    {
        dynamicRing.isActive = true;
        dynamicRing.ResetSize();
        SetRandomSprite();
        SetRingColor();
    }

    IEnumerator TriggerGameOverDelayed()
    {
        yield return new WaitForSeconds(delayForNextRing);
        TriggerGameOver();
    }

    void SetRingColor()
    {
        Image dynamicImage = dynamicRing.GetComponent<Image>();
        Image staticImage = staticRing.GetComponent<Image>();

        Color color1 = UnityEngine.Random.ColorHSV(
            0f, 1f,
            0.7f, 1f,
            0.8f, 1f,
            0.392f, 0.392f
        );

        Color color2;

        do
        {
            color2 = UnityEngine.Random.ColorHSV(
                0f, 1f,
                0.7f, 1f,
                0.8f, 1f,
                0.392f, 0.392f
            );

            Color.RGBToHSV(color1, out float h1, out _, out _);
            Color.RGBToHSV(color2, out float h2, out _, out _);

            float hueDifference = Mathf.Abs(h1 - h2);
            hueDifference = Mathf.Min(hueDifference, 1f - hueDifference);

            if (hueDifference >= 0.1f)
                break;

        } while (true);

        dynamicImage.color = color1;
        staticImage.color = color2;
    }

    void SetRandomSprite()
    {
        Image dynamicRingImage = dynamicRing.GetComponent<Image>();
        Image staticRingImage = staticRing.GetComponent<Image>();

        int selectedIndex = UnityEngine.Random.Range(0, sprites.Length);

        if(sprites.Length > 1)
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
