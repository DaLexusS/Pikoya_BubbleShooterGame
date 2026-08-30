using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinCelebrationView : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private FireworkPool fireworkPool;
    [SerializeField] private float bubbleLaunchInterval = 0.04f;
    [SerializeField] private float bubbleFlightDuration = 0.8f;
    [SerializeField] private Vector2 horizontalViewportRange = new Vector2(0.15f, 0.85f);
    [SerializeField] private Vector2 verticalViewportRange = new Vector2(0.3f, 0.75f);
    [SerializeField] private float minimumEndOpacity = 0.2f;
    [SerializeField] private float maximumRotation = 240f;
    [SerializeField] private float postVictoryFireworkDuration = 1f;
    [SerializeField] private float postVictoryFireworkInterval = 0.15f;

    public void Play(IReadOnlyList<BubbleView> bubbles, Action onBubbleStarted, Action onFinished = null)
    {
        StartCoroutine(PlaySequence(bubbles, onBubbleStarted, onFinished));
    }

    private IEnumerator PlaySequence(IReadOnlyList<BubbleView> bubbles, Action onBubbleStarted, Action onFinished)
    {
        int remaining = bubbles.Count;

        if (remaining > 0)
        {
            AudioManager.Instance?.PlaySfx(0.1f, SFX.EndBubblesPop, 1f);
            AudioManager.Instance?.PlaySfx(0.1f, SFX.Fireworks, 1f);

            foreach (BubbleView bubble in bubbles)
            {
                onBubbleStarted?.Invoke();
                fireworkPool.Play(GetRandomScreenPosition(0f));
                StartCoroutine(AnimateBubble(bubble, () => remaining--));

                if (bubbleLaunchInterval > 0f)
                {
                    yield return new WaitForSeconds(bubbleLaunchInterval);
                }
            }

            while (remaining > 0)
            {
                yield return null;
            }
        }

        onFinished?.Invoke();

        float elapsed = 0f;
        float duration = Mathf.Max(0f, postVictoryFireworkDuration);
        float interval = Mathf.Max(0.01f, postVictoryFireworkInterval);

        while (elapsed < duration)
        {
            fireworkPool.Play(GetRandomScreenPosition(0f));
            yield return new WaitForSecondsRealtime(interval);
            elapsed += interval;
        }
    }

    private IEnumerator AnimateBubble(BubbleView bubble, Action onFinished)
    {
        Collider2D bubbleCollider = bubble.GetComponent<Collider2D>();

        if (bubbleCollider != null)
        {
            bubbleCollider.enabled = false;
        }

        Vector3 startPosition = bubble.transform.position;
        Vector3 targetPosition = GetRandomScreenPosition(startPosition.z);
        float rotation = UnityEngine.Random.Range(-maximumRotation, maximumRotation);
        float duration = Mathf.Max(0.01f, bubbleFlightDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            bubble.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothTime);
            bubble.transform.localRotation = Quaternion.Euler(0f, 0f, rotation * smoothTime);
            bubble.SetOpacity(Mathf.Lerp(1f, minimumEndOpacity, smoothTime));
            yield return null;
        }

        BubblePopEffect popEffect = bubble.GetComponent<BubblePopEffect>();

        if (popEffect != null)
        {
            yield return popEffect.Play();
        }

        Destroy(bubble.gameObject);
        onFinished?.Invoke();
    }

    private Vector3 GetRandomScreenPosition(float worldZ)
    {
        float viewportX = UnityEngine.Random.Range(horizontalViewportRange.x, horizontalViewportRange.y);
        float viewportY = UnityEngine.Random.Range(verticalViewportRange.x, verticalViewportRange.y);
        float cameraDistance = Mathf.Abs(worldZ - gameplayCamera.transform.position.z);
        Vector3 position = gameplayCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, cameraDistance));
        position.z = worldZ;
        return position;
    }
}
