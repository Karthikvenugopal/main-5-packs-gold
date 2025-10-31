using UnityEngine;

// This script ensures that only the *correct* player
// can activate this specific exit zone.

[RequireComponent(typeof(Collider2D))]
public class TutorialExitZone : MonoBehaviour
{
    [Tooltip("Which player is this exit zone for?")]
    [SerializeField] private PlayerRole dedicatedRole;

    private GameManager _gameManager;
    private CoopPlayerController _playerInZone; // Tracks the *one* player this zone cares about

    private void Start()
    {
        // Automatically find the GameManager
        _gameManager = FindAnyObjectByType<GameManager>();
        if (_gameManager == null)
        {
            Debug.LogError("TutorialExitZone could not find a GameManager in the scene!", this);
        }

        // Ensure the collider is set to be a trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check if a GameManager was found
        if (_gameManager == null) return;

        // 2. Check if the object is a player AND if it's the correct player for this zone
        if (other.TryGetComponent(out CoopPlayerController player) && player.Role == dedicatedRole)
        {
            // 3. Check if the zone is already occupied by this player (prevents double-triggers)
            if (_playerInZone == null)
            {
                _playerInZone = player;
                
                // 4. Tell the GameManager that *this specific player* has reached an exit
                _gameManager.OnPlayerEnteredExit(player);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 1. Check if a GameManager was found
        if (_gameManager == null) return;

        // 2. Check if the player leaving is the *exact same one* we are tracking
        if (other.TryGetComponent(out CoopPlayerController player) && player == _playerInZone)
        {
            _playerInZone = null;

            // 3. Tell the GameManager that this player has left the exit
            _gameManager.OnPlayerExitedExit(player);
        }
    }
}