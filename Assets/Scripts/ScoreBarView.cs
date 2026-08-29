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
        if (starRewardPrefab == null)
        {
            RevealStar(index);
            return;
        }

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

        StarRewardEffect reward = Instantiate(starRewardPrefab, canvasRect);
        reward.Play(sourcePosition, targetPosition, () => RevealStar(index));
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
