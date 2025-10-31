using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CannonProjectile : MonoBehaviour
{
    [SerializeField] private float _speed = 6f;
    [SerializeField] private float _lifetime = 4f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private Vector2 _direction = Vector2.up;
    private Vector2 _startPosition;
    private float _age;
    private bool _consumed;
    private GameManager _gameManager;
    private GameObject _hitEffectPrefab;
    private Collider2D _collider;
    private float _maxTravelDistance = float.PositiveInfinity;

    private static Sprite _fallbackEffectSprite;

    public void Initialize(Vector2 direction, float speed, float lifetime, GameManager manager, float maxTravelDistance, GameObject hitEffectPrefab)
    {
        if (direction.sqrMagnitude > 0f)
        {
            _direction = direction.normalized;
        }

        _speed = speed > 0f ? speed : _speed;
        _lifetime = lifetime > 0f ? lifetime : _lifetime;
        _gameManager = manager;
        _hitEffectPrefab = hitEffectPrefab;
        _startPosition = transform.position;
        _maxTravelDistance = maxTravelDistance > 0f ? maxTravelDistance : float.PositiveInfinity;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

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

            Vector3 origin = start + (Vector3)(_direction * 0.02f);
            float rayDistance = distance + 0.02f;
            RaycastHit2D hit = Physics2D.Raycast(origin, _direction, rayDistance, collisionMask);
            if (hit.collider != null && hit.collider != _collider)
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || other == _collider) return;

        Vector2 impactPoint = other.ClosestPoint(transform.position);
        TriggerImpact(impactPoint, other);
    }

    private void TriggerImpact(Vector2 hitPoint, Collider2D collider)
    {
        if (_consumed) return;
        _consumed = true;

        if (collider != null && collider.TryGetComponent(out CoopPlayerController player))
        {
            _gameManager?.OnPlayerHitByEnemy(player);
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
        renderer.color = new Color(1f, 0.6f, 0.1f, 0.85f);
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
}
