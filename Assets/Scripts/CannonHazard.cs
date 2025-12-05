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
    [SerializeField] private bool limitProjectileDistance = false;
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
    private BoxCollider2D _solidCollider;

    private static Sprite _fallbackSprite;

    private void Awake()
    {
        EnsureVisuals();
        EnsureCollider();
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

        if (_projectilePrefabToUse != null)
        {
            projectileGO = Instantiate(_projectilePrefabToUse, transform.position, transform.rotation, transform.parent);
        }
        else
        {
            projectileGO = CreateFallbackProjectile();
            projectileGO.transform.rotation = transform.rotation;
        }

        projectileGO.transform.SetParent(transform.parent, true);
        Vector3 aimVector3 = transform.up;
        Vector2 aimVector2 = new Vector2(aimVector3.x, aimVector3.y);
        if (aimVector2.sqrMagnitude < 0.0001f)
        {
            aimVector2 = Vector2.up;
            aimVector3 = Vector3.up;
        }
        else
        {
            aimVector2.Normalize();
            aimVector3 = new Vector3(aimVector2.x, aimVector2.y, 0f);
        }

        projectileGO.transform.position = transform.position + aimVector3 * (_cellSize * muzzleOffset);

        if (!projectileGO.TryGetComponent(out CannonProjectile projectile))
        {
            projectile = projectileGO.AddComponent<CannonProjectile>();
        }

        float travelDistance = limitProjectileDistance
            ? Mathf.Max(_cellSize * projectileTravelHeight, _cellSize)
            : float.PositiveInfinity;
        projectile.Initialize(
            aimVector2,
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

    private void EnsureCollider()
    {
        if (!TryGetComponent(out _solidCollider))
        {
            _solidCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        _solidCollider.isTrigger = false;
        _solidCollider.usedByComposite = false;
        UpdateColliderDimensions();
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

        UpdateColliderDimensions();
    }

    private void UpdateColliderDimensions()
    {
        if (_solidCollider == null) return;

        float width = _cellSize * 0.7f;
        float height = _cellSize * 0.5f;
        _solidCollider.size = new Vector2(width, height);
        _solidCollider.offset = new Vector2(0f, height * 0.5f);
    }

    private void ApplyVariantStyling()
    {
        Color bodyColor = _variant == CannonVariant.Fire ? fireBodyColor : iceBodyColor;
        Color barrelColor = _variant == CannonVariant.Fire ? fireBarrelColor : iceBarrelColor;

        if (_bodyRenderer != null)
        {
            _bodyRenderer.color = bodyColor;
        }

        if (_barrelRenderer != null)
        {
            _barrelRenderer.color = barrelColor;
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

    public void OverrideTheme(Color body, Color barrel, Color projectile, Color impact)
    {
        if (_variant == CannonVariant.Fire)
        {
            fireBodyColor = body;
            fireBarrelColor = barrel;
            fireProjectileColor = projectile;
            fireImpactColor = impact;
        }
        else
        {
            iceBodyColor = body;
            iceBarrelColor = barrel;
            iceProjectileColor = projectile;
            iceImpactColor = impact;
        }

        _projectileColor = projectile;
        _impactColor = impact;

        ApplyVariantStyling();
    }
}
