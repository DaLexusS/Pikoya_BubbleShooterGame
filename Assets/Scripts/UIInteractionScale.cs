using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIInteractionScale :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float clickBounceScale = 1.12f;

    [Header("Timing")]
    [SerializeField] private float transitionDuration = 0.08f;
    [SerializeField] private float clickBounceDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 originalScale;
    private Coroutine animation;
    private bool initialized;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        originalScale = transform.localScale;
        initialized = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            AnimateTo(hoverScale);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            AnimateTo(1f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        AnimateTo(pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        AnimateTo(isHovered ? hoverScale : 1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartAnimation(PlayClickBounce());
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            AnimateTo(hoverScale);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        AnimateTo(1f);
    }

    private void AnimateTo(float multiplier)
    {
        StartAnimation(ScaleTo(originalScale * multiplier, transitionDuration));
    }

    private void StartAnimation(IEnumerator sequence)
    {
        Initialize();

        if (animation != null)
        {
            StopCoroutine(animation);
        }

        animation = StartCoroutine(sequence);
    }

    private IEnumerator PlayClickBounce()
    {
        yield return ScaleTo(
            originalScale * clickBounceScale,
            clickBounceDuration);
        yield return ScaleTo(
            originalScale * (isHovered ? hoverScale : 1f),
            clickBounceDuration);
        animation = null;
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;

        if (duration <= 0f)
        {
            transform.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = progress * progress * (3f - 2f * progress);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, smooth);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }

        if (animation != null)
        {
            StopCoroutine(animation);
            animation = null;
        }

        isHovered = false;
        isPressed = false;
        transform.localScale = originalScale;
    }
}
