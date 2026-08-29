using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPool : MonoBehaviour
{
    [SerializeField] private int poolSize = 30;

    private readonly List<AudioSource> audioSources = new List<AudioSource>();
    private AudioMixerGroup mixerGroup;
    private bool initialized;

    public void Init(AudioMixerGroup sfxMixer)
    {
        if (initialized)
        {
            return;
        }

        mixerGroup = sfxMixer;

        for (int i = 0; i < Mathf.Max(1, poolSize); i++)
        {
            audioSources.Add(CreateSource());
        }

        initialized = true;
    }

    public void PlaySound(float volume, AudioClip audio, float pitch)
    {
        if (!initialized || audio == null)
        {
            return;
        }

        AudioSource availableSource = audioSources.Find(source => !source.isPlaying);

        if (availableSource == null)
        {
            availableSource = CreateSource();
            audioSources.Add(availableSource);
        }

        availableSource.pitch = Mathf.Clamp(pitch, -3f, 3f);
        availableSource.volume = Mathf.Clamp01(volume);
        availableSource.clip = audio;
        availableSource.Play();
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }
}
