using UnityEngine;

public sealed class BubbleView : MonoBehaviour
{
    private static readonly int TextureColor = Shader.PropertyToID("_TextureColor");

    [SerializeField] private SpriteRenderer bubbleVisual;

    private MaterialPropertyBlock propertyBlock;

    public BubbleColor BubbleColor { get; private set; }

    public void SetColor(BubbleColor bubbleColor)
    {
        BubbleColor = bubbleColor;
        propertyBlock ??= new MaterialPropertyBlock();
        bubbleVisual.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TextureColor, GetDisplayColor(bubbleColor));
        bubbleVisual.SetPropertyBlock(propertyBlock);
    }

    private static Color GetDisplayColor(BubbleColor bubbleColor)
    {
        return bubbleColor switch
        {
            BubbleColor.Red => Color.red,
            BubbleColor.Blue => Color.blue,
            BubbleColor.Green => Color.green,
            BubbleColor.Yellow => Color.yellow,
            _ => Color.white
        };
    }
}
