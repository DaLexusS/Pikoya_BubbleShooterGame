using UnityEngine;

[CreateAssetMenu(menuName = "Bubble Shooter/Level", fileName = "Level")]
public sealed class LevelData : ScriptableObject
{
    [SerializeField] private int columns = 11;
    [SerializeField] private LevelRow[] rows = new LevelRow[8];
    [SerializeField] private BubbleColor[] shotColors = new BubbleColor[30];
    [SerializeField] private int maxScore = 10000;
    [SerializeField] private int firstStarScore = 3000;
    [SerializeField] private int secondStarScore = 6500;
    [SerializeField] private int thirdStarScore = 9000;

    public int Columns => columns;
    public int RowCount => rows.Length;
    public int ShotColorCount => shotColors.Length;
    public int MaxScore => Mathf.Max(1, maxScore);

    public int StartingBubbleCount
    {
        get
        {
            int bubbleCount = 0;

            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (GetCell(row, column) != BubbleColor.Empty)
                    {
                        bubbleCount++;
                    }
                }
            }

            return bubbleCount;
        }
    }

    public int GetStarScore(int index)
    {
        int score = index switch
        {
            0 => firstStarScore,
            1 => secondStarScore,
            2 => thirdStarScore,
            _ => MaxScore
        };

        return Mathf.Clamp(score, 0, MaxScore);
    }

    public BubbleColor GetCell(int row, int column)
    {
        if (row < 0 || row >= rows.Length || rows[row] == null)
        {
            return BubbleColor.Empty;
        }

        return rows[row].GetCell(column);
    }

    public BubbleColor GetShotColor(int index)
    {
        if (index < 0 || index >= shotColors.Length)
        {
            return BubbleColor.Empty;
        }

        return shotColors[index];
    }
}
