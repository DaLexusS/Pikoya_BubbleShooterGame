using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ScoreWorldPool scoreWorldPool;
    [SerializeField] private FireworkPool fireworkPool;
    [SerializeField] private WinCelebrationView winCelebration;
    [SerializeField] private PlayerBubbleCountView playerBubbleCountView;
    [SerializeField] private BoardBubbleCountView boardBubbleCountView;

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

        scoreWorldPool.Initialize();
        fireworkPool.Initialize();
        mapLoader.Initialize();
        boardBubbleCountView ??= BoardBubbleCountView.FindInScene();
        boardBubbleCountView.Initialize(mapLoader);
        playerShooter.Initialize();
        playerBubbleCountView ??= PlayerBubbleCountView.FindInScene();
        playerBubbleCountView.Initialize(playerShooter);
        scoreManager.Initialize(mapLoader.Level);
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
            playerShooter.DisableShooting();
            List<BubbleView> remainingBubbles = playerShooter.ReleaseRemainingBubbles(winCelebration.transform);
            winCelebration.Play(remainingBubbles, playerShooter.ConsumeCelebrationBubble);
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
