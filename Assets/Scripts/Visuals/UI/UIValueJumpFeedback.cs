using System.Collections;
using UnityEngine;

public sealed class UIValueJumpFeedback : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField, Min(0f)] private float jumpHeight = 8f;
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    [SerializeField, Min(1f)] private float peakScale = 1.08f;

    private Coroutine animation;
    private Vector2 restingPosition;
    private Vector3 restingScale;

    private void Awake()
    {
        target ??= transform as RectTransform;
        CacheRestingTransform();
    }

    public void Play()
    {
        if (target == null)
        {
            return;
        }

        if (animation != null)
        {
            StopCoroutine(animation);
            RestoreTransform();
        }
        else
        {
            CacheRestingTransform();
        }

        animation = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float animationDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            float jump = Mathf.Sin(progress * Mathf.PI);
            target.anchoredPosition = restingPosition + Vector2.up * (jumpHeight * jump);
            target.localScale = restingScale * Mathf.Lerp(1f, peakScale, jump);
            yield return null;
        }

        RestoreTransform();
        animation = null;
    }

    private void CacheRestingTransform()
    {
        if (target == null)
        {
            return;
        }

        restingPosition = target.anchoredPosition;
        restingScale = target.localScale;
    }

    private void RestoreTransform()
    {
        if (target == null)
        {
            return;
        }

        target.anchoredPosition = restingPosition;
        target.localScale = restingScale;
    }

    private void OnDisable()
    {
        if (animation != null)
        {
            StopCoroutine(animation);
            animation = null;
        }

        RestoreTransform();
    }
}
