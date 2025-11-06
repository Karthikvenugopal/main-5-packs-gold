using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    private readonly HashSet<CoopPlayerController> _occupants = new();
    private GameManager _gameManager;
    private GameManagerTutorial _gameManagerTutorial;

    public void Initialize(GameManager manager)
    {
        _gameManager = manager;
        _gameManagerTutorial = null;
    }

    public void Initialize(GameManagerTutorial manager)
    {
        _gameManagerTutorial = manager;
        _gameManager = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        TryRegisterPlayer(player);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        if (_occupants.Remove(player))
        {
            _gameManager?.OnPlayerExitedExit(player);
            _gameManagerTutorial?.OnPlayerExitedExit(player);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        TryRegisterPlayer(player);
    }

    private void TryRegisterPlayer(CoopPlayerController player)
    {
        if (_occupants.Add(player))
        {
            _gameManager?.OnPlayerEnteredExit(player);
            _gameManagerTutorial?.OnPlayerEnteredExit(player);
        }
    }
}
