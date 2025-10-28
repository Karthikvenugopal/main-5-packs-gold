using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    private readonly HashSet<CoopPlayerController> _occupants = new();
    private GameManager _gameManager;

    public void Initialize(GameManager manager)
    {
        _gameManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        if (_occupants.Add(player))
        {
            _gameManager?.OnPlayerEnteredExit(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        if (_occupants.Remove(player))
        {
            _gameManager?.OnPlayerExitedExit(player);
        }
    }
}
