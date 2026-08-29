using System;
using System.Collections;
using UnityEngine;

public class PreGameView : MonoBehaviour
{
    [SerializeField] private float appearDuration = 0.2f;
    [SerializeField] private float settleDuration = 0.08f;
    [SerializeField] private float visibleDuration = 1f;
    [SerializeField] private float disappearDuration = 0.2f;
    [SerializeField] private float startScale = 0.75f;
    [SerializeField] private float feedbackScale = 1.08f;

    private Vector3 originalScale;

    public void Play(Action onFinished)
    {
        originalScale = transform.localScale;
        gameObject.SetActive(true);
        StopAllCoroutines();
        AudioManager.Instance?.PlaySfx(0.05f, SFX.UiAppear, 1.1f);
        AudioManager.Instance?.PlaySfx(0.05f, SFX.UiSwoosh, 1.1f);
        StartCoroutine(PlaySequence(onFinished));
    }

    private IEnumerator PlaySequence(Action onFinished)
    {
        yield return ScaleTo(startScale, feedbackScale, appearDuration);
        yield return ScaleTo(feedbackScale, 1f, settleDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);
        AudioManager.Instance?.PlaySfx(0.05f, SFX.UiSwoosh, 0.9f);
        yield return ScaleTo(1f, feedbackScale, settleDuration);
        yield return ScaleTo(feedbackScale, 0f, disappearDuration);

        transform.localScale = originalScale;
        gameObject.SetActive(false);
        onFinished?.Invoke();
    }

    private IEnumerator ScaleTo(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            transform.localScale = originalScale * to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            transform.localScale = originalScale * Mathf.LerpUnclamped(from, to, smoothProgress);
            yield return null;
        }

        transform.localScale = originalScale * to;
    }
}
