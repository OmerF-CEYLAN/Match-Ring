using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [SerializeField] int difficultyIncreaseInterval;
    int successfullMatchCount;
    public int DifficultyLevel { get; private set; }

    EventBinding<SuccessfulMatchEvent> successfullMatchBinding;
    EventBinding<GameOverEvent> gameOverBinding;
    EventBinding<ReturnedToMainMenuEvent> returnedToMainMenuBinding;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        successfullMatchBinding = new EventBinding<SuccessfulMatchEvent>(OnSuccessfullMatch);
        gameOverBinding = new EventBinding<GameOverEvent>(OnGameOver);
        returnedToMainMenuBinding = new EventBinding<ReturnedToMainMenuEvent>(OnGameOver);

        EventBus<SuccessfulMatchEvent>.Subscribe(successfullMatchBinding);
        EventBus<GameOverEvent>.Subscribe(gameOverBinding);
        EventBus<ReturnedToMainMenuEvent>.Subscribe(returnedToMainMenuBinding);
    }

    private void OnDisable()
    {
        EventBus<SuccessfulMatchEvent>.Unsubscribe(successfullMatchBinding);
        EventBus<GameOverEvent>.Unsubscribe(gameOverBinding);
        EventBus<ReturnedToMainMenuEvent>.Unsubscribe(returnedToMainMenuBinding);
    }

    void OnSuccessfullMatch()
    {
        successfullMatchCount++;

        if (successfullMatchCount >= difficultyIncreaseInterval)
        {
            successfullMatchCount = 0;
            DifficultyLevel++;
            EventBus<IncreaseDifficultyEvent>.Publish(new IncreaseDifficultyEvent());
        }
    }

    void OnGameOver()
    {
        successfullMatchCount = 0;
        DifficultyLevel = 0;
    }
}