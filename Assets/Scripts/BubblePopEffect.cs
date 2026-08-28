using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BubblePopEffect : MonoBehaviour
{
    [SerializeField] private BubbleView bubbleView;
    [SerializeField] private Transform popRoot;
    [SerializeField] private ScoreWorldEffect scoreEffectPrefab;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float scaleDuration = 0.05f;
    [SerializeField] private float scaleMultiplier = 1.3f;
    [SerializeField] private float delayAfterPop = 0.01f;
    [SerializeField] private UnityEvent onPop = new UnityEvent();

    private ParticleSystem[] popParticles;

    private void Awake()
    {
        popParticles = popRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    public float DelayAfterPop => delayAfterPop;

    public IEnumerator Play()
    {
        BubbleAttachEffect attachEffect = GetComponent<BubbleAttachEffect>();

        if (attachEffect != null)
        {
            attachEffect.Stop();
        }

        Transform bubbleVisual = bubbleView.BubbleVisualTransform;
        Transform strokeVisual = bubbleView.StrokeVisualTransform;
        Vector3 bubbleStartScale = bubbleVisual.localScale;
        Vector3 strokeStartScale = strokeVisual.localScale;
        float duration = Mathf.Max(0.01f, scaleDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float smoothTime = time * time * (3f - 2f * time);
            float scale = Mathf.Lerp(1f, scaleMultiplier, smoothTime);
            bubbleVisual.localScale = bubbleStartScale * scale;
            strokeVisual.localScale = strokeStartScale * scale;
            yield return null;
        }

        PlayParticles();
        PlayScoreEffect();
        onPop.Invoke();
    }

    private void PlayScoreEffect()
    {
        ScoreWorldEffect scoreEffect = Instantiate(scoreEffectPrefab, transform.position, Quaternion.identity);
        scoreEffect.Play(scoreValue);
    }

    private void PlayParticles()
    {
        Color color = bubbleView.DisplayColor;
        float lifetime = 0.1f;
        popRoot.SetParent(null, true);
        popRoot.gameObject.SetActive(true);

        foreach (ParticleSystem particle in popParticles)
        {
            ParticleSystem.MainModule main = particle.main;
            main.startColor = color;
            float particleLifetime = (main.duration + main.startLifetime.constantMax) / main.simulationSpeed;
            lifetime = Mathf.Max(lifetime, particleLifetime);
            particle.Play();
        }

        Destroy(popRoot.gameObject, lifetime);
    }
}
