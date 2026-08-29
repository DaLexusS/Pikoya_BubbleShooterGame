using System;
using System.Collections;
using UnityEngine;

public class StarRewardEffect : MonoBehaviour
{
    [SerializeField] private float revealDuration = 0.22f;
    [SerializeField] private float revealHoldDuration = 0.5f;
    [SerializeField] private float holdCurveWidth = 28f;
    [SerializeField] private float holdCurveHeight = 18f;
    [SerializeField] private float holdScalePulse = 0.04f;
    [SerializeField] private float flightDuration = 0.55f;
    [SerializeField] private float arcHeight = 70f;
    [SerializeField] private float revealScale = 2f;
    [SerializeField] private float rotationDegrees = 540f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    public void Play(Vector2 startPosition, Vector2 targetPosition, Action onArrived)
    {
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        StartCoroutine(Animate(startPosition, targetPosition, onArrived));
    }

    public void PlayDirect(
        Vector2 startPosition,
        Vector2 targetPosition,
        float duration,
        float startScale,
        Action onArrived)
    {
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.one * startScale;
        rectTransform.localRotation = Quaternion.identity;
        StartCoroutine(AnimateDirect(
            startPosition,
            targetPosition,
            Mathf.Max(0.01f, duration),
            Mathf.Max(0.01f, startScale),
            onArrived));
    }

    private IEnumerator Animate(Vector2 startPosition, Vector2 targetPosition, Action onArrived)
    {
        float revealTime = Mathf.Max(0.01f, revealDuration);
        float elapsed = 0f;

        while (elapsed < revealTime)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / revealTime);
            float smoothTime = time * time * (3f - 2f * time);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(0f, revealScale, smoothTime);
            yield return null;
        }

        if (revealHoldDuration > 0f)
        {
            float holdTime = revealHoldDuration;
            elapsed = 0f;

            while (elapsed < holdTime)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / holdTime);
                float horizontalCurve = Mathf.Sin(time * Mathf.PI * 2f) * holdCurveWidth;
                float verticalCurve = Mathf.Sin(time * Mathf.PI) * holdCurveHeight;
                float scalePulse = 1f + Mathf.Sin(time * Mathf.PI) * holdScalePulse;
                rectTransform.anchoredPosition =
                    startPosition + new Vector2(horizontalCurve, verticalCurve);
                rectTransform.localScale = Vector3.one * (revealScale * scalePulse);
                yield return null;
            }

            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one * revealScale;
        }

        float duration = Mathf.Max(0.01f, flightDuration);
        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            Vector2 position = Vector2.Lerp(startPosition, targetPosition, smoothTime);
            position.y += Mathf.Sin(time * Mathf.PI) * arcHeight;
            rectTransform.anchoredPosition = position;
            float scale = Mathf.Lerp(revealScale, 1f, smoothTime);
            rectTransform.localScale = Vector3.one * scale;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, -rotationDegrees * smoothTime);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        onArrived?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator AnimateDirect(
        Vector2 startPosition,
        Vector2 targetPosition,
        float duration,
        float startScale,
        Action onArrived)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothTime);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, smoothTime);
            rectTransform.localRotation = Quaternion.identity;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        onArrived?.Invoke();
        Destroy(gameObject);
    }
}
