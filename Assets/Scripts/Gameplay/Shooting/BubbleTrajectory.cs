using UnityEngine;

public readonly struct BubbleTrajectoryHit
{
    public BubbleTrajectoryHit(Collider2D collider, Vector2 point, Vector2 normal, float distance, bool isTop)
    {
        Collider = collider;
        Point = point;
        Normal = normal;
        Distance = distance;
        IsTop = isTop;
    }

    public Collider2D Collider { get; }
    public Vector2 Point { get; }
    public Vector2 Normal { get; }
    public float Distance { get; }
    public bool IsTop { get; }
    public bool HasHit => Collider != null || IsTop;
}

public static class BubbleTrajectory
{
    public static BubbleTrajectoryHit Cast(
        Vector2 origin,
        Vector2 direction,
        float distance,
        float radius,
        MapLoader board,
        Collider2D leftWall,
        Collider2D rightWall,
        Collider2D ignoredCollider,
        Collider2D ignoredWall)
    {
        BubbleTrajectoryHit closestHit = default;
        float closestDistance = distance;

        if (direction.y > 0f)
        {
            float topDistance = (board.TopY - origin.y) / direction.y;

            if (topDistance >= 0f && topDistance <= closestDistance)
            {
                Vector2 topPoint = origin + direction * topDistance;
                closestHit = new BubbleTrajectoryHit(null, topPoint, Vector2.down, topDistance, true);
                closestDistance = topDistance;
            }
        }

        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, direction, distance);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == ignoredCollider || hit.collider == ignoredWall)
            {
                continue;
            }

            bool isWall = hit.collider == leftWall || hit.collider == rightWall;
            bool isBoardBubble = board.TryGetCell(hit.collider, out _);

            if ((!isWall && !isBoardBubble) || hit.distance > closestDistance)
            {
                continue;
            }

            closestHit = new BubbleTrajectoryHit(hit.collider, hit.centroid, hit.normal, hit.distance, false);
            closestDistance = hit.distance;
        }

        return closestHit;
    }
}
