using UnityEngine;




[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TokenCollect : MonoBehaviour
{
    [Header("Player Tags")]
    [SerializeField] private string firePlayerTag = "FirePlayer";
    [SerializeField] private string waterPlayerTag = "WaterPlayer";

    [Header("Token Tags")]
    [SerializeField] private string fireTokenTag = "FireToken";
    [SerializeField] private string waterTokenTag = "WaterToken";

    [Header("Shockwave Effect")]
    [SerializeField] private Color fireShockwaveColor = new Color(1f, 0.55f, 0.15f);
    [SerializeField] private Color waterShockwaveColor = new Color(0.2f, 0.45f, 0.95f);
    [SerializeField, Range(0.05f, 1f)] private float shockwaveStartScale = 0.15f;
    [SerializeField, Range(0.2f, 3f)] private float shockwaveEndScale = 1.2f;
    [SerializeField, Range(0.1f, 2f)] private float shockwaveDuration = 0.45f;

    private GameManager _gameManager;

    private void Awake()
    {
        
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager == null)
        {
            Debug.LogError("TokenCollect: No GameManager found in the scene to track token totals.");
        }

        
        Collider2D tokenCollider = GetComponent<Collider2D>();
        if (tokenCollider != null)
        {
            tokenCollider.isTrigger = true;
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (_gameManager == null)
        {
            return;
        }

        
        if (CompareTag(fireTokenTag) && other.CompareTag(firePlayerTag))
        {
            SpawnShockwave(fireShockwaveColor);
            _gameManager.OnFireTokenCollected();
            Destroy(gameObject);
            return;
        }

        
        if (CompareTag(waterTokenTag) && other.CompareTag(waterPlayerTag))
        {
            SpawnShockwave(waterShockwaveColor);
            _gameManager.OnWaterTokenCollected();
            Destroy(gameObject);
            return;
        }

        
    }

    private void SpawnShockwave(Color color)
    {
        Vector3 center = transform.position;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            center += (Vector3)(sr.sprite.bounds.center);
        }
        TokenShockwaveEffect.Spawn(center, color, shockwaveStartScale, shockwaveEndScale, shockwaveDuration);
    }
}
