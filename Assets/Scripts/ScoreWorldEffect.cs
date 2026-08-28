using System.Collections;
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

    public void Play(int scoreValue)
    {
        scoreText.text = scoreValue.ToString();
        transform.position += Vector3.up * verticalOffset;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 originalScale = transform.localScale;
        Color originalColor = scoreText.color;
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

        Destroy(gameObject);
    }
}
