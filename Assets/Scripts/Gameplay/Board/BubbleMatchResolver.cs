using System.Collections.Generic;

public class BubbleMatchResult
{
    public BubbleMatchResult(List<UnityEngine.Vector2Int> matchedCells, List<UnityEngine.Vector2Int> detachedCells)
    {
        MatchedCells = matchedCells;
        DetachedCells = detachedCells;
    }

    public IReadOnlyList<UnityEngine.Vector2Int> MatchedCells { get; }
    public IReadOnlyList<UnityEngine.Vector2Int> DetachedCells { get; }
}

public static class BubbleMatchResolver
{
    public static BubbleMatchResult Resolve(MapLoader board, UnityEngine.Vector2Int shotCell, int minimumMatchSize)
    {
        List<UnityEngine.Vector2Int> matchedCells = FindMatchingCells(board, shotCell);

        if (matchedCells.Count < minimumMatchSize)
        {
            return new BubbleMatchResult(new List<UnityEngine.Vector2Int>(), new List<UnityEngine.Vector2Int>());
        }

        HashSet<UnityEngine.Vector2Int> removedCells = new HashSet<UnityEngine.Vector2Int>(matchedCells);
        HashSet<UnityEngine.Vector2Int> supportedCells = FindSupportedCells(board, removedCells);
        List<UnityEngine.Vector2Int> detachedCells = new List<UnityEngine.Vector2Int>();

        foreach (UnityEngine.Vector2Int cell in board.GetOccupiedCells())
        {
            if (!removedCells.Contains(cell) && !supportedCells.Contains(cell))
            {
                detachedCells.Add(cell);
            }
        }

        return new BubbleMatchResult(matchedCells, detachedCells);
    }

    private static List<UnityEngine.Vector2Int> FindMatchingCells(MapLoader board, UnityEngine.Vector2Int startCell)
    {
        List<UnityEngine.Vector2Int> matches = new List<UnityEngine.Vector2Int>();

        if (!board.TryGetBubble(startCell, out BubbleView startBubble))
        {
            return matches;
        }

        BubbleColor targetColor = startBubble.BubbleColor;
        Queue<UnityEngine.Vector2Int> openCells = new Queue<UnityEngine.Vector2Int>();
        HashSet<UnityEngine.Vector2Int> visitedCells = new HashSet<UnityEngine.Vector2Int>();
        openCells.Enqueue(startCell);
        visitedCells.Add(startCell);

        while (openCells.Count > 0)
        {
            UnityEngine.Vector2Int cell = openCells.Dequeue();

            if (!board.TryGetBubble(cell, out BubbleView bubble) || bubble.BubbleColor != targetColor)
            {
                continue;
            }

            matches.Add(cell);

            foreach (UnityEngine.Vector2Int neighbour in board.GetNeighbours(cell))
            {
                if (visitedCells.Add(neighbour))
                {
                    openCells.Enqueue(neighbour);
                }
            }
        }

        return matches;
    }

    private static HashSet<UnityEngine.Vector2Int> FindSupportedCells(
        MapLoader board,
        HashSet<UnityEngine.Vector2Int> removedCells)
    {
        HashSet<UnityEngine.Vector2Int> supportedCells = new HashSet<UnityEngine.Vector2Int>();
        Queue<UnityEngine.Vector2Int> openCells = new Queue<UnityEngine.Vector2Int>();

        foreach (UnityEngine.Vector2Int cell in board.GetOccupiedCells())
        {
            if (cell.y == 0 && !removedCells.Contains(cell))
            {
                supportedCells.Add(cell);
                openCells.Enqueue(cell);
            }
        }

        while (openCells.Count > 0)
        {
            UnityEngine.Vector2Int cell = openCells.Dequeue();

            foreach (UnityEngine.Vector2Int neighbour in board.GetNeighbours(cell))
            {
                if (removedCells.Contains(neighbour) || !board.TryGetBubble(neighbour, out _))
                {
                    continue;
                }

                if (supportedCells.Add(neighbour))
                {
                    openCells.Enqueue(neighbour);
                }
            }
        }

        return supportedCells;
    }
}
