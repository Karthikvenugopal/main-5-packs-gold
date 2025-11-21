using UnityEngine;

public class SteamArea : MonoBehaviour
{
    public float steamDuration = 10f;
    private CoopPlayerController fireboy;
    private CoopPlayerController watergirl;

    private bool fireInside = false;
    private bool waterInside = false;

    private void Start()
    {
        // 获取两个玩家引用
        CoopPlayerController[] players = FindObjectsOfType<CoopPlayerController>();
        foreach (var p in players)
        {
            if (p.Role == PlayerRole.Fireboy) fireboy = p;
            else watergirl = p;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out CoopPlayerController player))
        {
            if (player.Role == PlayerRole.Fireboy) fireInside = true;
            if (player.Role == PlayerRole.Watergirl) waterInside = true;

            TryActivateSteamMode();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out CoopPlayerController player))
        {
            if (player.Role == PlayerRole.Fireboy) fireInside = false;
            if (player.Role == PlayerRole.Watergirl) waterInside = false;
        }
    }

    private void TryActivateSteamMode()
    {
        if (fireInside && waterInside)
        {
            fireboy.GetComponent<PlayerSteamState>().EnterSteamMode(steamDuration);
            watergirl.GetComponent<PlayerSteamState>().EnterSteamMode(steamDuration);
        }
    }
}
