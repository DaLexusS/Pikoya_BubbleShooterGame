using UnityEngine;

public sealed class MapLoader : MonoBehaviour
{
    [SerializeField] private LevelData level;
    [SerializeField] private BubbleView bubblePrefab;
    [SerializeField] private float horizontalSpacing = 0.5f;
    [SerializeField] private float verticalSpacing = 0.44f;
    [SerializeField] private float bubbleScale = 0.7f;

    private void Start()
    {
        LoadLevel();
    }

    public void LoadLevel()
    {
        for (int row = 0; row < level.RowCount; row++)
        {
            for (int column = 0; column < level.Columns; column++)
            {
                BubbleColor bubbleColor = level.GetCell(row, column);

                if (bubbleColor == BubbleColor.Empty)
                {
                    continue;
                }

                SpawnBubble(row, column, bubbleColor);
            }
        }
    }

    private void SpawnBubble(int row, int column, BubbleColor bubbleColor)
    {
        BubbleView bubble = Instantiate(bubblePrefab, transform);
        bubble.name = $"Bubble {row + 1}-{column + 1}";
        bubble.transform.localPosition = GetCellPosition(row, column);
        bubble.transform.localScale *= bubbleScale;
        bubble.SetColor(bubbleColor);
    }

    private Vector3 GetCellPosition(int row, int column)
    {
        float centeredColumn = column - (level.Columns - 1) * 0.5f;
        float rowOffset = row % 2 == 0 ? 0f : horizontalSpacing * 0.5f;
        float x = centeredColumn * horizontalSpacing + rowOffset;
        float y = -row * verticalSpacing;

        return new Vector3(x, y, 0f);
    }
}
