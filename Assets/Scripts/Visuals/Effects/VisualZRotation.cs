using UnityEngine;

public sealed class VisualZRotation : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 12f;
    [SerializeField] private bool useUnscaledTime;

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(0f, 0f, degreesPerSecond * deltaTime, Space.Self);
    }
}
