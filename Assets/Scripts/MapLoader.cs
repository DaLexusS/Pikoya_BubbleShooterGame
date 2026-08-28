using System.Collections.Generic;
using UnityEngine;

public sealed class MapLoader : MonoBehaviour
{
    private static readonly Vector2Int[] EvenRowNeighbours =
    {
        new(-1, 0), new(1, 0), new(-1, -1), new(0, -1), new(-1, 1), new(0, 1)
    };

    private static readonly Vector2Int[] OddRowNeighbours =
    {
        new(-1, 0), new(1, 0), new(0, -1), new(1, -1), new(0, 1), new(1, 1)
    };

    [SerializeField] private LevelData level;
    [SerializeField] private BubbleView bubblePrefab;
    [SerializeField] private float horizontalSpacing = 0.5f;
    [SerializeField] private float verticalSpacing = 0.44f;
    [SerializeField] private float bubbleScale = 0.7f;
    [SerializeField] private int maximumRows = 16;
    [SerializeField] private int minimumMatchSize = 3;

    private readonly Dictionary<Vector2Int, BubbleView> bubbles = new();
    private readonly Dictionary<Collider2D, Vector2Int> colliderCells = new();

    public LevelData Level => level;
    public float TopY => transform.position.y;

    private void Awake()
    {
        LoadLevel();
    }

    public void LoadLevel()
    {
        bubbles.Clear();
        colliderCells.Clear();

        for (int row = 0; row < level.RowCount; row++)
        {
            for (int column = 0; column < level.Columns; column++)
            {
                BubbleColor bubbleColor = level.GetCell(row, column);

                if (bubbleColor == BubbleColor.Empty)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(column, row);

                if (IsValidCell(cell))
                {
                    SpawnBubble(cell, bubbleColor);
                }
            }
        }
    }

    public bool TryGetCell(Collider2D bubbleCollider, out Vector2Int cell)
    {
        if (bubbleCollider == null)
        {
            cell = default;
            return false;
        }

        return colliderCells.TryGetValue(bubbleCollider, out cell);
    }

    public bool TryGetBubble(Vector2Int cell, out BubbleView bubble)
    {
        return bubbles.TryGetValue(cell, out bubble);
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        return new List<Vector2Int>(bubbles.Keys);
    }

    public IEnumerable<Vector2Int> GetNeighbours(Vector2Int cell)
    {
        Vector2Int[] offsets = cell.y % 2 == 0 ? EvenRowNeighbours : OddRowNeighbours;

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int neighbour = cell + offset;

            if (IsValidCell(neighbour))
            {
                yield return neighbour;
            }
        }
    }

    public bool AttachBubble(BubbleView bubble, Vector2 impactPosition, Vector2Int? contactCell)
    {
        if (!TryFindSnapCell(impactPosition, contactCell, out Vector2Int cell))
        {
            return false;
        }

        bubble.transform.SetParent(transform);
        bubble.transform.localPosition = GetCellPosition(cell.y, cell.x);
        bubble.transform.localRotation = Quaternion.identity;
        bubble.transform.localScale = bubblePrefab.transform.localScale * bubbleScale;
        bubble.name = $"Bubble {cell.y + 1}-{cell.x + 1}";
        RegisterBubble(cell, bubble);
        ResolveShot(cell);
        return true;
    }

    private void SpawnBubble(Vector2Int cell, BubbleColor bubbleColor)
    {
        BubbleView bubble = Instantiate(bubblePrefab, transform);
        bubble.name = $"Bubble {cell.y + 1}-{cell.x + 1}";
        bubble.transform.localPosition = GetCellPosition(cell.y, cell.x);
        bubble.transform.localScale *= bubbleScale;
        bubble.SetColor(bubbleColor);
        RegisterBubble(cell, bubble);
    }

    private void RegisterBubble(Vector2Int cell, BubbleView bubble)
    {
        bubbles[cell] = bubble;
        CircleCollider2D bubbleCollider = bubble.GetComponent<CircleCollider2D>();
        colliderCells[bubbleCollider] = cell;
    }

    private void ResolveShot(Vector2Int shotCell)
    {
        BubbleMatchResult result = BubbleMatchResolver.Resolve(this, shotCell, minimumMatchSize);

        if (result.MatchedCells.Count == 0)
        {
            return;
        }

        foreach (Vector2Int cell in result.MatchedCells)
        {
            if (TryRemoveBubble(cell, out BubbleView bubble))
            {
                Destroy(bubble.gameObject);
            }
        }

        List<BubbleFall> fallingBubbles = new List<BubbleFall>();

        foreach (Vector2Int cell in result.DetachedCells)
        {
            if (TryRemoveBubble(cell, out BubbleView bubble))
            {
                fallingBubbles.Add(bubble.GetComponent<BubbleFall>());
            }
        }

        foreach (BubbleFall fallingBubble in fallingBubbles)
        {
            fallingBubble.Play();
        }
    }

    private bool TryRemoveBubble(Vector2Int cell, out BubbleView bubble)
    {
        if (!bubbles.TryGetValue(cell, out bubble))
        {
            return false;
        }

        bubbles.Remove(cell);
        Collider2D bubbleCollider = bubble.GetComponent<Collider2D>();

        if (bubbleCollider != null)
        {
            colliderCells.Remove(bubbleCollider);
        }

        return true;
    }

    private bool TryFindSnapCell(Vector2 impactPosition, Vector2Int? contactCell, out Vector2Int bestCell)
    {
        bool found = false;
        bestCell = default;
        float bestDistance = float.MaxValue;

        if (contactCell.HasValue)
        {
            Vector2Int[] neighbours = contactCell.Value.y % 2 == 0 ? EvenRowNeighbours : OddRowNeighbours;

            foreach (Vector2Int offset in neighbours)
            {
                ConsiderCell(contactCell.Value + offset, impactPosition, ref found, ref bestCell, ref bestDistance);
            }
        }
        else
        {
            for (int column = 0; column < level.Columns; column++)
            {
                ConsiderCell(new Vector2Int(column, 0), impactPosition, ref found, ref bestCell, ref bestDistance);
            }
        }

        if (found)
        {
            return true;
        }

        for (int row = 0; row < maximumRows; row++)
        {
            for (int column = 0; column < level.Columns; column++)
            {
                ConsiderCell(new Vector2Int(column, row), impactPosition, ref found, ref bestCell, ref bestDistance);
            }
        }

        return found;
    }

    private void ConsiderCell(
        Vector2Int cell,
        Vector2 impactPosition,
        ref bool found,
        ref Vector2Int bestCell,
        ref float bestDistance)
    {
        if (!IsValidCell(cell) || bubbles.ContainsKey(cell))
        {
            return;
        }

        Vector2 cellPosition = transform.TransformPoint(GetCellPosition(cell.y, cell.x));
        float distance = (cellPosition - impactPosition).sqrMagnitude;

        if (distance >= bestDistance)
        {
            return;
        }

        found = true;
        bestCell = cell;
        bestDistance = distance;
    }

    private bool IsValidCell(Vector2Int cell)
    {
        if (cell.y < 0 || cell.y >= maximumRows || cell.x < 0 || cell.x >= level.Columns)
        {
            return false;
        }

        return cell.y % 2 == 0 || cell.x < level.Columns - 1;
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
