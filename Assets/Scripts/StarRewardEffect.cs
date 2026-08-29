using System;
using System.Collections;
using UnityEngine;

public class StarRewardEffect : MonoBehaviour
{
    [SerializeField] private float revealDuration = 0.22f;
    [SerializeField] private float revealHoldDuration = 0.08f;
    [SerializeField] private float flightDuration = 0.55f;
    [SerializeField] private float arcHeight = 70f;
    [SerializeField] private float revealScale = 1.5f;
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
            yield return new WaitForSeconds(revealHoldDuration);
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
}
