using System.Collections.Generic;
using UnityEngine;

public class FireworkPool : MonoBehaviour
{
    private class FireworkItem
    {
        public GameObject GameObject;
        public ParticleSystem[] Particles;
    }

    [SerializeField] private GameObject fireworkPrefab;
    [SerializeField] private int capacity = 10;

    private readonly List<FireworkItem> fireworks = new List<FireworkItem>();
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        for (int index = 0; index < capacity; index++)
        {
            GameObject firework = Instantiate(fireworkPrefab, transform);
            ParticleSystem[] particles = firework.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            firework.SetActive(false);
            fireworks.Add(new FireworkItem
            {
                GameObject = firework,
                Particles = particles
            });
        }

        isInitialized = true;
    }

    public void Play(Vector3 position)
    {
        FireworkItem firework = GetAvailableFirework();

        if (firework == null)
        {
            return;
        }

        firework.GameObject.transform.position = position;
        firework.GameObject.SetActive(true);

        foreach (ParticleSystem particle in firework.Particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(false);
        }
    }

    private FireworkItem GetAvailableFirework()
    {
        foreach (FireworkItem firework in fireworks)
        {
            if (!firework.GameObject.activeSelf)
            {
                return firework;
            }
        }

        return null;
    }

    private void LateUpdate()
    {
        foreach (FireworkItem firework in fireworks)
        {
            if (!firework.GameObject.activeSelf || IsAlive(firework))
            {
                continue;
            }

            firework.GameObject.SetActive(false);
        }
    }

    private bool IsAlive(FireworkItem firework)
    {
        foreach (ParticleSystem particle in firework.Particles)
        {
            if (particle.IsAlive(false))
            {
                return true;
            }
        }

        return false;
    }
}
