using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SteamWall : MonoBehaviour
{
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    
    
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandlePlayer(collision.collider);
    }

    
    
    
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayer(other);
    }

    
    
    
    private void TryHandlePlayer(Collider2D collider)
    {
        var player = collider.GetComponent<CoopPlayerController>();
        if (player == null) return; 

        var steamState = player.GetComponent<PlayerSteamState>();
        bool inSteam = steamState != null && steamState.IsInSteamMode;

        if (inSteam)
        {
            
            Debug.Log($"[SteamWall] {name} destroyed by {player.name} in STEAM MODE.", this);
            Destroy(gameObject);
        }
        else
        {
            
            
            Debug.Log($"[SteamWall] {name} blocked {player.name} (NOT in steam mode).", this);
        }
    }
}
