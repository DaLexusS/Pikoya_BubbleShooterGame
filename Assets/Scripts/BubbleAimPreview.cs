using UnityEngine;

public class BubbleAimPreview : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int maximumWallReflections = 2;
    [SerializeField] private float maximumDistance = 20f;

    private void Awake()
    {
        Hide();
    }

    public void Show(
        Vector2 origin,
        Vector2 direction,
        float radius,
        MapLoader board,
        Collider2D leftWall,
        Collider2D rightWall,
        Collider2D ignoredCollider)
    {
        Vector3[] points = new Vector3[maximumWallReflections + 2];
        int pointCount = 1;
        points[0] = origin;
        Collider2D ignoredWall = null;

        for (int reflection = 0; reflection <= maximumWallReflections; reflection++)
        {
            BubbleTrajectoryHit hit = BubbleTrajectory.Cast(
                origin,
                direction,
                maximumDistance,
                radius,
                board,
                leftWall,
                rightWall,
                ignoredCollider,
                ignoredWall);

            Vector2 endPoint = hit.HasHit ? hit.Point : origin + direction * maximumDistance;
            points[pointCount] = endPoint;
            pointCount++;

            if (!hit.HasHit || hit.IsTop || board.TryGetCell(hit.Collider, out _))
            {
                break;
            }

            if (reflection == maximumWallReflections)
            {
                break;
            }

            ignoredWall = hit.Collider;
            direction = Vector2.Reflect(direction, hit.Normal).normalized;
            origin = endPoint + direction * 0.02f;
        }

        lineRenderer.positionCount = pointCount;

        for (int index = 0; index < pointCount; index++)
        {
            lineRenderer.SetPosition(index, points[index]);
        }

        lineRenderer.enabled = true;
    }

    public void Hide()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }
}
