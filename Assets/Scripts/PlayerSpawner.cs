using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameManagerTutorial gameManagerTutorial;
    [SerializeField] private float playerVerticalOffset = 0f;
    [SerializeField] private Vector2 playerScale = new Vector2(0.35f, 0.35f);
    [SerializeField] private Vector2 playerColliderSize = new Vector2(0.3f, 0.3f);
    [SerializeField, Range(0.2f, 1f)] private float collisionBoxScaleOverride = 0.6f;

    private void Start()
    {
        SpawnPlayer("FireboySpawn", PlayerRole.Fireboy);
        SpawnPlayer("WatergirlSpawn", PlayerRole.Watergirl);
    }

    private void SpawnPlayer(string spawnName, PlayerRole role)
    {
        GameObject spawnMarker = GameObject.Find(spawnName);

        if (spawnMarker == null)
        {
            Debug.LogWarning($"Spawn marker '{spawnName}' not found.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning("Player prefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = spawnMarker.transform.position;
        spawnPosition.y -= playerVerticalOffset;

        GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerObject.name = role.ToString();
        playerObject.tag = role == PlayerRole.Fireboy ? "FirePlayer" : "WaterPlayer";

        DisableLegacyComponents(playerObject);
        Vector3 appliedScale;
        Vector2 colliderOffset;
        EnsurePhysicsConfiguration(playerObject, out appliedScale, out colliderOffset);

        CoopPlayerController controller = playerObject.GetComponent<CoopPlayerController>();
        if (controller == null)
        {
            controller = playerObject.AddComponent<CoopPlayerController>();
        }
        controller.ApplySpawnDimensions(appliedScale, playerColliderSize, colliderOffset, collisionBoxScaleOverride);
        
        if (gameManagerTutorial != null)
        {
            controller.Initialize(role, gameManagerTutorial);
        }
        else if (gameManager != null)
        {
            controller.Initialize(role, gameManager);
        }
        else
        {
            Debug.LogWarning("PlayerSpawner: No GameManager or GameManagerTutorial assigned!");
        }

        spawnMarker.SetActive(false);
    }

    private static void DisableLegacyComponents(GameObject playerObject)
    {
        if (playerObject.TryGetComponent<CircleCollider2D>(out var circleCollider))
        {
            Object.Destroy(circleCollider);
        }
    }

    private void EnsurePhysicsConfiguration(GameObject playerObject, out Vector3 appliedScale, out Vector2 colliderOffset)
    {
        if (!playerObject.TryGetComponent(out Rigidbody2D body))
        {
            body = playerObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        appliedScale = new Vector3(playerScale.x, playerScale.y, 1f);
        playerObject.transform.localScale = appliedScale;

        if (!playerObject.TryGetComponent(out Collider2D collider))
        {
            collider = playerObject.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = false;

        if (collider is BoxCollider2D boxCollider)
        {
            boxCollider.size = playerColliderSize;
            colliderOffset = boxCollider.offset;
        }
        else
        {
            colliderOffset = Vector2.zero;
        }
    }
}
