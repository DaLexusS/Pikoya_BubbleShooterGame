using System.Collections;
using UnityEngine;

public sealed class BombExplosionFeedback : MonoBehaviour
{
    private const string ResourceName = "BombExplosionFeedback";

    [Header("Camera Shake")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.35f;
    [SerializeField, Min(0f)] private float shakeStrength = 0.16f;
    [SerializeField, Min(0.01f)] private float shakeFrequency = 32f;

    [Header("Explosion")]
    [SerializeField] private ParticleSystem explosionParticlesPrefab;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.2f;
    [SerializeField] private float soundPitch = 0.9f;

    private static BombExplosionFeedback instance;
    private Coroutine shakeRoutine;

    public static void PlayAt(Vector3 worldPosition)
    {
        if (instance == null)
        {
            BombExplosionFeedback prefab = Resources.Load<BombExplosionFeedback>(ResourceName);
            if (prefab == null)
            {
                Debug.LogWarning("BombExplosionFeedback prefab is missing from Resources.");
                return;
            }

            instance = Instantiate(prefab);
        }

        instance.Play(worldPosition);
    }

    private void Play(Vector3 worldPosition)
    {
        AudioManager.Instance?.PlaySfx(soundVolume, SFX.BombExplosion, soundPitch);

        if (explosionParticlesPrefab != null)
        {
            ParticleSystem particles = Instantiate(
                explosionParticlesPrefab,
                worldPosition,
                Quaternion.identity);
            particles.Play();
            ParticleSystem.MainModule main = particles.main;
            Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
        }

        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            return;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeCamera(gameplayCamera.transform));
    }

    private IEnumerator ShakeCamera(Transform cameraTransform)
    {
        Vector3 startingPosition = cameraTransform.position;
        float duration = Mathf.Max(0.01f, shakeDuration);
        float elapsed = 0f;
        float seed = Random.value * 100f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float strength = shakeStrength * (1f - progress);
            float sample = elapsed * shakeFrequency;
            float x = Mathf.PerlinNoise(seed, sample) * 2f - 1f;
            float y = Mathf.PerlinNoise(seed + 20f, sample) * 2f - 1f;
            cameraTransform.position = startingPosition + new Vector3(x, y, 0f) * strength;
            yield return null;
        }

        cameraTransform.position = startingPosition;
        shakeRoutine = null;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
