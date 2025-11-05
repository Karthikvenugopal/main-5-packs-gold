using UnityEngine;
using System.Collections.Generic; 


[RequireComponent(typeof(Collider2D))]
public class TutorialExitZone : MonoBehaviour
{
    private GameManager _gameManager;

    private List<CoopPlayerController> _playersInZone = new List<CoopPlayerController>();

    private void Start()
    {
        _gameManager = FindAnyObjectByType<GameManager>();
        if (_gameManager == null)
        {
            Debug.LogError("CombinedExitZone 找不到场景中的 GameManager!", this);
        }

        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_gameManager == null) return;

        if (other.TryGetComponent(out CoopPlayerController player))
        {
            if (!_playersInZone.Contains(player))
            {
                _playersInZone.Add(player);
                _gameManager.OnPlayerEnteredExit(player);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_gameManager == null) return;

        if (other.TryGetComponent(out CoopPlayerController player))
        {
            if (_playersInZone.Contains(player))
            {
                _playersInZone.Remove(player);
                _gameManager.OnPlayerExitedExit(player);
            }
        }
    }
}