using System.Collections;
using System;
using TMPro;
using UnityEngine;

public class ScoreWorldEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float floatDuration = 0.5f;
    [SerializeField] private float floatDistance = 0.5f;
    [SerializeField] private float verticalOffset = 0.2f;

    private Vector3 originalScale;
    private Color originalColor;
    private Action<ScoreWorldEffect> finished;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalColor = scoreText.color;
    }

    public void Play(int scoreValue, Action<ScoreWorldEffect> onFinished)
    {
        StopAllCoroutines();
        finished = onFinished;
        scoreText.text = scoreValue.ToString();
        scoreText.color = originalColor;
        transform.localScale = originalScale;
        transform.position += Vector3.up * verticalOffset;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float duration = Mathf.Max(0.01f, scaleDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            float scale = Mathf.Lerp(1f, scaleMultiplier, smoothTime);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        yield return new WaitForSeconds(holdDuration);

        Vector3 startPosition = transform.position;
        duration = Mathf.Max(0.01f, floatDuration);
        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            transform.position = startPosition + Vector3.up * (floatDistance * smoothTime);
            Color color = originalColor;
            color.a = 1f - smoothTime;
            scoreText.color = color;
            yield return null;
        }

        Action<ScoreWorldEffect> callback = finished;
        finished = null;
        callback?.Invoke(this);
    }
}
