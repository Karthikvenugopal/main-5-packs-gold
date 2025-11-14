using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CannonProjectile : MonoBehaviour
{
    [SerializeField] private float _speed = 6f;
    [SerializeField] private float _lifetime = 4f;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private string fireTokenTag = "FireToken";
    [SerializeField] private string waterTokenTag = "WaterToken";

    private Vector2 _direction = Vector2.up;
    private Vector2 _startPosition;
    private float _age;
    private bool _consumed;
    private GameManager _gameManager;
    private GameObject _hitEffectPrefab;
    private Collider2D _collider;
    private Rigidbody2D _rigidbody;
    private float _maxTravelDistance = float.PositiveInfinity;
    private Color _impactColor = new Color(1f, 0.6f, 0.1f, 0.85f);
    private CannonVariant _variant = CannonVariant.Fire;

    private static Sprite _fallbackEffectSprite;

    public void Initialize(
        Vector2 direction,
        float speed,
        float lifetime,
        GameManager manager,
        float maxTravelDistance,
        GameObject hitEffectPrefab,
        Color impactColor,
        CannonVariant variant
    )
    {
        if (direction.sqrMagnitude > 0f)
        {
            _direction = direction.normalized;
        }

        _speed = speed > 0f ? speed : _speed;
        _lifetime = lifetime > 0f ? lifetime : _lifetime;
        _gameManager = manager;
        _hitEffectPrefab = hitEffectPrefab;
        _impactColor = impactColor;
        _startPosition = transform.position;
        _maxTravelDistance = maxTravelDistance > 0f ? maxTravelDistance : float.PositiveInfinity;
        _variant = variant;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody2D>();
        }

        ConfigureRigidbody();
        EnsureCollisionMask();

        _startPosition = transform.position;
    }

    private void Update()
    {
        if (_consumed) return;

        Vector3 start = transform.position;
        Vector2 current2D = start;
        float travelled = Vector2.Dot(current2D - _startPosition, _direction);
        float remaining = float.PositiveInfinity;

        if (_maxTravelDistance < float.PositiveInfinity)
        {
            remaining = _maxTravelDistance - travelled;
            if (remaining <= 0f)
            {
                Vector2 capPoint = _startPosition + _direction * _maxTravelDistance;
                TriggerImpact(capPoint, null);
                return;
            }
        }

        Vector2 displacement2D = _direction * (_speed * Time.deltaTime);
        float distance = displacement2D.magnitude;

        if (distance > 0f)
        {
            if (_maxTravelDistance < float.PositiveInfinity && distance >= remaining)
            {
                Vector2 capPoint = _startPosition + _direction * _maxTravelDistance;
                transform.position = capPoint;
                TriggerImpact(capPoint, null);
                return;
            }

            Vector2 origin = (Vector2)(start + (Vector3)(_direction * 0.02f));
            float rayDistance = distance + 0.02f;
            RaycastHit2D hit = RaycastIgnoringTokens(origin, rayDistance);
            if (hit.collider != null)
            {
                transform.position = hit.point - (Vector2)_direction * 0.02f;
                TriggerImpact(hit.point, hit.collider);
                return;
            }
        }

        transform.position += (Vector3)displacement2D;

        if (_maxTravelDistance < float.PositiveInfinity)
        {
            Vector2 updated2D = transform.position;
            travelled = Vector2.Dot(updated2D - _startPosition, _direction);
            if (travelled >= _maxTravelDistance)
            {
                Vector2 capPoint = _startPosition + _direction * _maxTravelDistance;
                transform.position = capPoint;
                TriggerImpact(capPoint, null);
                return;
            }
        }

        _age += Time.deltaTime;
        if (_age >= _lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void ConfigureRigidbody()
    {
        if (_rigidbody == null) return;

        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody.useFullKinematicContacts = true;
        _rigidbody.gravityScale = 0f;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rigidbody.freezeRotation = true;
    }

    private void EnsureCollisionMask()
    {
        int mask = collisionMask.value;
        if (mask == 0)
        {
            mask = Physics2D.AllLayers;
        }

        mask = IncludeLayer(mask, "Wall");
        mask = IncludeLayer(mask, "Default");
        collisionMask = mask;
    }

    private static int IncludeLayer(int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            mask |= 1 << layer;
        }

        return mask;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || other == _collider || IsTokenCollider(other)) return;

        Vector2 impactPoint = other.ClosestPoint(transform.position);
        TriggerImpact(impactPoint, other);
    }

    private RaycastHit2D RaycastIgnoringTokens(Vector2 origin, float distance)
    {
        float remaining = distance;
        Vector2 currentOrigin = origin;
        const float epsilon = 0.0005f;

        while (remaining > 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentOrigin, _direction, remaining, collisionMask);
            if (hit.collider == null)
            {
                return default;
            }

            if (hit.collider == _collider || IsTokenCollider(hit.collider))
            {
                float advance = Mathf.Max(hit.distance + epsilon, epsilon);
                currentOrigin += _direction * advance;
                remaining -= advance;
                continue;
            }

            return hit;
        }

        return default;
    }

    private bool IsTokenCollider(Collider2D collider)
    {
        if (collider == null) return false;

        if (!string.IsNullOrEmpty(fireTokenTag) && collider.CompareTag(fireTokenTag))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(waterTokenTag) && collider.CompareTag(waterTokenTag))
        {
            return true;
        }

        return false;
    }

    private void TriggerImpact(Vector2 hitPoint, Collider2D collider)
    {
        if (_consumed) return;
        _consumed = true;

        if (collider != null && collider.TryGetComponent(out CoopPlayerController player))
        {
            if (ShouldAffectPlayer(player))
            {
                _gameManager?.OnPlayerHitByEnemy(player, _variant);
            }
        }

        SpawnHitEffect(hitPoint);
        Destroy(gameObject);
    }

    private void SpawnHitEffect(Vector2 position)
    {
        GameObject effect = null;

        if (_hitEffectPrefab != null)
        {
            effect = Instantiate(_hitEffectPrefab, position, Quaternion.identity, transform.parent);
        }
        else
        {
            effect = CreateFallbackEffect(position);
        }

        if (effect != null && _hitEffectPrefab == null)
        {
            Destroy(effect, 0.4f);
        }
    }

    private GameObject CreateFallbackEffect(Vector2 position)
    {
        GameObject effect = new GameObject("CannonImpact");
        effect.transform.SetParent(transform.parent, false);
        effect.transform.position = position;

        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackEffectSprite();
        renderer.color = _impactColor;
        renderer.sortingOrder = 9;

        float baseScale = Mathf.Max(transform.localScale.y, 0.4f);
        effect.transform.localScale = new Vector3(baseScale, baseScale, 1f);

        return effect;
    }

    private static Sprite GetFallbackEffectSprite()
    {
        if (_fallbackEffectSprite != null) return _fallbackEffectSprite;

        Texture2D texture = new Texture2D(2, 2);
        texture.SetPixels(new[]
        {
            new Color(1f, 1f, 1f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(1f, 1f, 1f, 1f)
        });
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        _fallbackEffectSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            2f
        );

        return _fallbackEffectSprite;
    }

    private bool ShouldAffectPlayer(CoopPlayerController player)
    {
        if (player == null) return false;

        if (_variant == CannonVariant.Fire && player.Role == PlayerRole.Watergirl)
        {
            return false;
        }

        if (_variant == CannonVariant.Ice && player.Role == PlayerRole.Fireboy)
        {
            return false;
        }

        return true;
    }
}
