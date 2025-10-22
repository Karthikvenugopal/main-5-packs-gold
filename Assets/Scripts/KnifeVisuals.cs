using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class KnifeVisuals : MonoBehaviour
{
    private static Sprite cachedSprite;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        CacheRenderer();
        ApplySprite();
    }

    private void OnEnable()
    {
        ApplySprite();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRenderer();
        ApplySprite();
    }
#endif

    private void CacheRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void ApplySprite()
    {
        if (spriteRenderer == null) return;

        Sprite sprite = GetOrCreateSprite();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
    }

    private static Sprite GetOrCreateSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        const int width = 48;
        const int height = 128;

        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "KnifeTexture"
        };

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparent;
        }

        int handleHeight = height / 5;
        int guardHeight = Mathf.CeilToInt(handleHeight * 0.2f);
        int bladeStart = handleHeight + guardHeight;

        Color handleDark = new Color(0.2f, 0.16f, 0.14f);
        Color handleLight = new Color(0.32f, 0.24f, 0.2f);
        Color guardColor = new Color(0.6f, 0.6f, 0.65f);
        Color bladeEdge = new Color(0.92f, 0.95f, 0.98f);
        Color bladeMid = new Color(0.78f, 0.82f, 0.9f);
        Color bladeShadow = new Color(0.55f, 0.6f, 0.7f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                float center = width * 0.5f;

                if (y < handleHeight)
                {
                    float handleHalfWidth = width * 0.32f;
                    if (Mathf.Abs(x - center) > handleHalfWidth) continue;

                    float t = Mathf.InverseLerp(handleHeight, 0, y);
                    Color tone = Color.Lerp(handleDark, handleLight, t * 0.6f);
                    pixels[index] = tone;
                }
                else if (y < bladeStart)
                {
                    float guardHalfWidth = width * 0.45f;
                    if (Mathf.Abs(x - center) > guardHalfWidth) continue;
                    pixels[index] = guardColor;
                }
                else
                {
                    float bladeProgress = Mathf.InverseLerp(bladeStart, height - 1, y);
                    float halfWidth = Mathf.Lerp(width * 0.45f, width * 0.1f, bladeProgress);
                    float distance = Mathf.Abs(x - center);

                    if (distance > halfWidth) continue;

                    float edgeBlend = Mathf.InverseLerp(halfWidth, 0f, distance);
                    Color baseColor = Color.Lerp(bladeShadow, bladeMid, edgeBlend);
                    baseColor = Color.Lerp(baseColor, bladeEdge, Mathf.Pow(edgeBlend, 2f));

                    float sheen = Mathf.Clamp01(Mathf.Sin((bladeProgress * Mathf.PI) + (distance / width * Mathf.PI)));
                    baseColor = Color.Lerp(baseColor, bladeEdge, sheen * 0.15f);
                    pixels[index] = baseColor;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        cachedSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.05f), 64f);
        return cachedSprite;
    }
}
