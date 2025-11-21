using UnityEngine;

public class PlayerSteamState : MonoBehaviour
{
    public bool IsInSteamMode { get; private set; }
    private float steamTimer = 0f;

    public void EnterSteamMode(float duration)
    {
        IsInSteamMode = true;
        steamTimer = duration;
    }

    private void Update()
    {
        if (IsInSteamMode)
        {
            steamTimer -= Time.deltaTime;
            if (steamTimer <= 0f)
                IsInSteamMode = false;
        }
    }
}
