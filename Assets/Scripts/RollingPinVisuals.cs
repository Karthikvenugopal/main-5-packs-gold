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
        const float handleRadiusScale = 0.55f;
        const int handleLength = width / 8;
        const int transitionLength = 6;
        const int handleExtent = handleLength + transitionLength;
        int effectiveTransition = Mathf.Max(1, transitionLength);

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

        float halfHeight = height / 2f;
        float radius = halfHeight - 2f;

        Color bodyBase = new Color(0.76f, 0.61f, 0.4f);
        Color bodyHighlight = new Color(0.93f, 0.8f, 0.56f);
        Color bodyShadow = new Color(0.45f, 0.32f, 0.2f);
        Color handleBase = new Color(0.53f, 0.38f, 0.24f);
        Color handleHighlight = new Color(0.68f, 0.52f, 0.33f);
        Color handleShadow = handleBase * 0.6f;

        for (int y = 0; y < height; y++)
        {
            float yOffset = y - halfHeight + 0.5f;

            for (int x = 0; x < width; x++)
            {
                int distanceFromLeft = x;
                int distanceFromRight = (width - 1) - x;
                int distanceFromEdge = Mathf.Min(distanceFromLeft, distanceFromRight);

                float handleStrength = 0f;
                if (distanceFromEdge <= handleExtent)
                {
                    float transitionT = (distanceFromEdge - handleLength) / (float)effectiveTransition;
                    handleStrength = 1f - Mathf.Clamp01(transitionT);
                }

                float localRadius = radius * Mathf.Lerp(1f, handleRadiusScale, handleStrength);
                if (localRadius <= 0f) continue;

                float normalizedY = Mathf.Abs(yOffset) / localRadius;
                if (normalizedY > 1f) continue;

                float strip = 1f - Mathf.Clamp01(normalizedY);
                float blend = Mathf.Lerp(0.2f, 0.9f, strip);

                Color blendedHighlight = Color.Lerp(bodyHighlight, handleHighlight, handleStrength);
                Color blendedShadow = Color.Lerp(bodyShadow, handleShadow, handleStrength);
                Color midTone = Color.Lerp(bodyBase, handleBase, handleStrength);

                Color pixelColor = Color.Lerp(blendedShadow, blendedHighlight, blend);
                pixelColor = Color.Lerp(midTone, pixelColor, 0.85f);

                float centerAccent = Mathf.Clamp01(strip * 1.1f);
                pixelColor = Color.Lerp(pixelColor, blendedHighlight, centerAccent * 0.1f);

                float tipHighlight = handleLength > 0 ? Mathf.Clamp01(1f - (distanceFromEdge / (float)handleLength)) : 0f;
                if (handleStrength > 0f && tipHighlight > 0f)
                {
                    float tipBlend = tipHighlight * handleStrength * 0.3f;
                    pixelColor = Color.Lerp(pixelColor, blendedHighlight, tipBlend);
                }

                pixels[(y * width) + x] = pixelColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        cachedSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        return cachedSprite;
    }
}
