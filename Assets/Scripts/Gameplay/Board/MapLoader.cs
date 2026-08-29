using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    [SerializeField] private float fallCollectionY = -4.5f;
    [SerializeField] private UnityEvent onFallingBubbleCollected = new UnityEvent();

    private readonly Dictionary<Vector2Int, BubbleView> bubbles = new();
    private readonly Dictionary<Collider2D, Vector2Int> colliderCells = new();
    private bool isInitialized;
    private int activeRemovalEffects;

    public LevelData Level => level;
    public float TopY => transform.position.y;
    public int BubbleCount => bubbles.Count;
    public bool IsEmpty => bubbles.Count == 0 && activeRemovalEffects == 0;
    public event Action<int, bool> BubbleCountChanged;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
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

        NotifyBubbleCountChanged();
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

    public bool HasBubbleAtOrBelow(float worldY)
    {
        foreach (BubbleView bubble in bubbles.Values)
        {
            if (bubble.transform.position.y <= worldY)
            {
                return true;
            }
        }

        return false;
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
        ResolveShot(cell, impactPosition);
        NotifyBubbleCountChanged();
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

    private void ResolveShot(Vector2Int shotCell, Vector2 impactPosition)
    {
        BubbleMatchResult result = BubbleMatchResolver.Resolve(this, shotCell, minimumMatchSize);

        if (result.MatchedCells.Count == 0)
        {
            PlayAttachEffect(shotCell, impactPosition);
            return;
        }

        List<BubbleView> poppingBubbles = new List<BubbleView>();

        foreach (Vector2Int cell in result.MatchedCells)
        {
            if (TryRemoveBubble(cell, out BubbleView bubble))
            {
                poppingBubbles.Add(bubble);
            }
        }

        List<List<BubbleFall>> fallingGroups = CreateFallingGroups(result);
        activeRemovalEffects += poppingBubbles.Count;

        foreach (List<BubbleFall> fallingGroup in fallingGroups)
        {
            activeRemovalEffects += fallingGroup.Count;
        }

        StartCoroutine(PlayPopSequence(poppingBubbles, fallingGroups));
    }

    private IEnumerator PlayPopSequence(List<BubbleView> poppingBubbles, List<List<BubbleFall>> fallingGroups)
    {
        for (int index = 0; index < poppingBubbles.Count; index++)
        {
            BubbleView bubble = poppingBubbles[index];
            BubblePopEffect popEffect = bubble.GetComponent<BubblePopEffect>();
            float delay = popEffect == null ? 0f : popEffect.DelayAfterPop;
            StartCoroutine(PlayBubblePop(bubble, popEffect, fallingGroups[index]));

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private IEnumerator PlayBubblePop(
        BubbleView bubble,
        BubblePopEffect popEffect,
        List<BubbleFall> fallingBubbles)
    {
        if (popEffect != null)
        {
            yield return popEffect.Play();
        }

        Destroy(bubble.gameObject);
        activeRemovalEffects = Mathf.Max(0, activeRemovalEffects - 1);
        NotifyBubbleCountChanged();

        foreach (BubbleFall fallingBubble in fallingBubbles)
        {
            fallingBubble.Play(fallCollectionY, HandleFallingBubbleCollected);
        }
    }

    private void HandleFallingBubbleCollected()
    {
        activeRemovalEffects = Mathf.Max(0, activeRemovalEffects - 1);
        NotifyBubbleCountChanged();
        onFallingBubbleCollected.Invoke();
    }

    private List<List<BubbleFall>> CreateFallingGroups(BubbleMatchResult result)
    {
        List<List<BubbleFall>> fallingGroups = new List<List<BubbleFall>>();

        for (int index = 0; index < result.MatchedCells.Count; index++)
        {
            fallingGroups.Add(new List<BubbleFall>());
        }

        Dictionary<Vector2Int, int> matchedOrder = new Dictionary<Vector2Int, int>();

        for (int index = 0; index < result.MatchedCells.Count; index++)
        {
            matchedOrder[result.MatchedCells[index]] = index;
        }

        HashSet<Vector2Int> detachedCells = new HashSet<Vector2Int>(result.DetachedCells);
        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();

        foreach (Vector2Int startCell in result.DetachedCells)
        {
            if (!visitedCells.Add(startCell))
            {
                continue;
            }

            Queue<Vector2Int> openCells = new Queue<Vector2Int>();
            List<Vector2Int> componentCells = new List<Vector2Int>();
            int releaseIndex = 0;
            openCells.Enqueue(startCell);

            while (openCells.Count > 0)
            {
                Vector2Int cell = openCells.Dequeue();
                componentCells.Add(cell);

                foreach (Vector2Int neighbour in GetNeighbours(cell))
                {
                    if (detachedCells.Contains(neighbour))
                    {
                        if (visitedCells.Add(neighbour))
                        {
                            openCells.Enqueue(neighbour);
                        }

                        continue;
                    }

                    if (matchedOrder.TryGetValue(neighbour, out int matchedIndex))
                    {
                        releaseIndex = Mathf.Max(releaseIndex, matchedIndex);
                    }
                }
            }

            foreach (Vector2Int cell in componentCells)
            {
                if (TryRemoveBubble(cell, out BubbleView bubble))
                {
                    fallingGroups[releaseIndex].Add(bubble.GetComponent<BubbleFall>());
                }
            }
        }

        return fallingGroups;
    }

    private void PlayAttachEffect(Vector2Int shotCell, Vector2 impactPosition)
    {
        if (!TryGetBubble(shotCell, out BubbleView attachedBubble))
        {
            return;
        }

        BubbleAttachEffect attachEffect = attachedBubble.GetComponent<BubbleAttachEffect>();

        if (attachEffect == null)
        {
            return;
        }

        List<BubbleShockTarget> targets = FindShockTargets(shotCell, attachEffect.ShockRings);
        attachEffect.Play(impactPosition, targets);
    }

    private List<BubbleShockTarget> FindShockTargets(Vector2Int originCell, int maximumRing)
    {
        List<BubbleShockTarget> targets = new List<BubbleShockTarget>();
        Queue<Vector2Int> openCells = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> rings = new Dictionary<Vector2Int, int>();
        openCells.Enqueue(originCell);
        rings[originCell] = 0;

        while (openCells.Count > 0)
        {
            Vector2Int cell = openCells.Dequeue();
            int currentRing = rings[cell];

            if (currentRing >= maximumRing)
            {
                continue;
            }

            foreach (Vector2Int neighbourCell in GetNeighbours(cell))
            {
                if (rings.ContainsKey(neighbourCell) || !TryGetBubble(neighbourCell, out BubbleView neighbourBubble))
                {
                    continue;
                }

                int ring = currentRing + 1;
                rings[neighbourCell] = ring;
                targets.Add(new BubbleShockTarget(neighbourBubble, ring));
                openCells.Enqueue(neighbourCell);
            }
        }

        return targets;
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
            bubbleCollider.enabled = false;
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

    private void NotifyBubbleCountChanged()
    {
        BubbleCountChanged?.Invoke(BubbleCount, IsEmpty);
    }
}
