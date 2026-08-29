using System;
using UnityEngine;

[Serializable]
public sealed class LevelRow
{
    [SerializeField] private BubbleColor[] cells = new BubbleColor[11];

    public BubbleColor GetCell(int column)
    {
        if (column < 0 || column >= cells.Length)
        {
            return BubbleColor.Empty;
        }

        return cells[column];
    }
}
