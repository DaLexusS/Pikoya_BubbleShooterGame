using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class VictoryView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private RectTransform[] stars = new RectTransform[3];
    [SerializeField] private StarRewardEffect victoryStarPrefab;
    [SerializeField] private ScoreBarView scoreBarView;

    [Header("Star Appearance")]
    [SerializeField] private Color earnedStarColor = new Color(1f, 0.82f, 0f, 1f);

    [Header("Panel Appearance")]
    [SerializeField] private float panelStartScale = 0.75f;
    [SerializeField] private float panelOvershootScale = 1.08f;
    [SerializeField] private float panelAppearDuration = 0.2f;
    [SerializeField] private float panelSettleDuration = 0.08f;
    [SerializeField] private float delayBeforeStars = 0.12f;

    [Header("Star Arrival")]
    [SerializeField] private float starFlightDuration = 0.25f;
    [SerializeField] private float starStartScale = 2f;
    [SerializeField] private float starOffscreenPadding = 120f;
    [SerializeField] private float delayBetweenStars = 0.04f;
    [SerializeField] private float landingParticleScale = 3f;

    [Header("Landing Feedback")]
    [SerializeField] private float starPulseScale = 0.72f;
    [SerializeField] private float starPulseDuration = 0.22f;
    [SerializeField] private float panelImpactScale = 1.035f;
    [SerializeField] private float panelImpactInDuration = 0.06f;
    [SerializeField] private float panelImpactOutDuration = 0.1f;

    [Header("Idle Glow")]
    [SerializeField] private string glowProperty = "_GlowPower";
    [SerializeField] private float glowNormalPower = 1f;
    [SerializeField] private float glowPeakPower = 2f;
    [SerializeField] private float glowPulseDuration = 0.5f;
    [SerializeField] private float glowDelayBetweenStars = 0.2f;
    [SerializeField] private float glowDelayAfterSequence = 1f;

    public event Action ExitRequested;
    public event Action NextRequested;

    private RectTransform panel;
    private RectTransform canvasRect;
    private Image[] starImages;
    private Material[] starMaterials;
    private Material[] originalMaterials;
    private Vector3[] starScales;
    private Quaternion[] starRotations;
    private Color[] lockedColors;
    private Vector3 panelScale;
    private int glowPowerId;
    private bool initialized;

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        panel = transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        panelScale = panel.localScale;
        glowPowerId = Shader.PropertyToID(glowProperty);

        int count = stars != null ? stars.Length : 0;
        starImages = new Image[count];
        starMaterials = new Material[count];
        originalMaterials = new Material[count];
        starScales = new Vector3[count];
        starRotations = new Quaternion[count];
        lockedColors = new Color[count];

        for (int index = 0; index < count; index++)
        {
            if (stars[index] == null)
            {
                continue;
            }

            starImages[index] = stars[index].GetComponent<Image>();
            starScales[index] = stars[index].localScale;
            starRotations[index] = stars[index].localRotation;

            if (starImages[index] != null)
            {
                lockedColors[index] = starImages[index].color;
                originalMaterials[index] = starImages[index].material;
            }
        }

        exitButton?.onClick.AddListener(HandleExitClicked);
        nextButton?.onClick.AddListener(HandleNextClicked);
        initialized = true;
    }

    public void Play(int score, int earnedStarCount)
    {
        Initialize();
        StopAllCoroutines();

        if (scoreValue != null)
        {
            scoreValue.text = score.ToString("N0");
        }

        ResetStars();
        gameObject.SetActive(true);
        StartCoroutine(PlaySequence(Mathf.Clamp(earnedStarCount, 0, stars.Length)));
    }

    private void ResetStars()
    {
        for (int index = 0; index < stars.Length; index++)
        {
            if (stars[index] == null)
            {
                continue;
            }

            stars[index].localScale = starScales[index];
            stars[index].localRotation = starRotations[index];

            if (starImages[index] != null)
            {
                starImages[index].color = lockedColors[index];
                starImages[index].material = originalMaterials[index];
            }

            if (starMaterials[index] != null)
            {
                Destroy(starMaterials[index]);
                starMaterials[index] = null;
            }
        }
    }

    private IEnumerator PlaySequence(int earnedStarCount)
    {
        yield return ScalePanel(panelStartScale, panelOvershootScale, panelAppearDuration);
        yield return ScalePanel(panelOvershootScale, 1f, panelSettleDuration);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delayBeforeStars));

        for (int index = 0; index < earnedStarCount; index++)
        {
            if (stars[index] == null)
            {
                continue;
            }

            yield return AwardStar(index);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delayBetweenStars));
        }

        StartCoroutine(PlayGlowSequence(earnedStarCount));
    }

    private IEnumerator AwardStar(int index)
    {
        RectTransform target = stars[index];
        bool arrived = false;

        if (canvasRect != null && victoryStarPrefab != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(camera, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                targetScreen,
                camera,
                out Vector2 end);
            Vector2 start = new Vector2(end.x, canvasRect.rect.yMax + starOffscreenPadding);
            StarRewardEffect reward = Instantiate(victoryStarPrefab, canvasRect);
            reward.transform.SetAsLastSibling();

            Image flyingStar = reward.GetComponent<Image>();
            if (flyingStar != null)
            {
                flyingStar.color = earnedStarColor;
                CreateStarMaterial(index, flyingStar.material);
            }

            reward.PlayDirect(
                start,
                end,
                starFlightDuration,
                starStartScale,
                () => arrived = true);

            while (!arrived)
            {
                yield return null;
            }
        }

        if (starImages[index] != null)
        {
            if (starMaterials[index] != null)
            {
                starImages[index].material = starMaterials[index];
            }

            starImages[index].color = earnedStarColor;
        }

        scoreBarView?.PlayLandingParticles(target, landingParticleScale);
        StartCoroutine(PulseStar(index));
        yield return ScalePanel(1f, panelImpactScale, panelImpactInDuration);
        yield return ScalePanel(panelImpactScale, 1f, panelImpactOutDuration);
    }

    private void CreateStarMaterial(int index, Material source)
    {
        if (source == null || starImages[index] == null)
        {
            return;
        }

        starMaterials[index] = new Material(source);
        if (starMaterials[index].HasProperty(glowPowerId))
        {
            starMaterials[index].SetFloat(glowPowerId, glowNormalPower);
        }

    }

    private IEnumerator PlayGlowSequence(int earnedStarCount)
    {
        while (gameObject.activeInHierarchy)
        {
            for (int index = 0; index < earnedStarCount; index++)
            {
                Material material = starMaterials[index];
                if (material == null || !material.HasProperty(glowPowerId))
                {
                    continue;
                }

                yield return PulseGlow(material);

                if (index < earnedStarCount - 1)
                {
                    yield return new WaitForSecondsRealtime(
                        Mathf.Max(0f, glowDelayBetweenStars));
                }
            }

            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, glowDelayAfterSequence));
        }
    }

    private IEnumerator PulseGlow(Material material)
    {
        float duration = Mathf.Max(0.01f, glowPulseDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float power = progress < 0.5f
                ? Mathf.Lerp(glowNormalPower, glowPeakPower, progress * 2f)
                : Mathf.Lerp(glowPeakPower, glowNormalPower, (progress - 0.5f) * 2f);
            material.SetFloat(glowPowerId, power);
            yield return null;
        }

        material.SetFloat(glowPowerId, glowNormalPower);
    }

    private IEnumerator PulseStar(int index)
    {
        float duration = Mathf.Max(0.01f, starPulseDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            stars[index].localScale =
                starScales[index] * Mathf.Lerp(1f, starPulseScale, pulse);
            yield return null;
        }

        stars[index].localScale = starScales[index];
        stars[index].localRotation = starRotations[index];
    }

    private IEnumerator ScalePanel(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

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

    private void HandleExitClicked()
    {
        ExitRequested?.Invoke();
    }

    private void HandleNextClicked()
    {
        NextRequested?.Invoke();
    }

    private void OnDestroy()
    {
        exitButton?.onClick.RemoveListener(HandleExitClicked);
        nextButton?.onClick.RemoveListener(HandleNextClicked);

        if (starMaterials == null)
        {
            return;
        }

        foreach (Material material in starMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
