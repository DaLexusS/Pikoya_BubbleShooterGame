using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    [SerializeField] private PreGameView preGameView;
    [SerializeField] private VictoryView victoryView;
    [SerializeField] private float preGameDelay = 0.5f;

    [SerializeField] private float loseLineY = -2.5f;
    [SerializeField] private UnityEvent onLost = new UnityEvent();
    [SerializeField] private UnityEvent onWin = new UnityEvent();

    private bool isInitialized;
    private bool isGameFinished;

    private void Awake()
    {
        StartCoroutine(InitializeWhenSceneIsReady());
    }

    private IEnumerator InitializeWhenSceneIsReady()
    {
        while (!SceneTransitionManager.IsSceneReady)
        {
            yield return null;
        }

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
        victoryView?.Initialize();

        if (victoryView != null)
        {
            victoryView.ExitRequested += ExitToMenu;
            victoryView.NextRequested += GoToNextLevel;
        }

        playerShooter.DisableShooting();
        isInitialized = true;
        StartCoroutine(ShowPreGameAfterDelay());
    }

    private IEnumerator ShowPreGameAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, preGameDelay));

        if (isGameFinished)
        {
            yield break;
        }

        if (preGameView == null)
        {
            playerShooter.EnableShooting();
            yield break;
        }

        preGameView.Play(playerShooter.EnableShooting);
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
            winCelebration.Play(
                remainingBubbles,
                playerShooter.ConsumeCelebrationBubble,
                ShowVictory);
            onWin.Invoke();
            return;
        }

        if (playerShooter.RemainingShots <= 0 || mapLoader.HasBubbleAtOrBelow(loseLineY))
        {
            isGameFinished = true;
            onLost.Invoke();
        }
    }

    private void ShowVictory()
    {
        int earnedStarCount = 0;

        for (int index = 0; index < 3; index++)
        {
            if (scoreManager.Score >= mapLoader.Level.GetStarScore(index))
            {
                earnedStarCount++;
            }
        }

        victoryView?.Play(scoreManager.Score, earnedStarCount);
    }

    private static void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("Menu");
    }

    private static void GoToNextLevel()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        int nextIndex = scene.buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneTransitionManager.LoadScene(nextIndex);
            return;
        }

        int digitStart = scene.name.Length;
        while (digitStart > 0 && char.IsDigit(scene.name[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart < scene.name.Length &&
            int.TryParse(scene.name.Substring(digitStart), out int number))
        {
            string nextName = scene.name.Substring(0, digitStart) + (number + 1);
            if (Application.CanStreamedLevelBeLoaded(nextName))
            {
                SceneTransitionManager.LoadScene(nextName);
                return;
            }
        }

        Debug.LogWarning("No next level is included in Build Settings yet.");
    }

    private void OnDestroy()
    {
        if (victoryView != null)
        {
            victoryView.ExitRequested -= ExitToMenu;
            victoryView.NextRequested -= GoToNextLevel;
        }
    }

}
