using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class ComboFeedbackView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Bubble Thresholds")]
    [SerializeField] private int coolThreshold = 6;
    [SerializeField] private int epicThreshold = 7;
    [SerializeField] private int amazingThreshold = 10;

    [Header("Words")]
    [SerializeField] private string coolText = "COOL";
    [SerializeField] private string epicText = "EPIC";
    [SerializeField] private string amazingText = "AMAZING!";

    [Header("Entrance")]
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float overshootScale = 1.1f;
    [SerializeField] private float appearDuration = 0.18f;
    [SerializeField] private float settleDuration = 0.1f;

    [Header("Letter Float")]
    [SerializeField] private float floatDistance = 8f;
    [SerializeField] private float floatFrequency = 6f;
    [SerializeField] private float letterPhaseOffset = 0.75f;
    [SerializeField] private float letterDelay = 0.035f;
    [SerializeField] private float holdDuration = 0.65f;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Audio")]
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.1f;
    [SerializeField] private float soundPitch = 1f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine animation;
    private bool initialized;

    public static ComboFeedbackView FindInScene()
    {
        return FindAnyObjectByType<ComboFeedbackView>(FindObjectsInactive.Include);
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

        feedbackText ??= GetComponent<TMP_Text>();
        canvasGroup ??= GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = transform as RectTransform;
        originalScale = rectTransform.localScale;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        initialized = true;
        gameObject.SetActive(false);
    }

    public void Play(int clearedBubbleCount)
    {
        if (!TryGetFeedback(clearedBubbleCount, out string message, out SFX sound))
        {
            return;
        }

        Initialize();
        gameObject.SetActive(true);

        if (animation != null)
        {
            StopCoroutine(animation);
        }

        if (AudioManager.Instance != null && AudioManager.Instance.HasSfx(sound))
        {
            AudioManager.Instance.PlaySfx(soundVolume, sound, soundPitch);
        }

        animation = StartCoroutine(PlaySequence(message));
    }

    private bool TryGetFeedback(int count, out string message, out SFX sound)
    {
        if (count >= amazingThreshold)
        {
            message = amazingText;
            sound = SFX.ComboAmazing;
            return true;
        }

        if (count >= epicThreshold)
        {
            message = epicText;
            sound = SFX.ComboEpic;
            return true;
        }

        if (count >= coolThreshold)
        {
            message = coolText;
            sound = SFX.ComboCool;
            return true;
        }

        message = string.Empty;
        sound = default;
        return false;
    }

    private IEnumerator PlaySequence(string message)
    {
        feedbackText.text = message;
        feedbackText.ForceMeshUpdate();
        TMP_TextInfo textInfo = feedbackText.textInfo;
        TMP_MeshInfo[] originalMesh = textInfo.CopyMeshInfoVertexData();

        canvasGroup.alpha = 1f;
        rectTransform.localScale = originalScale * startScale;
        float entranceEnd = Mathf.Max(0f, appearDuration + settleDuration);
        float fadeStart = entranceEnd + Mathf.Max(0f, holdDuration);
        float totalDuration = fadeStart + Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            AnimatePanelScale(elapsed);
            AnimateLetters(textInfo, originalMesh, elapsed);

            if (elapsed > fadeStart)
            {
                canvasGroup.alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / Mathf.Max(0.01f, fadeDuration));
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = originalScale;
        animation = null;
        gameObject.SetActive(false);
    }

    private void AnimatePanelScale(float elapsed)
    {
        if (elapsed < appearDuration)
        {
            float progress = Smooth(elapsed / Mathf.Max(0.01f, appearDuration));
            rectTransform.localScale = originalScale * Mathf.LerpUnclamped(startScale, overshootScale, progress);
            return;
        }

        if (elapsed < appearDuration + settleDuration)
        {
            float progress = Smooth((elapsed - appearDuration) / Mathf.Max(0.01f, settleDuration));
            rectTransform.localScale = originalScale * Mathf.LerpUnclamped(overshootScale, 1f, progress);
            return;
        }

        rectTransform.localScale = originalScale;
    }

    private void AnimateLetters(TMP_TextInfo textInfo, TMP_MeshInfo[] originalMesh, float elapsed)
    {
        for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
        {
            Vector3[] source = originalMesh[meshIndex].vertices;
            Vector3[] destination = textInfo.meshInfo[meshIndex].vertices;
            System.Array.Copy(source, destination, source.Length);
        }

        for (int index = 0; index < textInfo.characterCount; index++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[index];
            if (!character.isVisible)
            {
                continue;
            }

            float characterTime = Mathf.Max(0f, elapsed - index * letterDelay);
            float offset = Mathf.Sin(characterTime * floatFrequency + index * letterPhaseOffset) * floatDistance;
            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            for (int corner = 0; corner < 4; corner++)
            {
                vertices[vertexIndex + corner] += Vector3.up * offset;
            }
        }

        for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[meshIndex];
            meshInfo.mesh.vertices = meshInfo.vertices;
            feedbackText.UpdateGeometry(meshInfo.mesh, meshIndex);
        }
    }

    private static float Smooth(float value)
    {
        float progress = Mathf.Clamp01(value);
        return progress * progress * (3f - 2f * progress);
    }
}
