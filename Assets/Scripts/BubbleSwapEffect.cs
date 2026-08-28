using System.Collections;
using UnityEngine;

public class BubbleSwapEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float curveDistance = 0.35f;

    public void Play(
        BubbleView firstBubble,
        Transform firstTarget,
        Vector3 firstTargetScale,
        BubbleView secondBubble,
        Transform secondTarget,
        Vector3 secondTargetScale,
        System.Action onFinished)
    {
        StartCoroutine(Animate(
            firstBubble,
            firstTarget,
            firstTargetScale,
            secondBubble,
            secondTarget,
            secondTargetScale,
            onFinished));
    }

    public void PlayPromotion(
        BubbleView bubble,
        Transform target,
        Vector3 targetScale,
        System.Action onFinished)
    {
        StartCoroutine(AnimatePromotion(bubble, target, targetScale, onFinished));
    }

    private IEnumerator Animate(
        BubbleView firstBubble,
        Transform firstTarget,
        Vector3 firstTargetScale,
        BubbleView secondBubble,
        Transform secondTarget,
        Vector3 secondTargetScale,
        System.Action onFinished)
    {
        Vector3 firstStartPosition = firstBubble.transform.position;
        Vector3 secondStartPosition = secondBubble.transform.position;
        firstBubble.transform.SetParent(null, true);
        secondBubble.transform.SetParent(null, true);
        Vector3 firstStartScale = firstBubble.transform.localScale;
        Vector3 secondStartScale = secondBubble.transform.localScale;
        Vector3 direction = (firstTarget.position - firstStartPosition).normalized;
        Vector3 curveDirection = new Vector3(-direction.y, direction.x, 0f);
        float animationDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / animationDuration);
            float smoothTime = time * time * (3f - 2f * time);
            float curve = Mathf.Sin(time * Mathf.PI) * curveDistance;
            firstBubble.transform.position = Vector3.Lerp(firstStartPosition, firstTarget.position, smoothTime) - curveDirection * curve;
            secondBubble.transform.position = Vector3.Lerp(secondStartPosition, secondTarget.position, smoothTime) + curveDirection * curve;
            firstBubble.transform.localScale = Vector3.Lerp(firstStartScale, firstTargetScale, smoothTime);
            secondBubble.transform.localScale = Vector3.Lerp(secondStartScale, secondTargetScale, smoothTime);
            yield return null;
        }

        PlaceBubble(firstBubble, firstTarget, firstTargetScale);
        PlaceBubble(secondBubble, secondTarget, secondTargetScale);
        onFinished?.Invoke();
    }


    private IEnumerator AnimatePromotion(
        BubbleView bubble,
        Transform target,
        Vector3 targetScale,
        System.Action onFinished)
    {
        Vector3 startPosition = bubble.transform.position;
        bubble.transform.SetParent(null, true);
        Vector3 startScale = bubble.transform.localScale;
        Vector3 direction = (target.position - startPosition).normalized;
        Vector3 curveDirection = new Vector3(-direction.y, direction.x, 0f);
        float animationDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / animationDuration);
            float smoothTime = time * time * (3f - 2f * time);
            float curve = Mathf.Sin(time * Mathf.PI) * curveDistance;
            bubble.transform.position = Vector3.Lerp(startPosition, target.position, smoothTime) - curveDirection * curve;
            bubble.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothTime);
            yield return null;
        }

        PlaceBubble(bubble, target, targetScale);
        onFinished?.Invoke();
    }
    private static void PlaceBubble(BubbleView bubble, Transform target, Vector3 targetScale)
    {
        bubble.transform.SetParent(target);
        bubble.transform.localPosition = Vector3.zero;
        bubble.transform.localRotation = Quaternion.identity;
        bubble.transform.localScale = targetScale;
    }
}
