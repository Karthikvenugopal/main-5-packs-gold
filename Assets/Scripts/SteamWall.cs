using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SteamWall : MonoBehaviour
{
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 如果 SteamWall 用的是真实碰撞体（isTrigger = false），会走这个回调
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandlePlayer(collision.collider);
    }

    /// <summary>
    /// 如果 SteamWall 的 Collider 勾了 isTrigger，则会走这个回调
    /// （两种都写上更保险）
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayer(other);
    }

    /// <summary>
    /// 统一处理逻辑：判断碰到的是不是玩家，以及玩家是否处于蒸汽模式
    /// </summary>
    private void TryHandlePlayer(Collider2D collider)
    {
        var player = collider.GetComponent<CoopPlayerController>();
        if (player == null) return; // 不是玩家，直接无视

        var steamState = player.GetComponent<PlayerSteamState>();
        bool inSteam = steamState != null && steamState.IsInSteamMode;

        if (inSteam)
        {
            // 玩家处于蒸汽模式：这面墙被消除，玩家可以穿过去
            Debug.Log($"[SteamWall] {name} destroyed by {player.name} in STEAM MODE.", this);
            Destroy(gameObject);
        }
        else
        {
            // 玩家不在蒸汽模式：什么都不做，保持默认物理阻挡
            // （不用写代码，Collider 自然会 block 住他们）
            Debug.Log($"[SteamWall] {name} blocked {player.name} (NOT in steam mode).", this);
        }
    }
}
