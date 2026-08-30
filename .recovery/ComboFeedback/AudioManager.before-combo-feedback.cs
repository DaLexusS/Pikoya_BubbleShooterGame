using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum SFX
{
    BubblePop,
    BubbleShot,
    BubbleSwap,
    BubbleHitWall,
    BubbleHitBubble,
    StarAppear,
    StarSet,
    VictoryStarReward,
    VictoryStarHit,
    ThreeStarWin,
    UiAppear,
    UiSwoosh,
    EndBubblesPop,
    Fireworks,
    UiHover,
    ExcellentAppear,
    Lose,
}

[RequireComponent(typeof(AudioPool))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private AudioMixerGroup sfxMixer;
    [SerializeField] private AudioPool sfxPool;

    [Header("Music")]
    [SerializeField] private AudioMixerGroup musicMixer;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.15f;
    [SerializeField, Min(0f)] private float musicFadeOutDuration = 0.5f;
    [SerializeField] private AudioSource musicSource;

    [Header("Sounds")]
    [SerializeField] private AudioClip sfxBubblePop;
    [SerializeField] private AudioClip sfxBubbleShot;
    [SerializeField] private AudioClip sfxBubbleSwap;
    [SerializeField] private AudioClip sfxBubbleHitWall;
    [SerializeField] private AudioClip sfxBubbleHitBubble;
    [SerializeField] private AudioClip sfxStarAppear;
    [SerializeField] private AudioClip sfxStarSet;
    [SerializeField] private AudioClip sfxVictoryStarReward;
    [SerializeField] private AudioClip sfxVictoryStarHit;
    [SerializeField] private AudioClip sfxThreeStarWin;
    [SerializeField] private AudioClip sfxUiAppear;
    [SerializeField] private AudioClip sfxUiSwoosh;
    [SerializeField] private AudioClip sfxEndBubblesPop;
    [SerializeField] private AudioClip sfxFireworks;
    [SerializeField] private AudioClip sfxUiHover;
    [SerializeField] private AudioClip sfxExcellentAppear;
    [SerializeField] private AudioClip sfxLose;

    private Coroutine musicFadeRoutine;

    public void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        sfxPool ??= GetComponent<AudioPool>();

        if (sfxPool == null)
        {
            Debug.LogError("AudioManager requires an AudioPool.");
            return;
        }

        sfxPool.Init(sfxMixer);
        StartBackgroundMusic();
    }

    private void StartBackgroundMusic()
    {
        if (gameMusic == null)
        {
            Debug.LogWarning("AudioManager has no background music assigned.");
            return;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.outputAudioMixerGroup = musicMixer != null ? musicMixer : sfxMixer;
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.pitch = 1f;
        musicSource.volume = Mathf.Clamp01(musicVolume);

        if (musicSource.clip != gameMusic)
        {
            musicSource.clip = gameMusic;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void FadeOutBackgroundMusic()
    {
        if (musicSource == null || !musicSource.isPlaying)
        {
            return;
        }

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
        }

        musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine());
    }

    public void RestoreBackgroundMusic()
    {
        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        StartBackgroundMusic();
    }

    private IEnumerator FadeOutMusicRoutine()
    {
        float startVolume = musicSource.volume;
        float duration = Mathf.Max(0f, musicFadeOutDuration);

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicFadeRoutine = null;
    }

    private void PlaySfx(float volume, AudioClip audio, bool hasRandomPitch, float randomPitchMin, float randomPitchMax)
    {
        if (audio == null || sfxPool == null)
        {
            Debug.LogWarning("AudioManager cannot play an unassigned sound.");
            return;
        }

        float currentPitch = hasRandomPitch
            ? GetRandomPitch(randomPitchMin, randomPitchMax)
            : randomPitchMin;

        sfxPool.PlaySound(volume, audio, currentPitch);
    }

    public void PlaySfx(float volume, SFX sfx, float fixedPitch)
    {
        PlaySfx(volume, sfx, false, fixedPitch, fixedPitch);
    }

    public void PlaySfx(float volume, SFX sfx, bool hasRandomPitch, float randomPitchMin, float randomPitchMax)
    {
        switch (sfx)
        {
            case SFX.BubblePop:
                PlaySfx(volume, sfxBubblePop, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.BubbleShot:
                PlaySfx(volume, sfxBubbleShot, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.BubbleSwap:
                PlaySfx(volume, sfxBubbleSwap, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.BubbleHitWall:
                PlaySfx(volume, sfxBubbleHitWall, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.BubbleHitBubble:
                PlaySfx(volume, sfxBubbleHitBubble, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.StarAppear:
                PlaySfx(volume, sfxStarAppear, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.StarSet:
                PlaySfx(volume, sfxStarSet, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.VictoryStarReward:
                PlaySfx(volume, sfxVictoryStarReward, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.VictoryStarHit:
                PlaySfx(volume, sfxVictoryStarHit, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.ThreeStarWin:
                PlaySfx(volume, sfxThreeStarWin, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.UiAppear:
                PlaySfx(volume, sfxUiAppear, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.UiSwoosh:
                PlaySfx(volume, sfxUiSwoosh, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.EndBubblesPop:
                PlaySfx(volume, sfxEndBubblesPop, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.Fireworks:
                PlaySfx(volume, sfxFireworks, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.UiHover:
                PlaySfx(volume, sfxUiHover, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.ExcellentAppear:
                PlaySfx(volume, sfxExcellentAppear, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
            case SFX.Lose:
                PlaySfx(volume, sfxLose, hasRandomPitch, randomPitchMin, randomPitchMax);
                break;
        }
    }

    public float GetRandomPitch(float min, float max)
    {
        return Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
