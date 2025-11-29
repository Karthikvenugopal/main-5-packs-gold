using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SteamArea : MonoBehaviour
{
    [Header("Steam Settings")]
    [SerializeField] private float steamDuration = 10f;
    [SerializeField] private float touchDistance = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool debugDistances = true;

    private CoopPlayerController _fireboy;
    private CoopPlayerController _watergirl;

    private bool _fireInside;
    private bool _waterInside;
    private bool _steamTriggeredWhileInside;
    private GameManager _cachedGameManager;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (_fireboy == null || _watergirl == null)
        {
            var players = FindObjectsOfType<CoopPlayerController>();
            foreach (var p in players)
            {
                if (p.Role == PlayerRole.Fireboy) _fireboy = p;
                else if (p.Role == PlayerRole.Watergirl) _watergirl = p;
            }
        }

        if (_fireboy == null || _watergirl == null) return;
        if (!_fireInside || !_waterInside) return;

        float sqrDist = (_fireboy.transform.position - _watergirl.transform.position).sqrMagnitude;
        float threshold = touchDistance * touchDistance;

        if (debugDistances)
        {
            Debug.Log($"[SteamArea] Both inside. sqrDist={sqrDist:F3}, threshold={threshold:F3}", this);
        }

        if (sqrDist <= threshold && !_steamTriggeredWhileInside)
        {
            TriggerSteamMode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<CoopPlayerController>();
        if (player == null) return;

        if (player.Role == PlayerRole.Fireboy)
        {
            _fireInside = true;
            if (debugLogs) Debug.Log("[SteamArea] Fireboy entered steam area.", this);
        }
        else if (player.Role == PlayerRole.Watergirl)
        {
            _waterInside = true;
            if (debugLogs) Debug.Log("[SteamArea] Watergirl entered steam area.", this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<CoopPlayerController>();
        if (player == null) return;

        if (player.Role == PlayerRole.Fireboy)
        {
            _fireInside = false;
            if (debugLogs) Debug.Log("[SteamArea] Fireboy exited steam area.", this);
        }
        else if (player.Role == PlayerRole.Watergirl)
        {
            _waterInside = false;
            if (debugLogs) Debug.Log("[SteamArea] Watergirl exited steam area.", this);
        }

        if (!_fireInside || !_waterInside)
        {
            _steamTriggeredWhileInside = false;
        }
    }

    private void TriggerSteamMode()
    {
        _steamTriggeredWhileInside = true;

        if (debugLogs)
        {
            Debug.Log("[SteamArea] TriggerSteamMode(): activating steam mode for both players.", this);
        }

        var fireSteam = _fireboy != null ? _fireboy.GetComponent<PlayerSteamState>() : null;
        var waterSteam = _watergirl != null ? _watergirl.GetComponent<PlayerSteamState>() : null;

        fireSteam?.EnterSteamMode(steamDuration);
        waterSteam?.EnterSteamMode(steamDuration);
        GetGameManager()?.StartSteamCountdown(steamDuration);
    }

    private GameManager GetGameManager()
    {
        if (_cachedGameManager != null) return _cachedGameManager;
        _cachedGameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
        return _cachedGameManager;
    }
}
