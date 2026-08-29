using TMPro;
using UnityEngine;

public class PlayerBubbleCountView : MonoBehaviour
{
    [SerializeField] private TMP_Text bubbleCountText;

    private PlayerShooter playerShooter;

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
        bubbleCountText.text = bubbleCount.ToString();
    }

    private void OnDestroy()
    {
        if (playerShooter != null)
        {
            playerShooter.RemainingShotsChanged -= SetBubbleCount;
        }
    }
}
