using UnityEngine;

[CreateAssetMenu(menuName = "Bubble Shooter/Level", fileName = "Level")]
public sealed class LevelData : ScriptableObject
{
    [SerializeField] private int columns = 11;
    [SerializeField] private LevelRow[] rows = new LevelRow[8];
    [SerializeField] private BubbleColor[] shotColors = new BubbleColor[30];

    public int Columns => columns;
    public int RowCount => rows.Length;
    public int ShotColorCount => shotColors.Length;

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
