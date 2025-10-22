using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class RollingPinVisuals : MonoBehaviour
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

        const int width = 96;
        const int height = 32;

        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "RollingPinTexture"
        };

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparent;
        }

        int handleWidth = width / 6;
        int bodyStart = handleWidth + 4;
        int bodyEnd = width - bodyStart;
        float halfHeight = height / 2f;
        float radius = halfHeight - 2f;

        Color bodyBase = new Color(0.76f, 0.61f, 0.4f);
        Color bodyHighlight = new Color(0.93f, 0.8f, 0.56f);
        Color bodyShadow = new Color(0.45f, 0.32f, 0.2f);
        Color handleBase = new Color(0.53f, 0.38f, 0.24f);
        Color handleHighlight = new Color(0.68f, 0.52f, 0.33f);

        for (int y = 0; y < height; y++)
        {
            float dy = (y - halfHeight + 0.5f) / radius;
            float strip = 1f - Mathf.Clamp01(Mathf.Abs(dy));

            for (int x = 0; x < width; x++)
            {
                if (Mathf.Abs(dy) > 1f) continue;

                bool isHandle = x < bodyStart - 4 || x >= bodyEnd + 4;
                bool isInsideBody = x >= bodyStart && x < bodyEnd;

                if (!isHandle && !isInsideBody) continue;

                float highlight = Mathf.Clamp01(0.2f + strip * 0.8f);
                Color baseColor = isHandle ? handleBase : bodyBase;
                Color targetHighlight = isHandle ? handleHighlight : bodyHighlight;
                Color targetShadow = isHandle ? handleBase * 0.75f : bodyShadow;

                float blend = Mathf.Lerp(0.15f, 0.8f, highlight);
                Color pixelColor = Color.Lerp(targetShadow, targetHighlight, blend);
                pixels[(y * width) + x] = pixelColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        cachedSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        return cachedSprite;
    }
}
