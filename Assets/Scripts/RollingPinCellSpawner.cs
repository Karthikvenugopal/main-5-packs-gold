using UnityEngine;

public class RollingPinCellSpawner : MonoBehaviour
{
    [Header("Rolling Pin Prefab")]
    [SerializeField] private GameObject rollingPinPrefab;

    [Header("Movement Settings")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -1f); 
    [SerializeField] private Vector2 moveDirection = Vector2.up;
    [SerializeField] private float moveSpeed = 2.0f;

    private GameObject spawnedPin;

    private void Start()
    {
        ExitTrigger exit = FindAnyObjectByType<ExitTrigger>();
        if (exit == null)
        {
            Debug.LogWarning("RollingPinCellSpawner: Could not find ExitTrigger in the scene!");
            return;
        }
        Vector3 spawnPos = exit.transform.position + (Vector3)spawnOffset;

        if (rollingPinPrefab != null)
        {
            spawnedPin = Instantiate(rollingPinPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"RollingPin spawned at {spawnPos}");

            RollingPinEnemy enemy = spawnedPin.GetComponent<RollingPinEnemy>();
            if (enemy != null)
            {
                var dir = moveDirection != Vector2.zero ? moveDirection : Vector2.up;
                var rb = spawnedPin.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.gravityScale = 0f;
                    rb.freezeRotation = true;
                }

                typeof(RollingPinEnemy)
                    .GetField("initialDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(enemy, dir);
            }
        }
        else
        {
            Debug.LogWarning("RollingPinCellSpawner: Rolling Pin Prefab not assigned!");
        }
    }
}
