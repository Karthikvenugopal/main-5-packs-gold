using UnityEngine;

public class CannonHazard : MonoBehaviour
{
    [SerializeField] private float fireInterval = 1.25f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private float muzzleOffset = 0.75f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float projectileTravelHeight = 1.8f;

    private GameManager _gameManager;
    private float _timer;
    private float _cellSize = 1f;
    private GameObject _hitEffectPrefab;

    private static Sprite _fallbackSprite;

    private void Awake()
    {
        EnsureVisuals();
        _timer = Random.Range(0f, fireInterval);
    }

    private void Update()
    {
        if (fireInterval <= 0f) return;

        _timer += Time.deltaTime;
        if (_timer >= fireInterval)
        {
            Fire();
            _timer = 0f;
        }
    }

    public void Initialize(GameManager manager, GameObject overrideProjectilePrefab, float cellSize, GameObject overrideHitEffectPrefab)
    {
        _gameManager = manager;

        if (overrideProjectilePrefab != null)
        {
            projectilePrefab = overrideProjectilePrefab;
        }

        if (cellSize > 0f)
        {
            _cellSize = cellSize;
        }

        if (overrideHitEffectPrefab != null)
        {
            hitEffectPrefab = overrideHitEffectPrefab;
        }

        _hitEffectPrefab = hitEffectPrefab;

        ApplySizing();
    }

    private void Fire()
    {
        GameObject projectileGO;

        if (projectilePrefab != null)
        {
            projectileGO = Instantiate(projectilePrefab, transform.position, Quaternion.identity, transform.parent);
        }
        else
        {
            projectileGO = CreateFallbackProjectile();
        }

        projectileGO.transform.SetParent(transform.parent, true);
        projectileGO.transform.position = transform.position + Vector3.up * (_cellSize * muzzleOffset);

        if (!projectileGO.TryGetComponent(out CannonProjectile projectile))
        {
            projectile = projectileGO.AddComponent<CannonProjectile>();
        }

        float travelDistance = Mathf.Max(_cellSize * projectileTravelHeight, _cellSize);
        projectile.Initialize(Vector2.up, projectileSpeed, projectileLifetime, _gameManager, travelDistance, _hitEffectPrefab);
    }

    private GameObject CreateFallbackProjectile()
    {
        GameObject projectile = new GameObject("CannonProjectile");
        projectile.transform.SetParent(transform.parent, false);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackSprite();
        renderer.color = new Color(0.86f, 0.2f, 0.2f);
        renderer.sortingOrder = 8;

        BoxCollider2D collider = projectile.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        Vector2 colliderSize = new Vector2(_cellSize * 0.25f, _cellSize * 0.7f);
        collider.size = colliderSize;
        collider.offset = new Vector2(0f, colliderSize.y * 0.5f);

        projectile.transform.localScale = new Vector3(_cellSize * 0.25f, _cellSize * 0.7f, 1f);

        return projectile;
    }

    private void EnsureVisuals()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = GetFallbackSprite();
        renderer.color = new Color(0.2f, 0.2f, 0.2f);
        renderer.sortingOrder = 6;

        if (transform.Find("Barrel") == null)
        {
            GameObject barrel = new GameObject("Barrel");
            barrel.transform.SetParent(transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.25f, 0f);

            SpriteRenderer barrelRenderer = barrel.AddComponent<SpriteRenderer>();
            barrelRenderer.sprite = GetFallbackSprite();
            barrelRenderer.color = new Color(0.4f, 0.4f, 0.4f);
            barrelRenderer.sortingOrder = 7;

            barrel.transform.localScale = new Vector3(0.25f, 1.1f, 1f);
        }
    }

    private void ApplySizing()
    {
        transform.localScale = new Vector3(_cellSize * 0.75f, _cellSize * 0.45f, 1f);

        Transform barrel = transform.Find("Barrel");
        if (barrel != null)
        {
            barrel.localPosition = new Vector3(0f, _cellSize * 0.25f, 0f);
            barrel.localScale = new Vector3(_cellSize * 0.25f, _cellSize * 1.1f, 1f);
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (_fallbackSprite != null) return _fallbackSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        _fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0f),
            1f
        );

        return _fallbackSprite;
    }
}
