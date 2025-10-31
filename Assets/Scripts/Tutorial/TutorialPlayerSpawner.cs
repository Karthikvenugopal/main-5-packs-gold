using UnityEngine;

public class TutorialPlayerSpawner : MonoBehaviour
{
    [Header("Prefabs & Dependencies")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameManager gameManager;

    [Header("Spawn Markers")]
    [Tooltip("create an empty object as fireplayer spawn point, and drag it here")]
    [SerializeField] private Transform fireboySpawnPoint;

    [Tooltip("create an empty object as waterplayer spawn point, and drag it here")]
    [SerializeField] private Transform watergirlSpawnPoint;

    private void Start()
    {
        if (playerPrefab == null || gameManager == null || 
            fireboySpawnPoint == null || watergirlSpawnPoint == null)
        {
            Debug.LogError("TutorialPlayerSpawner is missing necessary references. Please assign all fields in the Inspector.", this);
            return;
        }

        SpawnPlayer(fireboySpawnPoint.position, PlayerRole.Fireboy);
        SpawnPlayer(watergirlSpawnPoint.position, PlayerRole.Watergirl);
    }

    private void SpawnPlayer(Vector3 spawnPosition, PlayerRole role)
    {
        GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerObject.name = role.ToString();
        
        // (注意：原始脚本中的其余配置逻辑应该复制到这里)
        // 确保你的玩家有正确的物理配置等。
        
        // 示例：从你的原始脚本中复制过来的逻辑
        EnsurePhysicsConfiguration(playerObject);

        CoopPlayerController controller = playerObject.GetComponent<CoopPlayerController>();
        if (controller == null)
        {
            controller = playerObject.AddComponent<CoopPlayerController>();
        }
        controller.Initialize(role, gameManager);
    }

    // (确保你也复制了原始脚本中的这个辅助方法)
    private static void EnsurePhysicsConfiguration(GameObject playerObject)
    {
        if (!playerObject.TryGetComponent(out Rigidbody2D body))
        {
            body = playerObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        playerObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        if (!playerObject.TryGetComponent(out Collider2D collider))
        {
            collider = playerObject.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = false;

        if (collider is BoxCollider2D boxCollider)
        {
            boxCollider.size = new Vector2(0.65f, 0.65f);
        }
    }
}