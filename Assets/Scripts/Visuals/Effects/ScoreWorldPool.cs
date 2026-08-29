using System.Collections.Generic;
using UnityEngine;

public class ScoreWorldPool : MonoBehaviour
{
    [SerializeField] private ScoreWorldEffect scorePrefab;
    [SerializeField] private int initialSize = 20;
    [SerializeField] private int maximumSize = 100;

    private readonly List<ScoreWorldEffect> effects = new List<ScoreWorldEffect>();
    private int reuseIndex;
    private bool isInitialized;

    public static ScoreWorldPool Active { get; private set; }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        Active = this;

        for (int index = 0; index < initialSize; index++)
        {
            CreateEffect();
        }

        isInitialized = true;
    }

    public void Play(Vector3 position, int scoreValue)
    {
        ScoreWorldEffect effect = GetAvailableEffect();
        effect.transform.position = position;
        effect.gameObject.SetActive(true);
        effect.Play(scoreValue, Release);
    }

    private ScoreWorldEffect GetAvailableEffect()
    {
        foreach (ScoreWorldEffect effect in effects)
        {
            if (!effect.gameObject.activeSelf)
            {
                return effect;
            }
        }

        if (effects.Count < maximumSize)
        {
            return CreateEffect();
        }

        ScoreWorldEffect reusedEffect = effects[reuseIndex];
        reuseIndex = (reuseIndex + 1) % effects.Count;
        return reusedEffect;
    }

    private ScoreWorldEffect CreateEffect()
    {
        ScoreWorldEffect effect = Instantiate(scorePrefab, transform);
        effect.gameObject.SetActive(false);
        effects.Add(effect);
        return effect;
    }

    private void Release(ScoreWorldEffect effect)
    {
        effect.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }
}
