using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private ScoreBarView scoreBarView;

    private LevelData level;
    private int totalBubbleCount;
    private int scoredBubbleCount;
    private bool isInitialized;

    public static ScoreManager Active { get; private set; }
    public int Score { get; private set; }
    public ScoreBarView ScoreBarView => scoreBarView;

    public void Initialize(LevelData levelData)
    {
        if (isInitialized)
        {
            return;
        }

        level = levelData;
        totalBubbleCount = Mathf.Max(1, level.StartingBubbleCount + level.ShotColorCount);
        scoredBubbleCount = 0;
        Score = 0;
        Active = this;
        scoreBarView.Initialize(level);
        isInitialized = true;
    }

    public int AddBubble()
    {
        if (!isInitialized)
        {
            return 0;
        }

        scoredBubbleCount = Mathf.Min(scoredBubbleCount + 1, totalBubbleCount);
        int newScore = Mathf.RoundToInt(level.MaxScore * (scoredBubbleCount / (float)totalBubbleCount));
        int awardedScore = Mathf.Max(0, newScore - Score);
        Score = newScore;
        scoreBarView.SetScore(Score);
        return awardedScore;
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }
}
