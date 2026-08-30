using UnityEngine;

public sealed class BubbleView : MonoBehaviour
{
    private static readonly int TextureColor = Shader.PropertyToID("_TextureColor");

    [SerializeField] private SpriteRenderer bubbleVisual;
    [SerializeField] private SpriteRenderer strokeVisual;
    [SerializeField] private SpriteRenderer bombVisual;
    [ColorUsage(false)]
    [SerializeField] private Color redColor = Color.red;
    [ColorUsage(false)]
    [SerializeField] private Color blueColor = Color.blue;
    [ColorUsage(false)]
    [SerializeField] private Color greenColor = Color.green;
    [ColorUsage(false)]
    [SerializeField] private Color yellowColor = Color.yellow;

    private MaterialPropertyBlock propertyBlock;

    public BubbleColor BubbleColor { get; private set; }
    public bool WasPlayerShot { get; private set; }
    public bool IsBomb { get; private set; }
    public Transform BubbleVisualTransform => IsBomb && bombVisual != null
        ? bombVisual.transform
        : bubbleVisual.transform;
    public Transform StrokeVisualTransform => strokeVisual.transform;

    public Color DisplayColor => GetDisplayColor(BubbleColor);
    public void SetColor(BubbleColor bubbleColor)
    {
        BubbleColor = bubbleColor;
        propertyBlock ??= new MaterialPropertyBlock();
        bubbleVisual.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TextureColor, GetDisplayColor(bubbleColor));
        bubbleVisual.SetPropertyBlock(propertyBlock);
    }

    public void MarkAsPlayerShot()
    {
        WasPlayerShot = true;
    }

    public void SetBomb(bool isBomb)
    {
        IsBomb = isBomb;
        bubbleVisual.gameObject.SetActive(!isBomb);
        strokeVisual.gameObject.SetActive(!isBomb);

        if (bombVisual != null)
        {
            bombVisual.gameObject.SetActive(isBomb);
            bombVisual.enabled = isBomb;
        }
    }

    public void PrepareForCelebration()
    {
        SetBomb(IsBomb);
        bubbleVisual.enabled = !IsBomb;
        strokeVisual.enabled = !IsBomb;
        SetOpacity(1f);
    }

    public void SetOpacity(float opacity)
    {
        Color bubbleColor = DisplayColor;
        bubbleColor.a = Mathf.Clamp01(opacity);
        propertyBlock ??= new MaterialPropertyBlock();
        bubbleVisual.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TextureColor, bubbleColor);
        bubbleVisual.SetPropertyBlock(propertyBlock);
        Color strokeColor = strokeVisual.color;
        strokeColor.a = bubbleColor.a;
        strokeVisual.color = strokeColor;

        if (bombVisual != null)
        {
            Color bombColor = bombVisual.color;
            bombColor.a = bubbleColor.a;
            bombVisual.color = bombColor;
        }
    }

    private Color GetDisplayColor(BubbleColor bubbleColor)
    {
        return bubbleColor switch
        {
            BubbleColor.Red => redColor,
            BubbleColor.Blue => blueColor,
            BubbleColor.Green => greenColor,
            BubbleColor.Yellow => yellowColor,
            _ => Color.white
        };
    }
}
