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
            StartCoroutine(NextRingDelay());
            StartCoroutine(SetRingColor());
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
            TriggerGameOver();
        }
    }

    void OnRingReleased()
    {
        dynamicRing.isActive = false;
        CalculateScore();
    }

    void SetStaticRingSize()
    {
        float randSize = UnityEngine.Random.Range(minRingSize.x + 0.1f, maxRingSize.x - 0.1f);

        staticRing.transform.localScale = new Vector3(randSize, randSize, 1);
    }

    protected override void OnGameStarted_Hook()
    {
        dynamicRing.isActive = true;
        dynamicRing.ResetSize();
        SetStaticRingSize();
    }

    protected override void OnRestart_Hook()
    {
        dynamicRing.isActive = true;
        dynamicRing.ResetSize();
    }

    IEnumerator SetRingColor()
    {
        yield return new WaitForSeconds(delayForNextRing);
        dynamicRing.GetComponent<Image>().color = UnityEngine.Random.ColorHSV(
        0f, 1f,
        0.7f, 1f,
        0.8f, 1f,
        0.392f, 0.392f
        );
        
        staticRing.GetComponent<Image>().color = UnityEngine.Random.ColorHSV(
        0f, 1f,
        0.7f, 1f,
        0.8f, 1f,
        0.392f, 0.392f
        );
    }
}
