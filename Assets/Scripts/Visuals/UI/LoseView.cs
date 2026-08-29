using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoseView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    [Header("Panel Appearance")]
    [SerializeField] private float panelStartScale = 0.75f;
    [SerializeField] private float panelOvershootScale = 1.08f;
    [SerializeField] private float panelAppearDuration = 0.2f;
    [SerializeField] private float panelSettleDuration = 0.08f;

    public event Action RetryRequested;
    public event Action ExitRequested;

    private RectTransform panel;
    private Vector3 panelScale;
    private Coroutine animation;
    private bool initialized;

    public static LoseView FindInScene()
    {
        return FindAnyObjectByType<LoseView>(FindObjectsInactive.Include);
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

        panel = transform as RectTransform;
        panelScale = panel.localScale;
        retryButton?.onClick.AddListener(HandleRetryClicked);
        exitButton?.onClick.AddListener(HandleExitClicked);
        initialized = true;
        gameObject.SetActive(false);
    }

    public void Play()
    {
        Initialize();

        if (animation != null)
        {
            StopCoroutine(animation);
        }

        gameObject.SetActive(true);
        AudioManager.Instance?.PlaySfx(0.05f, SFX.UiAppear, 1.1f);
        AudioManager.Instance?.PlaySfx(0.05f, SFX.UiSwoosh, 1.1f);
        animation = StartCoroutine(PlayEntrance());
    }

    private IEnumerator PlayEntrance()
    {
        yield return ScalePanel(panelStartScale, panelOvershootScale, panelAppearDuration);
        yield return ScalePanel(panelOvershootScale, 1f, panelSettleDuration);
        animation = null;
    }

    private IEnumerator ScalePanel(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        panel.localScale = panelScale * from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = progress * progress * (3f - 2f * progress);
            panel.localScale = panelScale * Mathf.LerpUnclamped(from, to, smooth);
            yield return null;
        }

        panel.localScale = panelScale * to;
    }

    private void HandleRetryClicked()
    {
        RetryRequested?.Invoke();
    }

    private void HandleExitClicked()
    {
        ExitRequested?.Invoke();
    }

    private void OnDestroy()
    {
        retryButton?.onClick.RemoveListener(HandleRetryClicked);
        exitButton?.onClick.RemoveListener(HandleExitClicked);
    }
}
