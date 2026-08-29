using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBarView : MonoBehaviour
{
    [SerializeField] private Slider scoreSlider;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private RectTransform[] starMarkers = new RectTransform[3];
    [SerializeField] private Image[] starIcons = new Image[3];
    [SerializeField] private StarRewardEffect starRewardPrefab;
    [SerializeField] private ParticleSystem starLandingParticlesPrefab;
    [SerializeField] private float particleUiScale = 100f;
    [SerializeField] private Color lockedStarColor = new Color(0.42f, 0.42f, 0.42f, 1f);
    [SerializeField] private Color earnedStarColor = Color.white;
    [SerializeField] private float glowScale = 1.3f;
    [SerializeField] private float glowDuration = 0.3f;

    private readonly int[] starScores = new int[3];
    private readonly bool[] earnedStars = new bool[3];
    private Canvas canvas;
    private RectTransform canvasRect;

    public void Initialize(LevelData level)
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;
        scoreSlider.minValue = 0f;
        scoreSlider.maxValue = level.MaxScore;
        scoreSlider.wholeNumbers = true;
        scoreSlider.SetValueWithoutNotify(0f);
        scoreText.text = 0.ToString("N0");

        for (int index = 0; index < starScores.Length; index++)
        {
            starScores[index] = level.GetStarScore(index);
            earnedStars[index] = starScores[index] <= 0;
            starIcons[index].color = earnedStars[index] ? earnedStarColor : lockedStarColor;
            PositionStarMarker(starMarkers[index], starScores[index] / (float)level.MaxScore);
        }
    }

    public void SetScore(int score)
    {
        scoreSlider.SetValueWithoutNotify(score);
        scoreText.text = score.ToString("N0");

        for (int index = 0; index < starScores.Length; index++)
        {
            if (earnedStars[index] || score < starScores[index])
            {
                continue;
            }

            earnedStars[index] = true;
            PlayStarReward(index);
        }
    }

    private void PositionStarMarker(RectTransform marker, float progress)
    {
        RectTransform sliderRect = scoreSlider.transform as RectTransform;
        Vector3 leftWorld = sliderRect.TransformPoint(new Vector3(sliderRect.rect.xMin, 0f));
        Vector3 rightWorld = sliderRect.TransformPoint(new Vector3(sliderRect.rect.xMax, 0f));
        Vector3 markerLocalPosition = marker.parent.InverseTransformPoint(
            Vector3.Lerp(leftWorld, rightWorld, Mathf.Clamp01(progress)));
        Vector2 markerPosition = marker.anchoredPosition;
        markerPosition.x = markerLocalPosition.x;
        marker.anchoredPosition = markerPosition;
    }

    private void PlayStarReward(int index)
    {
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 sourceScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, starIcons[index].rectTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            sourceScreenPosition,
            canvasCamera,
            out Vector2 sourcePosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreenPosition,
            canvasCamera,
            out Vector2 targetPosition);

        if (starRewardPrefab == null)
        {
            CompleteStarReward(index, targetPosition);
            return;
        }

        StarRewardEffect reward = Instantiate(starRewardPrefab, canvasRect);
        reward.transform.SetAsLastSibling();
        reward.Play(sourcePosition, targetPosition, () => CompleteStarReward(index, targetPosition));
    }

    private void CompleteStarReward(int index, Vector2 targetPosition)
    {
        RevealStar(index);

        if (starLandingParticlesPrefab == null)
        {
            return;
        }

        ParticleSystem reward = Instantiate(starLandingParticlesPrefab, canvasRect, false);
        reward.transform.localPosition = targetPosition;
        reward.transform.localRotation = Quaternion.identity;
        reward.transform.localScale = Vector3.one * particleUiScale;
        reward.transform.SetAsLastSibling();
        AddCanvasRenderers(reward);
        StartCoroutine(PlayParticleReward(reward));
    }

    private static void AddCanvasRenderers(ParticleSystem reward)
    {
        ParticleSystem[] particleSystems = reward.GetComponentsInChildren<ParticleSystem>(true);

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
            rendererTransform.localPosition = Vector3.zero;
            rendererTransform.localRotation = Quaternion.identity;
            rendererTransform.localScale = Vector3.one;

            UIParticleGraphic graphic = rendererObject.GetComponent<UIParticleGraphic>();
            graphic.Initialize(particleSystem, sourceRenderer.sharedMaterial);
            sourceRenderer.enabled = false;
        }
    }

    private IEnumerator PlayParticleReward(ParticleSystem reward)
    {
        ParticleSystem[] particleSystems = reward.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
        }

        reward.Play(true);

        while (reward != null && reward.IsAlive(true))
        {
            yield return null;
        }

        if (reward != null)
        {
            Destroy(reward.gameObject);
        }
    }

    private void RevealStar(int index)
    {
        Image starIcon = starIcons[index];
        starIcon.color = earnedStarColor;
        StartCoroutine(AnimateGlow(starIcon.rectTransform));
    }

    private IEnumerator AnimateGlow(RectTransform star)
    {
        Vector3 startScale = star.localScale;
        float duration = Mathf.Max(0.01f, glowDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(time * Mathf.PI);
            star.localScale = startScale * Mathf.Lerp(1f, glowScale, pulse);
            yield return null;
        }

        star.localScale = startScale;
    }
}

internal sealed class UIParticleGraphic : MaskableGraphic
{
    private ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private Material particleMaterial;

    public override Texture mainTexture
    {
        get
        {
            if (particleMaterial != null && particleMaterial.mainTexture != null)
            {
                return particleMaterial.mainTexture;
            }

            return s_WhiteTexture;
        }
    }

    public void Initialize(ParticleSystem source, Material sourceMaterial)
    {
        particleSystem = source;
        particleMaterial = sourceMaterial;
        material = sourceMaterial;
        raycastTarget = false;
        particles = new ParticleSystem.Particle[Mathf.Max(1, source.main.maxParticles)];
        SetAllDirty();
    }

    private void LateUpdate()
    {
        if (particleSystem != null)
        {
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (particleSystem == null || particles == null)
        {
            return;
        }

        int particleCount = particleSystem.GetParticles(particles);
        ParticleSystem.MainModule main = particleSystem.main;

        for (int index = 0; index < particleCount; index++)
        {
            ParticleSystem.Particle particle = particles[index];
            Vector2 position = GetLocalPosition(particle.position, main);
            float halfSize = particle.GetCurrentSize(particleSystem) * 0.5f;
            float radians = -particle.rotation * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * halfSize;
            Vector2 up = new Vector2(-right.y, right.x);
            Color32 color = particle.GetCurrentColor(particleSystem);
            int vertexIndex = vertexHelper.currentVertCount;

            vertexHelper.AddVert(position - right - up, color, new Vector2(0f, 0f));
            vertexHelper.AddVert(position - right + up, color, new Vector2(0f, 1f));
            vertexHelper.AddVert(position + right + up, color, new Vector2(1f, 1f));
            vertexHelper.AddVert(position + right - up, color, new Vector2(1f, 0f));
            vertexHelper.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vertexHelper.AddTriangle(vertexIndex + 2, vertexIndex + 3, vertexIndex);
        }
    }

    private Vector3 GetLocalPosition(Vector3 particlePosition, ParticleSystem.MainModule main)
    {
        if (main.simulationSpace == ParticleSystemSimulationSpace.World)
        {
            return rectTransform.InverseTransformPoint(particlePosition);
        }

        if (main.simulationSpace == ParticleSystemSimulationSpace.Custom && main.customSimulationSpace != null)
        {
            Vector3 worldPosition = main.customSimulationSpace.TransformPoint(particlePosition);
            return rectTransform.InverseTransformPoint(worldPosition);
        }

        return particlePosition;
    }
}
