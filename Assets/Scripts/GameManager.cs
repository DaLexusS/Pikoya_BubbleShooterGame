using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private PlayerShooter playerShooter;

    [SerializeField] private float loseLineY = -2.5f;
    [SerializeField] private UnityEvent onLost = new UnityEvent();
    [SerializeField] private UnityEvent onWin = new UnityEvent();

    private bool isInitialized;
    private bool isGameFinished;

    private void Awake()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        if (isInitialized)
        {
            return;
        }

        mapLoader.Initialize();
        playerShooter.Initialize();
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || isGameFinished || playerShooter.IsShotActive)
        {
            return;
        }

        if (mapLoader.IsEmpty)
        {
            isGameFinished = true;
            onWin.Invoke();
            return;
        }

        if (playerShooter.RemainingShots <= 0 || mapLoader.HasBubbleAtOrBelow(loseLineY))
        {
            isGameFinished = true;
            onLost.Invoke();
        }
    }
}
