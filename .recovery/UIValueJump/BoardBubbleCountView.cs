using TMPro;
using UnityEngine;

public class BoardBubbleCountView : MonoBehaviour
{
    [SerializeField] private TMP_Text bubbleCountText;
    [SerializeField] private GameObject checkmark;

    private MapLoader board;

    public static BoardBubbleCountView FindInScene()
    {
        BoardBubbleCountView view = FindFirstObjectByType<BoardBubbleCountView>();

        if (view != null)
        {
            return view;
        }

        GameObject bubblesAmount = GameObject.Find("BubblesAmount");
        return bubblesAmount == null ? null : bubblesAmount.AddComponent<BoardBubbleCountView>();
    }

    public void Initialize(MapLoader mapLoader)
    {
        if (bubbleCountText == null)
        {
            bubbleCountText = GetComponentInChildren<TMP_Text>(true);
        }

        if (checkmark == null)
        {
            Transform checkmarkTransform = transform.Find("Checkmark");
            checkmark = checkmarkTransform == null ? null : checkmarkTransform.gameObject;
        }

        if (board != null)
        {
            board.BubbleCountChanged -= SetBubbleCount;
        }

        board = mapLoader;
        board.BubbleCountChanged += SetBubbleCount;
        SetBubbleCount(board.BubbleCount, board.IsEmpty);
    }

    private void SetBubbleCount(int bubbleCount, bool isFinished)
    {
        bubbleCountText.gameObject.SetActive(!isFinished);
        checkmark.SetActive(isFinished);

        if (!isFinished)
        {
            bubbleCountText.text = bubbleCount.ToString();
        }
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            board.BubbleCountChanged -= SetBubbleCount;
        }
    }
}
