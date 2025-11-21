using UnityEngine;

public class SteamWall : MonoBehaviour
{
    private Collider2D col;
    private CoopPlayerController fireboy;
    private CoopPlayerController watergirl;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        CoopPlayerController[] players = FindObjectsOfType<CoopPlayerController>();
        foreach (var p in players)
        {
            if (p.Role == PlayerRole.Fireboy) fireboy = p;
            else watergirl = p;
        }
    }

    private void Update()
    {
        bool fireSteam = fireboy.GetComponent<PlayerSteamState>().IsInSteamMode;
        bool waterSteam = watergirl.GetComponent<PlayerSteamState>().IsInSteamMode;

        // 只要任一玩家在蒸汽形态，蒸汽墙就暂时关闭
        col.enabled = !(fireSteam || waterSteam);
    }
}
