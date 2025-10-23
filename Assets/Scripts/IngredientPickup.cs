using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[ExecuteAlways]
public class IngredientPickup : MonoBehaviour
{
    [SerializeField] private IngredientType ingredientType = IngredientType.Chili;
    [SerializeField] private float abilityDurationSeconds = 12f;

    private bool isCollected;
    private GameManager gameManager;
    private GameManagerTutorial tutorialManager;
    private SpriteRenderer spriteRenderer;
    private bool highlightActive;
    private Color baseColor = Color.white;
    private Vector3 originalScale = Vector3.one;
    [SerializeField] private Color highlightPulseColor = new Color(1f, 0.9f, 0.25f, 1f);
    [SerializeField] private float highlightPulseSpeed = 6f;
    [SerializeField] private float highlightScaleMultiplier = 1.15f;

    public IngredientType Type => ingredientType;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
        originalScale = transform.localScale;
    }

    private void Start()
    {
        ApplyVisuals();

        if (!Application.isPlaying) return;

        gameManager = Object.FindFirstObjectByType<GameManager>();
        tutorialManager = Object.FindFirstObjectByType<GameManagerTutorial>();

        if (gameManager == null && tutorialManager == null)
        {
            Debug.LogWarning("IngredientPickup could not find any GameManager in the scene. Pickups will still function for sandbox testing.");
        }
    }

    private void OnEnable()
    {
        ApplyVisuals();
    }

    private void OnDisable()
    {
        highlightActive = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
        transform.localScale = originalScale;
    }

    private void Update()
    {
        if (!highlightActive || spriteRenderer == null) return;

        float pulse = (Mathf.Sin(Time.time * highlightPulseSpeed) + 1f) * 0.5f;
        spriteRenderer.color = Color.Lerp(baseColor, highlightPulseColor, pulse);
        transform.localScale = originalScale * Mathf.Lerp(1f, highlightScaleMultiplier, pulse);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyVisuals();
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if (!other.TryGetComponent(out PlayerAbilityController abilityController)) return;

        // Single-slot behavior is enforced inside PlayerAbilityController.GrantAbility
        abilityController.GrantAbility(ingredientType, abilityDurationSeconds);
        isCollected = true;

        if (tutorialManager != null)
        {
            switch (ingredientType)
            {
                case IngredientType.Chili:  tutorialManager.OnChiliCollected();  break;
                case IngredientType.Butter: tutorialManager.OnButterCollected(); break;
                case IngredientType.Bread:  tutorialManager.OnBreadCollected();  break;
            }
        }
        else if (gameManager != null)
        {
            gameManager.OnIngredientEaten(ingredientType);
        }

        gameObject.SetActive(false);
    }

    public void Configure(IngredientType type, float durationSeconds)
    {
        ingredientType = type;
        abilityDurationSeconds = durationSeconds;
        isCollected = false;
        gameObject.SetActive(true);
        highlightActive = false;
        ApplyVisuals();
    }

    public void EnableHighlight()
    {
        if (highlightActive || !gameObject.activeInHierarchy) return;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null) return;

        baseColor = spriteRenderer.color;
        highlightActive = true;
    }

    private void ApplyVisuals()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        Sprite sprite = IngredientVisualFactory.GetSprite(ingredientType);
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
        }

        if (!highlightActive)
        {
            baseColor = spriteRenderer.color;
            transform.localScale = originalScale;
        }
    }
}
