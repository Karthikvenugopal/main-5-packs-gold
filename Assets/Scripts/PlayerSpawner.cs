using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameManager gameManager;

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

        GameObject playerObject = Instantiate(playerPrefab, spawnMarker.transform.position, Quaternion.identity);
        playerObject.name = role.ToString();
        playerObject.tag = "Player";

        DisableLegacyComponents(playerObject);
        EnsurePhysicsConfiguration(playerObject);

        CoopPlayerController controller = playerObject.GetComponent<CoopPlayerController>();
        if (controller == null)
        {
            controller = playerObject.AddComponent<CoopPlayerController>();
        }
        controller.Initialize(role, gameManager);

        spawnMarker.SetActive(false);
    }

    private static void DisableLegacyComponents(GameObject playerObject)
    {
        if (playerObject.TryGetComponent<CircleCollider2D>(out var circleCollider))
        {
            Object.Destroy(circleCollider);
        }
    }

    private static void EnsurePhysicsConfiguration(GameObject playerObject)
    {
        if (!playerObject.TryGetComponent(out Rigidbody2D body))
        {
            body = playerObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!playerObject.TryGetComponent(out Collider2D collider))
        {
            collider = playerObject.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = false;
    }
}
