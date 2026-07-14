using System.Collections;
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
    bool isPreloaded;

    EventBinding<ComboIncreasedEvent> comboIncreasedBinding;
    EventBinding<ComboFinishedEvent> comboFinishedBinding;
    EventBinding<ReturnedToMainMenuEvent> returnedToMainMenuBinding;

    private void OnEnable()
    {
        comboIncreasedBinding = new EventBinding<ComboIncreasedEvent>(OnComboIncreased);
        comboFinishedBinding = new EventBinding<ComboFinishedEvent>(OnComboFinished);
        returnedToMainMenuBinding = new EventBinding<ReturnedToMainMenuEvent>(DisableFire);
        EventBus<ComboIncreasedEvent>.Subscribe(comboIncreasedBinding);
        EventBus<ComboFinishedEvent>.Subscribe(comboFinishedBinding);
        EventBus<ReturnedToMainMenuEvent>.Subscribe(returnedToMainMenuBinding);
    }

    private void OnDisable()
    {
        EventBus<ComboIncreasedEvent>.Unsubscribe(comboIncreasedBinding);
        EventBus<ComboFinishedEvent>.Unsubscribe(comboFinishedBinding);
        EventBus<ReturnedToMainMenuEvent>.Unsubscribe(returnedToMainMenuBinding);
    }

    private void Start()
    {
        StartCoroutine(PreloadFireAtlas());
    }

    private IEnumerator PreloadFireAtlas()
    {
        // Görünür durumdayken alpha'yý 0 yapýyoruz ki Canvas render etsin
        // (enabled=false render etmez, texture yüklenmez)
        Color originalColor = fireImage.color;
        Color transparent = originalColor;
        transparent.a = 0f;

        fireImage.color = transparent;
        fireImage.enabled = true;

        // Her benzersiz texture/atlas sayfasýný en az bir kez göster
        // Eðer hepsi tek atlas sayfasýndaysa tek sprite yeterli, ama farklý
        // sayfalara düþmüþ olabilecekleri için birkaç tanesini örnekliyoruz
        int step = Mathf.Max(1, fireSprites.Length / 8); // ~8 örnek yeterli

        for (int i = 0; i < fireSprites.Length; i += step)
        {
            fireImage.sprite = fireSprites[i];
            yield return null; // bir frame render olsun, texture upload tetiklensin
        }

        // Eski haline döndür
        fireImage.sprite = null;
        fireImage.color = originalColor;
        fireImage.enabled = false;

        isPreloaded = true;
    }

    void Update()
    {
        if (fireImage.enabled == false)
            return;
        timer += Time.deltaTime;
        if (timer >= 1f / animationSpeeds[currentFireLevel])
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
            if (!fireImage.enabled)
            {
                fireImage.enabled = true;
                currentIndex = 0;
                timer = 0f;
                fireImage.sprite = fireSprites[currentIndex];
            }
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