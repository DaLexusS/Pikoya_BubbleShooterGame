using UnityEngine;

public sealed class BubbleView : MonoBehaviour
{
    private static readonly int TextureColor = Shader.PropertyToID("_TextureColor");

    [SerializeField] private SpriteRenderer bubbleVisual;
    [SerializeField] private SpriteRenderer strokeVisual;
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
    public Transform BubbleVisualTransform => bubbleVisual.transform;
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
