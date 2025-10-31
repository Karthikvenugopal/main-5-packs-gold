using UnityEngine;

/// <summary>
/// Handles token collection rules for Fire and Water players.
/// </summary>
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

    private GameManager _gameManager;

    private void Awake()
    {
        // Cache the GameManager once so we can increment the correct counters.
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager == null)
        {
            Debug.LogError("TokenCollect: No GameManager found in the scene to track token totals.");
        }

        // Ensure the collider is configured as a trigger so OnTriggerEnter2D can fire.
        Collider2D tokenCollider = GetComponent<Collider2D>();
        if (tokenCollider != null)
        {
            tokenCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If we failed to locate the GameManager we cannot track tokens, so exit early.
        if (_gameManager == null)
        {
            return;
        }

        // Fire player touching a fire token increments the fire count and removes the token.
        if (CompareTag(fireTokenTag) && other.CompareTag(firePlayerTag))
        {
            _gameManager.OnFireTokenCollected();
            Destroy(gameObject);
            return;
        }

        // Water player touching a water token increments the water count and removes the token.
        if (CompareTag(waterTokenTag) && other.CompareTag(waterPlayerTag))
        {
            _gameManager.OnWaterTokenCollected();
            Destroy(gameObject);
            return;
        }

        // Wrong player or any other collider does nothing, keeping the token in place.
    }
}
