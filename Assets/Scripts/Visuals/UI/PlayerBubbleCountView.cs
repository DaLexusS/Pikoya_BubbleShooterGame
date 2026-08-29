using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerBubbleCountView : MonoBehaviour
{
    [SerializeField] private TMP_Text bubbleCountText;

    [Header("Low Bubbles Warning")]
    [SerializeField] private int warningThreshold = 5;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.03f, 0.03f, 1f);
    [SerializeField] private float blinkCycleDuration = 1.5f;

    private PlayerShooter playerShooter;
    private Coroutine warningAnimation;

    public static PlayerBubbleCountView FindInScene()
    {
        PlayerBubbleCountView view = FindFirstObjectByType<PlayerBubbleCountView>();

        if (view != null)
        {
            return view;
        }

        GameObject playerBubbles = GameObject.Find("PlayerBubbles");
        return playerBubbles == null ? null : playerBubbles.AddComponent<PlayerBubbleCountView>();
    }

    public void Initialize(PlayerShooter shooter)
    {
        if (bubbleCountText == null)
        {
            bubbleCountText = GetComponentInChildren<TMP_Text>(true);
        }

        if (playerShooter != null)
        {
            playerShooter.RemainingShotsChanged -= SetBubbleCount;
        }

        playerShooter = shooter;
        playerShooter.RemainingShotsChanged += SetBubbleCount;
        SetBubbleCount(playerShooter.RemainingShots);
    }

    private void SetBubbleCount(int bubbleCount)
    {
        if (bubbleCountText == null)
        {
            return;
        }

        bubbleCountText.text = bubbleCount.ToString();

        if (bubbleCount == 0)
        {
            StopWarning();
            bubbleCountText.color = warningColor;
            return;
        }

        if (bubbleCount > 0 && bubbleCount <= warningThreshold)
        {
            if (warningAnimation == null)
            {
                warningAnimation = StartCoroutine(BlinkWarning());
            }

            return;
        }

        StopWarning();
    }

    private IEnumerator BlinkWarning()
    {
        float elapsed = 0f;

        while (true)
        {
            float duration = Mathf.Max(0.1f, blinkCycleDuration);
            float progress = Mathf.PingPong(elapsed * 2f / duration, 1f);
            float smooth = progress * progress * (3f - 2f * progress);
            bubbleCountText.color = Color.Lerp(normalColor, warningColor, smooth);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void StopWarning()
    {
        if (warningAnimation != null)
        {
            StopCoroutine(warningAnimation);
            warningAnimation = null;
        }

        if (bubbleCountText != null)
        {
            bubbleCountText.color = normalColor;
        }
    }

    private void OnDisable()
    {
        StopWarning();
    }

    private void OnDestroy()
    {
        StopWarning();

        if (playerShooter != null)
        {
            playerShooter.RemainingShotsChanged -= SetBubbleCount;
        }
    }
}
