using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadingScreenView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blackOverlay;

    [Header("Fade")]
    [SerializeField] private float fadeToBlackDuration = 0.35f;
    [SerializeField] private float fadeFromBlackDuration = 0.35f;
    [SerializeField] private float opaqueHoldDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float transparentAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float opaqueAlpha = 1f;

    [Header("Canvas")]
    [SerializeField] private int sortingOrder = 32767;

    private Canvas loadingCanvas;

    private void Awake()
    {
        transform.localScale = Vector3.one;
        loadingCanvas = GetComponent<Canvas>();

        if (loadingCanvas != null)
        {
            loadingCanvas.overrideSorting = true;
            loadingCanvas.sortingOrder = sortingOrder;
        }
    }

    public void ShowOpaque()
    {
        gameObject.SetActive(true);
        SetBlackAlpha(opaqueAlpha);
    }

    public IEnumerator FadeToBlack()
    {
        gameObject.SetActive(true);
        yield return FadeBlackTo(opaqueAlpha, fadeToBlackDuration);
    }

    public IEnumerator FadeFromBlack()
    {
        gameObject.SetActive(true);
        yield return FadeBlackTo(transparentAlpha, fadeFromBlackDuration);
        gameObject.SetActive(false);
    }

    public IEnumerator RenderOpaqueFrame()
    {
        gameObject.SetActive(true);
        SetBlackAlpha(opaqueAlpha);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
    }

    public IEnumerator HoldOpaqueBeforeReveal()
    {
        gameObject.SetActive(true);
        SetBlackAlpha(opaqueAlpha);
        Canvas.ForceUpdateCanvases();

        if (opaqueHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(opaqueHoldDuration);
        }

        yield return new WaitForEndOfFrame();
    }

    private IEnumerator FadeBlackTo(float targetAlpha, float duration)
    {
        if (blackOverlay == null)
        {
            yield break;
        }

        float startAlpha = blackOverlay.color.a;

        if (duration <= 0f)
        {
            SetBlackAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = progress * progress * (3f - 2f * progress);
            SetBlackAlpha(Mathf.Lerp(startAlpha, targetAlpha, smooth));
            yield return null;
        }

        SetBlackAlpha(targetAlpha);
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackOverlay == null)
        {
            return;
        }

        Color color = blackOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        blackOverlay.color = color;
    }
}
