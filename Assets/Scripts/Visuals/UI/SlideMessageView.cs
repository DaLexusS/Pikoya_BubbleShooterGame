using System.Collections;
using UnityEngine;

public sealed class SlideMessageView : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float slideInDuration = 0.35f;
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float slideOutDuration = 0.35f;

    [Header("Position")]
    [SerializeField] private float offscreenPadding = 80f;
    [SerializeField] private bool elasticEntrance = true;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform message;
    private RectTransform canvasRect;
    private Vector2 centerPosition;
    private bool initialized;

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        message = transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        centerPosition = message.anchoredPosition;
        initialized = true;
    }

    public void Play()
    {
        Initialize();
        StopAllCoroutines();
        gameObject.SetActive(true);
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        float halfVisualWidth =
            message.rect.width * Mathf.Abs(message.localScale.x) * 0.5f;
        float rightEdge = canvasRect != null
            ? canvasRect.rect.xMax
            : Screen.width * 0.5f;
        float leftEdge = canvasRect != null
            ? canvasRect.rect.xMin
            : Screen.width * -0.5f;
        Vector2 rightPosition = new Vector2(
            rightEdge + halfVisualWidth + offscreenPadding,
            centerPosition.y);
        Vector2 leftPosition = new Vector2(
            leftEdge - halfVisualWidth - offscreenPadding,
            centerPosition.y);

        message.anchoredPosition = rightPosition;
        yield return SlideTo(centerPosition, slideInDuration, elasticEntrance);
        yield return WaitRealtimeOrScaled(visibleDuration);
        yield return SlideTo(leftPosition, slideOutDuration, false);

        message.anchoredPosition = centerPosition;
        gameObject.SetActive(false);
    }

    private IEnumerator SlideTo(
        Vector2 targetPosition,
        float duration,
        bool useElasticEasing)
    {
        Vector2 startPosition = message.anchoredPosition;

        if (duration <= 0f)
        {
            message.anchoredPosition = targetPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = useElasticEasing
                ? EaseOutElastic(progress)
                : progress * progress * (3f - 2f * progress);
            message.anchoredPosition =
                Vector2.LerpUnclamped(startPosition, targetPosition, smooth);
            yield return null;
        }

        message.anchoredPosition = targetPosition;
    }

    private static float EaseOutElastic(float progress)
    {
        if (progress <= 0f || progress >= 1f)
        {
            return progress;
        }

        const float oscillation = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * progress) *
            Mathf.Sin((progress * 10f - 0.75f) * oscillation) + 1f;
    }

    private IEnumerator WaitRealtimeOrScaled(float duration)
    {
        float elapsed = 0f;

        while (elapsed < Mathf.Max(0f, duration))
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
