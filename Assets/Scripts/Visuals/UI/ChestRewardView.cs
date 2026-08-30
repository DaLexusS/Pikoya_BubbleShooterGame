using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ChestRewardView : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName = "Level2";

    [Header("References")]
    [SerializeField] private RectTransform chestClose;
    [SerializeField] private RectTransform chestOpen;
    [SerializeField] private RectTransform bomb;

    [Header("Chest Star Particles")]
    [SerializeField] private ParticleSystem starParticlesPrefab;
    [SerializeField] private Vector2 starParticlesPosition;
    [SerializeField, Min(0f)] private float starParticlesScale = 100f;

    [Header("Chest Sounds")]
    [SerializeField, Range(0f, 1f)] private float rotationSoundVolume = 0.05f;
    [SerializeField, Range(0.1f, 3f)] private float rotationSoundPitch = 1f;
    [SerializeField, Range(0f, 1f)] private float openSoundVolume = 0.1f;
    [SerializeField, Range(0.1f, 3f)] private float openSoundPitch = 1f;
    [SerializeField, Range(0f, 1f)] private float openAccentVolume = 0.08f;
    [SerializeField, Range(0.1f, 3f)] private float openAccentPitch = 1f;

    [Header("Chest Appearance")]
    [SerializeField, Range(0f, 1f)] private float chestStartScale = 0.15f;
    [SerializeField, Min(0.01f)] private float chestAppearDuration = 0.25f;
    [SerializeField, Min(0f)] private float holdBeforeRotation = 1f;

    [Header("Chest Tilt")]
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField, Min(1)] private int tiltRepeats = 2;
    [SerializeField, Min(0.01f)] private float tiltDuration = 0.12f;
    [SerializeField, Min(0f)] private float rotationPauseDuration = 0.08f;

    [Header("Open Squash")]
    [SerializeField, Min(0.01f)] private float squashDuration = 0.08f;
    [SerializeField, Range(0.1f, 1f)] private float squashY = 0.72f;
    [SerializeField, Min(1f)] private float squashX = 1.08f;
    [SerializeField, Min(0.01f)] private float recoverDuration = 0.14f;

    [Header("Bomb Reveal")]
    [SerializeField, Range(0f, 1f)] private float bombStartScale = 0.2f;
    [SerializeField, Min(1f)] private float bombOvershootScale = 1.12f;
    [SerializeField, Min(0.01f)] private float bombAppearDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float bombSettleDuration = 0.1f;
    [SerializeField, Min(0f)] private float bombHoldDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float bombDropDuration = 0.3f;
    [SerializeField, Min(0f)] private float bombDropPadding = 100f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;

    private CanvasGroup canvasGroup;
    private Vector3 closedScale;
    private Vector3 openScale;
    private Vector3 bombScale;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Vector2 bombPosition;
    private bool initialized;

    public bool ShouldPlay => SceneManager.GetActiveScene().name == sceneName;

    public static ChestRewardView FindInScene()
    {
        return FindAnyObjectByType<ChestRewardView>(FindObjectsInactive.Include);
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        closedScale = chestClose.localScale;
        openScale = chestOpen.localScale;
        bombScale = bomb.localScale;
        closedRotation = chestClose.localRotation;
        openRotation = chestOpen.localRotation;
        bombPosition = bomb.anchoredPosition;
        initialized = true;
        ResetVisuals();
        gameObject.SetActive(false);
    }

    public IEnumerator Play()
    {
        Initialize();

        if (!ShouldPlay)
        {
            yield break;
        }

        ResetVisuals();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        canvasGroup.alpha = 1f;
        chestClose.gameObject.SetActive(true);

        yield return AnimateTransform(
            chestClose,
            closedScale * chestStartScale,
            closedScale,
            0f,
            0f,
            chestAppearDuration);

        if (holdBeforeRotation > 0f)
        {
            yield return new WaitForSecondsRealtime(holdBeforeRotation);
        }

        for (int repeat = 0; repeat < Mathf.Max(1, tiltRepeats); repeat++)
        {
            AudioManager.Instance?.PlaySfx(
                rotationSoundVolume,
                SFX.ChestRotate,
                rotationSoundPitch);
            yield return RotateChest(chestClose, -Mathf.Abs(tiltAngle), tiltDuration);

            if (rotationPauseDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(rotationPauseDuration);
            }

            yield return RotateChest(chestClose, Mathf.Abs(tiltAngle), tiltDuration);

            if (repeat < Mathf.Max(1, tiltRepeats) - 1 && rotationPauseDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(rotationPauseDuration);
            }
        }

        yield return RotateChest(chestClose, 0f, tiltDuration);
        chestClose.gameObject.SetActive(false);
        chestOpen.gameObject.SetActive(true);
        AudioManager.Instance?.PlaySfx(openSoundVolume, SFX.ChestOpenOne, openSoundPitch);
        AudioManager.Instance?.PlaySfx(openAccentVolume, SFX.ChestOpenTwo, openAccentPitch);

        Vector3 squashedScale = new Vector3(
            openScale.x * squashX,
            openScale.y * squashY,
            openScale.z);
        yield return AnimateTransform(
            chestOpen,
            openScale,
            squashedScale,
            0f,
            0f,
            squashDuration);
        yield return AnimateTransform(
            chestOpen,
            squashedScale,
            openScale,
            0f,
            0f,
            recoverDuration);

        PlayStarParticles();
        bomb.localScale = bombScale * bombStartScale;
        bomb.gameObject.SetActive(true);
        yield return AnimateTransform(
            bomb,
            bombScale * bombStartScale,
            bombScale * bombOvershootScale,
            0f,
            0f,
            bombAppearDuration);
        yield return AnimateTransform(
            bomb,
            bombScale * bombOvershootScale,
            bombScale,
            0f,
            0f,
            bombSettleDuration);

        if (bombHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(bombHoldDuration);
        }

        yield return DropBombAndFadePanel();
        gameObject.SetActive(false);
    }

    private void PlayStarParticles()
    {
        if (starParticlesPrefab == null || chestOpen == null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

        if (canvasRect == null)
        {
            return;
        }

        ParticleSystem particles = Instantiate(starParticlesPrefab, canvasRect, false);
        particles.transform.position = chestOpen.position;
        particles.transform.localPosition += (Vector3)starParticlesPosition;
        particles.transform.localRotation = Quaternion.identity;
        particles.transform.localScale = Vector3.one * starParticlesScale;
        particles.transform.SetAsLastSibling();
        AddCanvasRenderers(particles);
        StartCoroutine(PlayParticleEffect(particles));
    }

    private static void AddCanvasRenderers(ParticleSystem root)
    {
        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystemRenderer sourceRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            if (sourceRenderer == null)
            {
                continue;
            }

            GameObject rendererObject = new GameObject(
                $"{particleSystem.name} UI Renderer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIParticleGraphic));
            RectTransform rendererTransform = rendererObject.transform as RectTransform;
            rendererTransform.SetParent(particleSystem.transform, false);

            UIParticleGraphic graphic = rendererObject.GetComponent<UIParticleGraphic>();
            graphic.Initialize(particleSystem, sourceRenderer.sharedMaterial);
            sourceRenderer.enabled = false;
        }
    }

    private static IEnumerator PlayParticleEffect(ParticleSystem root)
    {
        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.useUnscaledTime = true;
        }

        root.Play(true);

        while (root != null && root.IsAlive(true))
        {
            yield return null;
        }

        if (root != null)
        {
            Destroy(root.gameObject);
        }
    }

    private IEnumerator RotateChest(RectTransform target, float angle, float duration)
    {
        float startAngle = NormalizeAngle(target.localEulerAngles.z);
        yield return AnimateTransform(
            target,
            target.localScale,
            target.localScale,
            startAngle,
            angle,
            duration);
    }

    private static IEnumerator AnimateTransform(
        RectTransform target,
        Vector3 startScale,
        Vector3 endScale,
        float startAngle,
        float endAngle,
        float duration)
    {
        float animationDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            float smooth = progress * progress * (3f - 2f * progress);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, smooth);
            float angle = Mathf.LerpAngle(startAngle, endAngle, smooth);
            target.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        target.localScale = endScale;
        target.localRotation = Quaternion.Euler(0f, 0f, endAngle);
    }

    private IEnumerator DropBombAndFadePanel()
    {
        float dropTime = Mathf.Max(0.01f, bombDropDuration);
        float fadeTime = Mathf.Max(0.01f, fadeDuration);
        float animationDuration = Mathf.Max(dropTime, fadeTime);
        RectTransform panel = transform as RectTransform;
        float dropDistance = panel != null
            ? panel.rect.height + bomb.rect.height + bombDropPadding
            : Screen.height + bomb.rect.height + bombDropPadding;
        Vector2 startPosition = bomb.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.down * dropDistance;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float dropProgress = Mathf.Clamp01(elapsed / dropTime);
            float dropSmooth = dropProgress * dropProgress * (3f - 2f * dropProgress);
            bomb.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, dropSmooth);

            float fadeProgress = Mathf.Clamp01(elapsed / fadeTime);
            float fadeSmooth = fadeProgress * fadeProgress * (3f - 2f * fadeProgress);
            canvasGroup.alpha = 1f - fadeSmooth;
            yield return null;
        }

        bomb.anchoredPosition = endPosition;
        canvasGroup.alpha = 0f;
    }

    private void ResetVisuals()
    {
        canvasGroup.alpha = 0f;
        chestClose.localScale = closedScale;
        chestClose.localRotation = closedRotation;
        chestOpen.localScale = openScale;
        chestOpen.localRotation = openRotation;
        bomb.localScale = bombScale;
        bomb.anchoredPosition = bombPosition;
        chestClose.gameObject.SetActive(false);
        chestOpen.gameObject.SetActive(false);
        bomb.gameObject.SetActive(false);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
