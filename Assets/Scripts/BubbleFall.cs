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

    public void Play()
    {
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
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

        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
