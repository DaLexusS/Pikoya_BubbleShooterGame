using System;
using System.Collections;
using UnityEngine;

public sealed class ExcellentView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Entrance")]
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float overshootScale = 1.1f;
    [SerializeField] private float appearDuration = 0.18f;
    [SerializeField] private float settleDuration = 0.12f;

    [Header("Hold And Exit")]
    [SerializeField] private float visibleDuration = 0.3f;
    [SerializeField] private float disappearDuration = 0.2f;
    [SerializeField] private float disappearScale = 0.2f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine animation;
    private bool initialized;

    public static ExcellentView FindInScene()
    {
        return FindAnyObjectByType<ExcellentView>(FindObjectsInactive.Include);
    }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        rectTransform = transform as RectTransform;
        canvasGroup ??= GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        originalScale = rectTransform.localScale;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        initialized = true;
        gameObject.SetActive(false);
    }

    public void Play(Action onFinished)
    {
        Initialize();
        gameObject.SetActive(true);
        AudioManager.Instance?.PlaySfx(0.1f, SFX.ExcellentAppear, 1f);

        if (animation != null)
        {
            StopCoroutine(animation);
        }

        animation = StartCoroutine(PlaySequence(onFinished));
    }

    private IEnumerator PlaySequence(Action onFinished)
    {
        canvasGroup.alpha = 1f;
        rectTransform.localScale = originalScale * startScale;

        yield return ScaleTo(overshootScale, appearDuration);
        yield return ScaleTo(1f, settleDuration);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, visibleDuration));
        yield return ScaleAndFadeOut();

        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 0f;
        animation = null;
        gameObject.SetActive(false);
        onFinished?.Invoke();
    }

    private IEnumerator ScaleTo(float targetMultiplier, float duration)
    {
        Vector3 start = rectTransform.localScale;
        Vector3 target = originalScale * targetMultiplier;
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = progress * progress * (3f - 2f * progress);
            rectTransform.localScale = Vector3.LerpUnclamped(start, target, smooth);
            yield return null;
        }

        rectTransform.localScale = target;
    }

    private IEnumerator ScaleAndFadeOut()
    {
        Vector3 start = rectTransform.localScale;
        Vector3 target = originalScale * disappearScale;
        float duration = Mathf.Max(0.01f, disappearDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = progress * progress * (3f - 2f * progress);
            rectTransform.localScale = Vector3.LerpUnclamped(start, target, smooth);
            canvasGroup.alpha = 1f - smooth;
            yield return null;
        }

        rectTransform.localScale = target;
        canvasGroup.alpha = 0f;
    }
}
