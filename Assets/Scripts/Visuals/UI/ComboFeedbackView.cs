using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ComboFeedbackView : MonoBehaviour
{
    [Header("Word Groups")]
    [SerializeField] private RectTransform coolGroup;
    [SerializeField] private RectTransform epicGroup;
    [SerializeField] private RectTransform amazingGroup;

    [Header("Bubble Thresholds")]
    [SerializeField] private int coolThreshold = 6;
    [SerializeField] private int epicThreshold = 7;
    [SerializeField] private int amazingThreshold = 10;
    [SerializeField] private bool randomizeWord = true;

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

    [Header("Position")]
    [SerializeField] private Vector2 impactOffset;

    [Header("Audio")]
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.1f;
    [SerializeField] private float startingPitch = 1f;
    [SerializeField] private float pitchStep = 0.1f;

    private RectTransform[] groups;
    private Vector3[] groupScales;
    private RectTransform activeGroup;
    private CanvasGroup activeCanvasGroup;
    private TMP_Text[] activeTexts;
    private TMP_MeshInfo[][] originalMeshes;
    private Vector3 activeGroupScale;
    private Coroutine animation;
    private bool initialized;
    private float worldZ;

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

        worldZ = transform.position.z;
        coolGroup ??= FindGroup("Cool");
        epicGroup ??= FindGroup("Epic");
        amazingGroup ??= FindGroup("Amazing");
        groups = new[] { coolGroup, epicGroup, amazingGroup };
        groupScales = new Vector3[groups.Length];

        for (int index = 0; index < groups.Length; index++)
        {
            if (groups[index] == null)
            {
                continue;
            }

            groupScales[index] = groups[index].localScale;
            CanvasGroup groupCanvas = groups[index].GetComponent<CanvasGroup>();
            if (groupCanvas == null)
            {
                groupCanvas = groups[index].gameObject.AddComponent<CanvasGroup>();
            }

            groupCanvas.alpha = 0f;
            groupCanvas.interactable = false;
            groupCanvas.blocksRaycasts = false;
            groups[index].gameObject.SetActive(false);
        }

        initialized = true;
        gameObject.SetActive(false);
    }

    public void Play(int clearedBubbleCount, Vector2 impactPosition)
    {
        Initialize();

        if (!TrySelectGroup(clearedBubbleCount, out int groupIndex))
        {
            return;
        }

        if (animation != null)
        {
            StopCoroutine(animation);
            ResetActiveGroup();
        }

        activeGroup = groups[groupIndex];
        if (activeGroup == null)
        {
            return;
        }

        activeGroupScale = groupScales[groupIndex];
        transform.position = new Vector3(
            impactPosition.x + impactOffset.x,
            impactPosition.y + impactOffset.y,
            worldZ);
        gameObject.SetActive(true);
        activeGroup.gameObject.SetActive(true);
        activeCanvasGroup = activeGroup.GetComponent<CanvasGroup>();
        activeCanvasGroup.alpha = 1f;
        activeTexts = activeGroup.GetComponentsInChildren<TMP_Text>(true);
        originalMeshes = new TMP_MeshInfo[activeTexts.Length][];

        for (int index = 0; index < activeTexts.Length; index++)
        {
            activeTexts[index].ForceMeshUpdate();
            originalMeshes[index] = activeTexts[index].textInfo.CopyMeshInfoVertexData();
        }

        if (AudioManager.Instance != null && AudioManager.Instance.HasSfx(SFX.ComboWord))
        {
            float pitch = startingPitch + groupIndex * pitchStep;
            AudioManager.Instance.PlaySfx(soundVolume, SFX.ComboWord, pitch);
        }

        animation = StartCoroutine(PlaySequence());
    }

    private RectTransform FindGroup(string groupName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == groupName)
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private bool TrySelectGroup(int count, out int groupIndex)
    {
        if (count < coolThreshold)
        {
            groupIndex = -1;
            return false;
        }

        if (randomizeWord)
        {
            groupIndex = Random.Range(0, groups.Length);
            return true;
        }

        if (count >= amazingThreshold)
        {
            groupIndex = 2;
            return true;
        }

        if (count >= epicThreshold)
        {
            groupIndex = 1;
            return true;
        }

        groupIndex = 0;
        return true;
    }

    private IEnumerator PlaySequence()
    {
        activeGroup.localScale = activeGroupScale * startScale;
        float entranceEnd = Mathf.Max(0f, appearDuration + settleDuration);
        float fadeStart = entranceEnd + Mathf.Max(0f, holdDuration);
        float totalDuration = fadeStart + Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            AnimateGroupScale(elapsed);

            for (int index = 0; index < activeTexts.Length; index++)
            {
                AnimateLetters(activeTexts[index], originalMeshes[index], elapsed);
            }

            if (elapsed > fadeStart)
            {
                activeCanvasGroup.alpha = 1f - Mathf.Clamp01(
                    (elapsed - fadeStart) / Mathf.Max(0.01f, fadeDuration));
            }

            yield return null;
        }

        animation = null;
        ResetActiveGroup();
        gameObject.SetActive(false);
    }

    private void AnimateGroupScale(float elapsed)
    {
        if (elapsed < appearDuration)
        {
            float progress = Smooth(elapsed / Mathf.Max(0.01f, appearDuration));
            activeGroup.localScale = activeGroupScale * Mathf.LerpUnclamped(
                startScale,
                overshootScale,
                progress);
            return;
        }

        if (elapsed < appearDuration + settleDuration)
        {
            float progress = Smooth(
                (elapsed - appearDuration) / Mathf.Max(0.01f, settleDuration));
            activeGroup.localScale = activeGroupScale * Mathf.LerpUnclamped(
                overshootScale,
                1f,
                progress);
            return;
        }

        activeGroup.localScale = activeGroupScale;
    }

    private void AnimateLetters(TMP_Text text, TMP_MeshInfo[] originalMesh, float elapsed)
    {
        TMP_TextInfo textInfo = text.textInfo;

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
            float offset = Mathf.Sin(
                characterTime * floatFrequency + index * letterPhaseOffset) * floatDistance;
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
            text.UpdateGeometry(meshInfo.mesh, meshIndex);
        }
    }

    private void ResetActiveGroup()
    {
        if (activeGroup == null)
        {
            return;
        }

        activeGroup.localScale = activeGroupScale;
        if (activeCanvasGroup != null)
        {
            activeCanvasGroup.alpha = 0f;
        }

        activeGroup.gameObject.SetActive(false);
        activeGroup = null;
        activeCanvasGroup = null;
        activeTexts = null;
        originalMeshes = null;
    }

    private static float Smooth(float value)
    {
        float progress = Mathf.Clamp01(value);
        return progress * progress * (3f - 2f * progress);
    }
}
