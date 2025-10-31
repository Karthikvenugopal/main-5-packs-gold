using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitTrigger : MonoBehaviour
{
    private GameManager gameManager;

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("FirePlayer") || other.CompareTag("WaterPlayer"))
        {
            gameManager?.OnExitReached();
        }
    }
}
