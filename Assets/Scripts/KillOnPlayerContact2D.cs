using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class KillOnPlayerContact2D : MonoBehaviour
{
    [SerializeField] private bool useTrigger = false;   
    [SerializeField] private float restartDelay = 0.75f;

    private bool _fired;

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (useTrigger || _fired) return;
        if (!col.collider.TryGetComponent<PlayerAbilityController>(out _)) return;
        HandleDeath();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger || _fired) return;
        if (!other.TryGetComponent<PlayerAbilityController>(out _)) return;
        HandleDeath();
    }

    private void HandleDeath()
    {
        _fired = true;

        // GameManager with fail method, call it here.
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            // method like gm.GameOver(); 

        }
        Invoke(nameof(Reload), restartDelay);
    }

    private void Reload()
    {
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }
}
