using System.Collections;
using UnityEngine;

public class BubbleFall : MonoBehaviour
{
    [SerializeField] private float minimumHorizontalSpeed = 0.7f;
    [SerializeField] private float maximumHorizontalSpeed = 1.5f;
    [SerializeField] private float minimumUpwardSpeed = 1.2f;
    [SerializeField] private float maximumUpwardSpeed = 1.8f;
    [SerializeField] private float gravity = 8f;
    [SerializeField] private float maximumRotationSpeed = 180f;

    private Vector2 velocity;
    private float rotationSpeed;
    private float destroyY;
    private float collectionY;
    private System.Action collected;
    private bool hasCollected;

    public void Play(float targetY, System.Action onCollected)
    {
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        collectionY = targetY;
        collected = onCollected;
        hasCollected = false;
        float horizontalSpeed = Random.Range(minimumHorizontalSpeed, maximumHorizontalSpeed);
        velocity = new Vector2(
            horizontalDirection * horizontalSpeed,
            Random.Range(minimumUpwardSpeed, maximumUpwardSpeed));
        rotationSpeed = Random.Range(-maximumRotationSpeed, maximumRotationSpeed);
        CircleCollider2D bubbleCollider = GetComponent<CircleCollider2D>();

        if (bubbleCollider != null)
        {
            bubbleCollider.enabled = false;
        }

        transform.SetParent(null, true);
        Camera gameplayCamera = Camera.main;
        destroyY = gameplayCamera == null
            ? transform.position.y - 12f
            : gameplayCamera.ViewportToWorldPoint(new Vector3(0f, -0.2f, 0f)).y;
        enabled = true;
    }

    private void Update()
    {
        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (transform.position.y <= collectionY)
        {
            Collect();
            return;
        }

        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    private void Collect()
    {
        if (hasCollected)
        {
            return;
        }

        hasCollected = true;
        enabled = false;
        System.Action callback = collected;
        collected = null;
        callback?.Invoke();
        BubblePopEffect popEffect = GetComponent<BubblePopEffect>();

        if (popEffect == null)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(PlayCollectionEffect(popEffect));
    }

    private IEnumerator PlayCollectionEffect(BubblePopEffect popEffect)
    {
        yield return popEffect.Play();
        Destroy(gameObject);
    }
}
