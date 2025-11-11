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
    [SerializeField] private int sortingOrder = 5;

    [Header("Breath Effect")]
    [Tooltip("Enables a gentle scale up/down animation so tokens feel alive.")]
    [SerializeField] private bool enableBreathEffect = true;
    [Tooltip("Time in seconds for a full shrink -> grow cycle.")]
    [SerializeField, Min(0.1f)] private float breathCycleDuration = 1.6f;
    [Tooltip("Range of the scale multiplier that will be applied on top of the prefab scale (x = min, y = max).")]
    [SerializeField] private Vector2 breathScaleRange = new Vector2(0.85f, 1.1f);
    [Tooltip("Randomizes the starting phase so nearby tokens do not pulse in sync.")]
    [SerializeField] private bool randomizePhase = true;

    private SpriteRenderer _spriteRenderer;
    private Vector3 _baseScale = Vector3.one;
    private float _phaseOffset;

    private void Awake()
    {
        CacheRenderer();
        CaptureBaseScale(force: true);
        InitializePhase();
        ApplySprite();
    }

    private void Reset()
    {
        ApplySprite();
    }

    private void OnValidate()
    {
        CacheRenderer();
        CaptureBaseScale(force: !Application.isPlaying);
        ApplySprite();
    }

    private void OnEnable()
    {
        CaptureBaseScale(force: true);
        InitializePhase();
    }

    private void Update()
    {
        ApplyBreathEffect();
    }

    private void CacheRenderer()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void CaptureBaseScale(bool force)
    {
        if (!force && Application.isPlaying) return;
        _baseScale = transform.localScale;
    }

    private void InitializePhase()
    {
        if (!randomizePhase)
        {
            _phaseOffset = 0f;
            return;
        }

        // Use position-based hash in edit mode so each token keeps a stable phase without relying on Random state.
        if (!Application.isPlaying)
        {
            int hash = transform.GetInstanceID();
            _phaseOffset = (hash & 1023) / 1023f * Mathf.PI * 2f;
            return;
        }

        _phaseOffset = UnityEngine.Random.value * Mathf.PI * 2f;
    }

    private void ApplySprite()
    {
        CacheRenderer();
        if (_spriteRenderer == null) return;

        _spriteRenderer.sortingOrder = sortingOrder;

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

    private void ApplyBreathEffect()
    {
        if (!enableBreathEffect)
        {
            transform.localScale = _baseScale;
            return;
        }

        float duration = Mathf.Max(0.01f, breathCycleDuration);
        float minScale = Mathf.Min(breathScaleRange.x, breathScaleRange.y);
        float maxScale = Mathf.Max(breathScaleRange.x, breathScaleRange.y);

        if (Mathf.Approximately(minScale, maxScale))
        {
            transform.localScale = _baseScale * minScale;
            return;
        }

        float elapsed = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        float normalized = 0.5f + 0.5f * Mathf.Sin((elapsed / duration) * Mathf.PI * 2f + _phaseOffset);
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, normalized);

        transform.localScale = _baseScale * scaleMultiplier;
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
