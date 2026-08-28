using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private MapLoader board;
    [SerializeField] private BubbleView bubblePrefab;
    [SerializeField] private Transform currentBallPoint;
    [SerializeField] private Transform nextBallPoint;
    [SerializeField] private Collider2D leftWall;
    [SerializeField] private Collider2D rightWall;
    [SerializeField] private int startingShots = 30;
    [SerializeField] private float shotSpeed = 12f;
    [SerializeField] private float bubbleScale = 0.7f;
    [SerializeField] private float swapClickRadius = 0.35f;

    private BubbleView currentBubble;
    private BubbleView nextBubble;
    private Vector2 aimDirection = Vector2.up;
    private int nextColorIndex;
    private bool isAiming;
    private bool isShotActive;

    public int RemainingShots { get; private set; }

    private void Start()
    {
        RemainingShots = Mathf.Min(startingShots, board.Level.ShotColorCount);
        currentBubble = CreateBubble(currentBallPoint, board.Level.GetShotColor(0));
        nextBubble = CreateBubble(nextBallPoint, board.Level.GetShotColor(1));
        nextColorIndex = 2;
    }

    private void Update()
    {
        if (isShotActive || currentBubble == null || Pointer.current == null)
        {
            return;
        }

        Vector2 pointerWorldPosition = GetPointerWorldPosition();

        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (IsSwapClick(pointerWorldPosition))
            {
                SwapBalls();
                isAiming = false;
                return;
            }

            isAiming = TrySetAimDirection(pointerWorldPosition);
        }

        if (isAiming && Pointer.current.press.isPressed)
        {
            TrySetAimDirection(pointerWorldPosition);
        }

        if (isAiming && Pointer.current.press.wasReleasedThisFrame)
        {
            TrySetAimDirection(pointerWorldPosition);
            Shoot();
            isAiming = false;
        }
    }

    private Vector2 GetPointerWorldPosition()
    {
        Vector3 screenPosition = Pointer.current.position.ReadValue();
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPosition);
        return worldPosition;
    }

    private bool TrySetAimDirection(Vector2 targetPosition)
    {
        Vector2 newDirection = targetPosition - (Vector2)currentBallPoint.position;

        if (newDirection.y <= 0.05f)
        {
            return false;
        }

        aimDirection = newDirection.normalized;
        return true;
    }

    private bool IsSwapClick(Vector2 pointerPosition)
    {
        return nextBubble != null &&
               Vector2.Distance(pointerPosition, currentBallPoint.position) <= swapClickRadius;
    }

    private void SwapBalls()
    {
        (currentBubble, nextBubble) = (nextBubble, currentBubble);
        MoveBubbleToPoint(currentBubble, currentBallPoint);
        MoveBubbleToPoint(nextBubble, nextBallPoint);
    }

    private void Shoot()
    {
        if (RemainingShots <= 0)
        {
            return;
        }

        BubbleView firedBubble = currentBubble;
        currentBubble = null;
        RemainingShots--;
        isShotActive = true;
        firedBubble.transform.SetParent(null, true);
        firedBubble.GetComponent<BubbleShot>().Launch(
            aimDirection,
            shotSpeed,
            board,
            leftWall,
            rightWall,
            HandleShotFinished);
    }

    private void HandleShotFinished()
    {
        isShotActive = false;
        currentBubble = nextBubble;
        nextBubble = null;

        if (currentBubble != null)
        {
            MoveBubbleToPoint(currentBubble, currentBallPoint);
        }

        if (RemainingShots > 1)
        {
            BubbleColor nextColor = board.Level.GetShotColor(nextColorIndex);
            nextColorIndex++;
            nextBubble = CreateBubble(nextBallPoint, nextColor);
        }
    }

    private BubbleView CreateBubble(Transform parentPoint, BubbleColor bubbleColor)
    {
        if (bubbleColor == BubbleColor.Empty)
        {
            return null;
        }

        BubbleView bubble = Instantiate(bubblePrefab, parentPoint);
        bubble.name = parentPoint == currentBallPoint ? "Current Ball" : "Next Ball";
        bubble.transform.localPosition = Vector3.zero;
        bubble.transform.localRotation = Quaternion.identity;
        bubble.transform.localScale *= bubbleScale;
        bubble.SetColor(bubbleColor);
        return bubble;
    }

    private static void MoveBubbleToPoint(BubbleView bubble, Transform point)
    {
        bubble.transform.SetParent(point);
        bubble.transform.localPosition = Vector3.zero;
        bubble.transform.localRotation = Quaternion.identity;
        bubble.name = point.name == "CurrentBallPoint" ? "Current Ball" : "Next Ball";
    }

    private void OnDrawGizmos()
    {
        if (currentBallPoint == null || board == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentBallPoint.position, swapClickRadius);

        if (nextBallPoint != null)
        {
            Gizmos.DrawWireSphere(nextBallPoint.position, swapClickRadius * 0.75f);
        }

        DrawTrajectoryGizmo();
    }

    private void DrawTrajectoryGizmo()
    {
        Vector2 origin = currentBallPoint.position;
        Vector2 direction = aimDirection.sqrMagnitude > 0f ? aimDirection.normalized : Vector2.up;
        float radius = GetBubbleRadius();
        Collider2D ignoredCollider = currentBubble == null ? null : currentBubble.GetComponent<Collider2D>();
        Collider2D ignoredWall = null;

        for (int bounce = 0; bounce <= 2; bounce++)
        {
            BubbleTrajectoryHit hit = BubbleTrajectory.Cast(
                origin,
                direction,
                20f,
                radius,
                board,
                leftWall,
                rightWall,
                ignoredCollider,
                ignoredWall);

            Vector2 endPoint = hit.HasHit ? hit.Point : origin + direction * 20f;
            Gizmos.DrawLine(origin, endPoint);

            if (!hit.HasHit || hit.IsTop || board.TryGetCell(hit.Collider, out _))
            {
                Gizmos.DrawWireSphere(endPoint, radius);
                return;
            }

            ignoredWall = hit.Collider;
            direction = Vector2.Reflect(direction, hit.Normal).normalized;
            origin = endPoint + direction * 0.02f;
        }
    }

    private float GetBubbleRadius()
    {
        if (currentBubble != null)
        {
            return currentBubble.GetComponent<CircleCollider2D>().bounds.extents.x;
        }

        CircleCollider2D prefabCollider = bubblePrefab == null ? null : bubblePrefab.GetComponent<CircleCollider2D>();
        return prefabCollider == null ? 0.23f : prefabCollider.radius * bubblePrefab.transform.localScale.x * bubbleScale;
    }
}
