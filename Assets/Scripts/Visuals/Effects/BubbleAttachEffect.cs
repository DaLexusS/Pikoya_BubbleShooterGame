using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleShockTarget
{
    public BubbleShockTarget(BubbleView bubble, int ring)
    {
        Bubble = bubble;
        Ring = ring;
    }

    public BubbleView Bubble { get; }
    public int Ring { get; }
}

public class BubbleAttachEffect : MonoBehaviour
{
    [SerializeField] private BubbleView bubbleView;
    [SerializeField] private float attachDuration = 0.25f;
    [SerializeField] private float attachElasticity = 0.12f;
    [SerializeField] private float attachScalePulse = 0.08f;
    [SerializeField] private float shockDelay = 0.03f;
    [Range(1, 3)]
    [SerializeField] private int shockRings = 3;
    [SerializeField] private float shockRingDelay = 0.04f;
    [Range(0f, 1f)]
    [SerializeField] private float shockRingFalloff = 0.3f;
    [SerializeField] private float shockDuration = 0.25f;
    [SerializeField] private float shockDistance = 0.06f;
    [SerializeField] private float shockScalePulse = 0.06f;

    private Coroutine activeAnimation;
    private Transform[] visuals;
    private Vector3[] restingPositions;
    private Vector3[] restingScales;

    private void Awake()
    {
        visuals = new[]
        {
            bubbleView.BubbleVisualTransform,
            bubbleView.StrokeVisualTransform
        };
        restingPositions = new Vector3[visuals.Length];
        restingScales = new Vector3[visuals.Length];

        for (int index = 0; index < visuals.Length; index++)
        {
            restingPositions[index] = visuals[index].localPosition;
            restingScales[index] = visuals[index].localScale;
        }
    }

    public int ShockRings => shockRings;

    public void Stop()
    {
        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        ResetVisual();
    }

    public void Play(Vector2 impactPosition, IReadOnlyList<BubbleShockTarget> targets)
    {
        PlayAttach(impactPosition);

        foreach (BubbleShockTarget target in targets)
        {
            BubbleAttachEffect targetEffect = target.Bubble.GetComponent<BubbleAttachEffect>();

            if (targetEffect != null)
            {
                Vector2 direction = target.Bubble.transform.position - transform.position;
                targetEffect.PlayShock(direction.normalized, target.Ring);
            }
        }
    }

    private void PlayAttach(Vector2 impactPosition)
    {
        Vector3[] impactPositions = new Vector3[visuals.Length];

        for (int index = 0; index < visuals.Length; index++)
        {
            Vector3 worldImpact = new Vector3(impactPosition.x, impactPosition.y, visuals[index].position.z);
            Vector3 localPosition = visuals[index].parent.InverseTransformPoint(worldImpact);
            localPosition.z = restingPositions[index].z;
            impactPositions[index] = localPosition;
        }

        StartVisualAnimation(AnimateAttach(impactPositions));
    }

    private void PlayShock(Vector2 worldDirection, int ring)
    {
        Vector3 localDirection = visuals[0].parent.InverseTransformVector(worldDirection).normalized;
        localDirection.z = 0f;
        float strength = Mathf.Pow(1f - shockRingFalloff, ring - 1);
        StartVisualAnimation(AnimateShock(localDirection, ring, strength));
    }

    private void StartVisualAnimation(IEnumerator animation)
    {
        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
        }

        ResetVisual();
        activeAnimation = StartCoroutine(animation);
    }

    private IEnumerator AnimateAttach(Vector3[] impactPositions)
    {
        float duration = Mathf.Max(0.01f, attachDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            float elasticOffset = Mathf.Sin(time * Mathf.PI * 2f) * (1f - time) * attachElasticity;
            float movement = smoothTime + elasticOffset;
            float scalePulse = 1f + Mathf.Sin(time * Mathf.PI) * attachScalePulse;

            for (int index = 0; index < visuals.Length; index++)
            {
                visuals[index].localPosition = Vector3.LerpUnclamped(impactPositions[index], restingPositions[index], movement);
                visuals[index].localScale = restingScales[index] * scalePulse;
            }

            yield return null;
        }

        ResetVisual();
        activeAnimation = null;
    }

    private IEnumerator AnimateShock(Vector3 localDirection, int ring, float strength)
    {
        float delay = shockDelay + (ring - 1) * shockRingDelay;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float duration = Mathf.Max(0.01f, shockDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(time * Mathf.PI);
            float distance = wave * shockDistance * strength;
            float scalePulse = 1f + wave * shockScalePulse * strength;

            for (int index = 0; index < visuals.Length; index++)
            {
                visuals[index].localPosition = restingPositions[index] + localDirection * distance;
                visuals[index].localScale = restingScales[index] * scalePulse;
            }

            yield return null;
        }

        ResetVisual();
        activeAnimation = null;
    }

    private void OnDisable()
    {
        ResetVisual();
    }

    private void ResetVisual()
    {
        if (visuals == null)
        {
            return;
        }

        for (int index = 0; index < visuals.Length; index++)
        {
            visuals[index].localPosition = restingPositions[index];
            visuals[index].localScale = restingScales[index];
        }
    }
}
