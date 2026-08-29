using System;
using UnityEngine;

public class BubbleShot : MonoBehaviour
{
    private const int MaximumCollisionsPerFrame = 4;
    private const float SurfaceOffset = 0.02f;

    private BubbleView bubbleView;
    private CircleCollider2D bubbleCollider;
    private MapLoader board;
    private Collider2D leftWall;
    private Collider2D rightWall;
    private Action shotFinished;
    private Vector2 direction;
    private float speed;

    public void Launch(
        Vector2 shotDirection,
        float shotSpeed,
        MapLoader bubbleBoard,
        Collider2D leftWallCollider,
        Collider2D rightWallCollider,
        Action onShotFinished)
    {
        bubbleView = GetComponent<BubbleView>();
        bubbleView.MarkAsPlayerShot();
        bubbleCollider = GetComponent<CircleCollider2D>();
        board = bubbleBoard;
        leftWall = leftWallCollider;
        rightWall = rightWallCollider;
        shotFinished = onShotFinished;
        direction = shotDirection.normalized;
        speed = shotSpeed;
        enabled = true;
    }

    private void Update()
    {
        Travel(speed * Time.deltaTime);
    }

    private void Travel(float distance)
    {
        Collider2D ignoredWall = null;
        for (int collision = 0; collision < MaximumCollisionsPerFrame && distance > 0f; collision++)
        {
            BubbleTrajectoryHit hit = BubbleTrajectory.Cast(
                transform.position,
                direction,
                distance,
                bubbleCollider.bounds.extents.x,
                board,
                leftWall,
                rightWall,
                bubbleCollider,
                ignoredWall);

            if (!hit.HasHit)
            {
                transform.position += (Vector3)(direction * distance);
                return;
            }

            transform.position = hit.Point;
            distance -= hit.Distance;

            if (hit.IsTop)
            {
                FinishShot(null);
                return;
            }

            if (board.TryGetCell(hit.Collider, out Vector2Int contactCell))
            {
                FinishShot(contactCell);
                return;
            }

            ignoredWall = hit.Collider;
            direction = Vector2.Reflect(direction, hit.Normal).normalized;
            transform.position += (Vector3)(direction * SurfaceOffset);
            distance = Mathf.Max(0f, distance - SurfaceOffset);
        }
    }

    private void FinishShot(Vector2Int? contactCell)
    {
        enabled = false;
        bool attached = board.AttachBubble(bubbleView, transform.position, contactCell);

        if (!attached)
        {
            Destroy(gameObject);
        }

        Action callback = shotFinished;
        shotFinished = null;
        callback?.Invoke();
    }
}
