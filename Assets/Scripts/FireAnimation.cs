using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FireAnimation : MonoBehaviour
{
    [SerializeField] float[] animationSpeeds;
    [SerializeField] Vector3[] fireSizes;

    [SerializeField] Image fireImage;

    [SerializeField] Sprite[] fireSprites;
    int currentIndex = 0;

    [SerializeField] int currentFireLevel;

    float timer;

    EventBinding<ComboIncreasedEvent> comboIncreasedBinding;
    EventBinding<ComboFinishedEvent> comboFinishedBinding;

    private void OnEnable()
    {
        comboIncreasedBinding = new EventBinding<ComboIncreasedEvent>(OnComboIncreased);
        comboFinishedBinding = new EventBinding<ComboFinishedEvent>(OnComboFinished);

        EventBus<ComboIncreasedEvent>.Subscribe(comboIncreasedBinding);
        EventBus<ComboFinishedEvent>.Subscribe(comboFinishedBinding);
    }

    private void OnDisable()
    {
        EventBus<ComboIncreasedEvent>.Unsubscribe(comboIncreasedBinding);
        EventBus<ComboFinishedEvent>.Unsubscribe(comboFinishedBinding);
    }

    void Update()
    {
        if (fireImage.enabled == false)
            return;

        timer += Time.deltaTime;

        if(timer >= 1f / animationSpeeds[currentFireLevel])
        {
            timer = 0;

            AnimateFire();
        }
    }

    void AnimateFire()
    {
        fireImage.sprite = fireSprites[currentIndex];

        currentIndex++;

        if (currentIndex >= fireSprites.Length)
            currentIndex = 0;
    }

    void OnComboIncreased(ComboIncreasedEvent e)
    {
        if (e.combo >= 2)
        {
            fireImage.enabled = true;
        }

        if (e.combo >= 3 && e.combo <= 4)
        {
            IncreaseFireLevel();
        }
        SetSize();
    }

    void OnComboFinished()
    {
        DisableFire();
    }

    void IncreaseFireLevel()
    {
        currentFireLevel++;
    }

    void ResetFireLevel()
    {
        currentFireLevel = 0;
    }

    void SetSize()
    {
        fireImage.transform.localScale = fireSizes[currentFireLevel];
    }

    void DisableFire()
    {
        ResetFireLevel();
        SetSize();

        fireImage.enabled = false;
    }
}
