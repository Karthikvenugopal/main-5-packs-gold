using UnityEngine;

/// <summary>
/// Simple expanding ring that fades out and destroys itself.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TokenShockwaveEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float startScale = 0.15f;
    [SerializeField] private float endScale = 1.2f;

    private SpriteRenderer _renderer;
    private Color _baseColor = Color.white;
    private float _elapsed;

    public static void Spawn(Vector3 position, Color color, float startScale = 0.15f, float endScale = 1.2f, float duration = 0.45f)
    {
        var go = new GameObject("TokenShockwave");
        go.transform.position = position;
        var effect = go.AddComponent<TokenShockwaveEffect>();
        effect.Initialize(color, startScale, endScale, duration);
    }

    private void Initialize(Color color, float startScaleValue, float endScaleValue, float effectDuration)
    {
        _baseColor = color;
        startScale = startScaleValue;
        endScale = endScaleValue;
        duration = Mathf.Max(0.05f, effectDuration);
        CacheRenderer();
        ApplySprite();
        transform.localScale = Vector3.one * startScale;
        _renderer.color = color;
    }

    private void Awake()
    {
        CacheRenderer();
        ApplySprite();
        transform.localScale = Vector3.one * startScale;
        _renderer.color = _baseColor;
    }

    private void CacheRenderer()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sortingOrder = 10;
        }
    }

    private void ApplySprite()
    {
        if (_renderer == null) return;
        _renderer.sprite = TokenSpriteLibrary.GetShockwaveSprite();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);
        float scale = Mathf.Lerp(startScale, endScale, EaseOutQuad(t));
        transform.localScale = Vector3.one * scale;

        Color c = _baseColor;
        c.a = Mathf.Lerp(_baseColor.a, 0f, t);
        _renderer.color = c;

        if (_elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
