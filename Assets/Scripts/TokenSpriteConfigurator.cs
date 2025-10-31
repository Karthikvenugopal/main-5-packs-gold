using UnityEngine;

/// <summary>
/// Assigns a procedurally generated sprite to the token at edit and play time.
/// This keeps the project self-contained without relying on external art assets.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TokenSpriteConfigurator : MonoBehaviour
{
    public enum TokenType
    {
        Fire,
        Water
    }

    [Header("Token Appearance")]
    [SerializeField] private TokenType tokenType = TokenType.Fire;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        ApplySprite();
    }

    private void Reset()
    {
        ApplySprite();
    }

    private void OnValidate()
    {
        ApplySprite();
    }

    private void ApplySprite()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        _spriteRenderer.sortingOrder = 5;

        switch (tokenType)
        {
            case TokenType.Fire:
                _spriteRenderer.sprite = TokenSpriteLibrary.GetFireSprite();
                _spriteRenderer.color = new Color(1f, 0.55f, 0.15f);
                break;
            case TokenType.Water:
                _spriteRenderer.sprite = TokenSpriteLibrary.GetWaterSprite();
                _spriteRenderer.color = new Color(0.3f, 0.6f, 1f);
                break;
        }
    }
}

/// <summary>
/// Generates simple triangle and droplet sprites procedurally so no image files are required.
/// </summary>
public static class TokenSpriteLibrary
{
    private static Sprite _fireSprite;
    private static Sprite _waterSprite;

    public static Sprite GetFireSprite()
    {
        if (_fireSprite != null)
        {
            return _fireSprite;
        }

        _fireSprite = CreateFireSprite();
        return _fireSprite;
    }

    public static Sprite GetWaterSprite()
    {
        if (_waterSprite != null)
        {
            return _waterSprite;
        }

        _waterSprite = CreateWaterSprite();
        return _waterSprite;
    }

    private static Sprite CreateFireSprite()
    {
        const int size = 64;
        var texture = CreateBlankTexture(size);
        var pixels = texture.GetPixels32();
        int index = 0;

        float centerX = (size - 1) * 0.5f;
        float maxHalfWidth = centerX;

        for (int y = 0; y < size; y++)
        {
            float progress = 1f - y / (size - 1f);
            float halfWidth = Mathf.Lerp(0f, maxHalfWidth, progress);

            for (int x = 0; x < size; x++, index++)
            {
                if (Mathf.Abs(x - centerX) <= halfWidth)
                {
                    pixels[index] = Color.white;
                }
                else
                {
                    pixels[index] = Color.clear;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), size);
    }

    private static Sprite CreateWaterSprite()
    {
        const int size = 64;
        var texture = CreateBlankTexture(size);
        var pixels = texture.GetPixels32();
        int index = 0;

        Vector2 dropCenter = new Vector2((size - 1) * 0.5f, size * 0.55f);
        float radius = size * 0.32f;
        float radiusSqr = radius * radius;
        float bottomPointY = size * 0.05f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++, index++)
            {
                Vector2 pixel = new Vector2(x, y);
                bool withinCircle = (pixel - dropCenter).sqrMagnitude <= radiusSqr;

                float t = Mathf.InverseLerp(bottomPointY, dropCenter.y, y);
                float halfWidth = Mathf.Lerp(0f, radius, t);
                bool withinLowerCone = y <= dropCenter.y && Mathf.Abs(pixel.x - dropCenter.x) <= halfWidth;

                if (withinCircle || withinLowerCone)
                {
                    pixels[index] = Color.white;
                }
                else
                {
                    pixels[index] = Color.clear;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), size);
    }

    private static Texture2D CreateBlankTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
