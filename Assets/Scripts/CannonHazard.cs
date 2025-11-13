using UnityEngine;

public enum CannonVariant
{
    Fire = 0,
    Ice = 1
}

public class CannonHazard : MonoBehaviour
{
    [SerializeField] private float fireInterval = 1.25f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private float muzzleOffset = 0.75f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float projectileTravelHeight = 1.8f;

    [Header("Fire Variant")]
    [SerializeField] private Color fireBodyColor = new Color(0.35f, 0.18f, 0.12f);
    [SerializeField] private Color fireBarrelColor = new Color(0.78f, 0.32f, 0.18f);
    [SerializeField] private Color fireProjectileColor = new Color(0.86f, 0.2f, 0.2f);
    [SerializeField] private Color fireImpactColor = new Color(1f, 0.6f, 0.1f, 0.85f);

    [Header("Ice Variant")]
    [SerializeField] private Color iceBodyColor = new Color(0.18f, 0.28f, 0.45f);
    [SerializeField] private Color iceBarrelColor = new Color(0.4f, 0.7f, 0.95f);
    [SerializeField] private Color iceProjectileColor = new Color(0.55f, 0.85f, 1f);
    [SerializeField] private Color iceImpactColor = new Color(0.6f, 0.9f, 1f, 0.85f);

    private GameManager _gameManager;
    private float _timer;
    private float _cellSize = 1f;
    private CannonVariant _variant = CannonVariant.Fire;
    private GameObject _projectilePrefabToUse;
    private GameObject _hitEffectPrefabToUse;
    private Color _projectileColor;
    private Color _impactColor;
    private SpriteRenderer _bodyRenderer;
    private SpriteRenderer _barrelRenderer;

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

    public void Initialize(GameManager manager, float cellSize, CannonVariant variant, GameObject projectileOverride, GameObject hitEffectOverride)
    {
        _gameManager = manager;

        if (cellSize > 0f)
        {
            _cellSize = cellSize;
        }

        _variant = variant;
        _projectilePrefabToUse = projectileOverride != null ? projectileOverride : projectilePrefab;
        _hitEffectPrefabToUse = hitEffectOverride != null ? hitEffectOverride : hitEffectPrefab;

        _projectileColor = variant == CannonVariant.Fire ? fireProjectileColor : iceProjectileColor;
        _impactColor = variant == CannonVariant.Fire ? fireImpactColor : iceImpactColor;

        ApplySizing();
        ApplyVariantStyling();
    }

    private void Fire()
    {
        GameObject projectileGO;
        Vector2 fireDirection = Vector2.up;
        float muzzleSign = 1f;

        if (_variant == CannonVariant.Ice)
        {
            fireDirection = Vector2.down;
            muzzleSign = -1f;
        }

        if (_projectilePrefabToUse != null)
        {
            projectileGO = Instantiate(_projectilePrefabToUse, transform.position, Quaternion.identity, transform.parent);
        }
        else
        {
            projectileGO = CreateFallbackProjectile();
        }

        projectileGO.transform.SetParent(transform.parent, true);
        projectileGO.transform.position = transform.position + Vector3.up * (_cellSize * muzzleOffset * muzzleSign);

        if (!projectileGO.TryGetComponent(out CannonProjectile projectile))
        {
            projectile = projectileGO.AddComponent<CannonProjectile>();
        }

        float travelDistance = Mathf.Max(_cellSize * projectileTravelHeight, _cellSize);
        projectile.Initialize(
            fireDirection,
            projectileSpeed,
            projectileLifetime,
            _gameManager,
            travelDistance,
            _hitEffectPrefabToUse,
            _impactColor,
            _variant
        );
    }

    private GameObject CreateFallbackProjectile()
    {
        GameObject projectile = new GameObject(_variant == CannonVariant.Fire ? "FireProjectile" : "IceProjectile");
        projectile.transform.SetParent(transform.parent, false);

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackSprite();
        renderer.color = _projectileColor;
        renderer.sortingOrder = 8;

        BoxCollider2D collider = projectile.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        Vector2 colliderSize = new Vector2(_cellSize * 0.14f, _cellSize * 0.45f);
        collider.size = colliderSize;
        collider.offset = new Vector2(0f, colliderSize.y * 0.5f);

        projectile.transform.localScale = new Vector3(_cellSize * 0.14f, _cellSize * 0.45f, 1f);

        return projectile;
    }

    private void EnsureVisuals()
    {
        _bodyRenderer = GetComponent<SpriteRenderer>();
        if (_bodyRenderer == null)
        {
            _bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        _bodyRenderer.sprite = GetFallbackSprite();
        _bodyRenderer.sortingOrder = 6;

        Transform barrelTransform = transform.Find("Barrel");
        if (barrelTransform == null)
        {
            GameObject barrelGO = new GameObject("Barrel");
            barrelGO.transform.SetParent(transform, false);
            barrelGO.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            barrelTransform = barrelGO.transform;
        }

        _barrelRenderer = barrelTransform.GetComponent<SpriteRenderer>();
        if (_barrelRenderer == null)
        {
            _barrelRenderer = barrelTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        _barrelRenderer.sprite = GetFallbackSprite();
        _barrelRenderer.sortingOrder = 7;
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

    private void ApplyVariantStyling()
    {
        Color bodyColor = _variant == CannonVariant.Fire ? fireBodyColor : iceBodyColor;
        Color barrelColor = _variant == CannonVariant.Fire ? fireBarrelColor : iceBarrelColor;
        float zRotation = _variant == CannonVariant.Ice ? 180f : 0f;

        if (_bodyRenderer != null)
        {
            _bodyRenderer.color = bodyColor;
            _bodyRenderer.flipY = false;
        }

        if (_barrelRenderer != null)
        {
            _barrelRenderer.color = barrelColor;
            _barrelRenderer.flipY = false;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
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
