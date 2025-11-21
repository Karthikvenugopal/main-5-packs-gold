using UnityEngine;
using TMPro;

public class MazeBuilder_Level2 : MonoBehaviour
{
    [Header("Maze Settings")]
    [Min(0.1f)]
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject iceWallPrefab;
    public GameObject fireWallPrefab;
    public GameObject cannonPrefab;
    public GameObject fireCannonPrefab;
    public GameObject iceCannonPrefab;
    public GameObject cannonProjectilePrefab;
    public GameObject fireProjectilePrefab;
    public GameObject iceProjectilePrefab;
    public GameObject cannonHitEffectPrefab;
    public GameObject fireHitEffectPrefab;
    public GameObject iceHitEffectPrefab;

    [Header("Dependencies")]
    public GameManager gameManager;
    [SerializeField] private TokenPlacementManager tokenPlacementManager;

    private static readonly string[] Layout =
    {
        "###########################",
        "#F........##############.W#",
        "#.###.###I##############..#",
        "#...#.#.........I....H....#",
        "###.#.######H##########.###",
        "#.#.#.#...#..##########.###",
        "#.#.###.I...###########.###",
        "#.#.....#........H..I....##",
        "#.########H################",
        "#..I..H.....##.###.##....##",
        "#.############.###.##.....#",
        "#.........................#",
        "#.........................#",
        "#.1111..2222..1111..2222.E#",
        "###########################"
    };

    private const string FireboySpawnName = "FireboySpawn";
    private const string WatergirlSpawnName = "WatergirlSpawn";

    private void Start()
    {
        BuildMaze(Layout);
        CenterMaze(Layout);
        SpawnDialogueTrigger();
        tokenPlacementManager?.SpawnTokens();
        gameManager?.OnLevelReady();
    }

    private void BuildMaze(string[] layout)
    {
        for (int y = 0; y < layout.Length; y++)
        {
            string row = layout[y];
            for (int x = 0; x < row.Length; x++)
            {
                char cell = row[x];
                Vector2 cellPosition = new Vector2(x * cellSize, -y * cellSize);

                switch (cell)
                {
                    case '#':
                        SpawnWall(cellPosition);
                        break;

                    case '.':
                        SpawnFloor(cellPosition);
                        break;

                    case 'F':
                        SpawnFloor(cellPosition);
                        CreateSpawnMarker(cellPosition, FireboySpawnName);
                        break;

                    case 'W':
                        SpawnFloor(cellPosition);
                        CreateSpawnMarker(cellPosition, WatergirlSpawnName);
                        break;

                    case 'I':
                        SpawnFloor(cellPosition);
                        SpawnIceWall(cellPosition);
                        break;

                    case 'H':
                        SpawnFloor(cellPosition);
                        SpawnFireWall(cellPosition);
                        break;

                    case 'C':
                        SpawnFloor(cellPosition);
                        SpawnCannon(cellPosition, CannonVariant.Fire);
                        break;

                    case '1':
                        SpawnFloor(cellPosition);
                        SpawnCannon(cellPosition, CannonVariant.Fire);
                        break;

                    case '2':
                        SpawnFloor(cellPosition);
                        SpawnCannon(cellPosition, CannonVariant.Ice);
                        break;

                    case 'E':
                        SpawnFloor(cellPosition);
                        SpawnExit(cellPosition);
                        break;

                    default:
                        SpawnFloor(cellPosition);
                        break;
                }
            }
        }
    }

    private void SpawnWall(Vector2 position)
    {
        if (wallPrefab == null) return;
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, transform);
        wall.layer = LayerMask.NameToLayer("Wall");
    }

    private void SpawnFloor(Vector2 position)
    {
        if (floorPrefab == null) return;
        Instantiate(floorPrefab, position, Quaternion.identity, transform);
    }

    private void SpawnIceWall(Vector2 position)
    {
        GameObject prefab = iceWallPrefab != null ? iceWallPrefab : wallPrefab;
        if (prefab == null) return;

        GameObject iceWall = Instantiate(prefab, position, Quaternion.identity, transform);
        iceWall.layer = LayerMask.NameToLayer("Wall");

        if (!iceWall.TryGetComponent(out IceWall component))
        {
            component = iceWall.AddComponent<IceWall>();
        }
    }

    private void SpawnFireWall(Vector2 position)
    {
        if (fireWallPrefab == null)
        {
            Debug.LogWarning("Fire wall prefab is not assigned.");
            return;
        }

        GameObject fireWall = Instantiate(fireWallPrefab, position, Quaternion.identity, transform);
        fireWall.layer = LayerMask.NameToLayer("Wall");
    }

    private void SpawnCannon(Vector2 position, CannonVariant variant)
    {
        Vector3 worldPosition = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );

        GameObject selectedPrefab = variant == CannonVariant.Fire ? fireCannonPrefab : iceCannonPrefab;
        if (selectedPrefab == null)
        {
            selectedPrefab = cannonPrefab;
        }

        GameObject cannon = selectedPrefab != null
            ? Instantiate(selectedPrefab, worldPosition, Quaternion.identity, transform)
            : new GameObject(variant == CannonVariant.Fire ? "FireCannon" : "IceCannon");

        if (cannon.transform.parent != transform)
        {
            cannon.transform.SetParent(transform);
        }

        cannon.transform.position = worldPosition;

        if (!cannon.TryGetComponent(out CannonHazard hazard))
        {
            hazard = cannon.AddComponent<CannonHazard>();
        }

        GameObject selectedProjectile = variant == CannonVariant.Fire ? fireProjectilePrefab : iceProjectilePrefab;
        if (selectedProjectile == null)
        {
            selectedProjectile = cannonProjectilePrefab;
        }

        GameObject selectedHitEffect = variant == CannonVariant.Fire ? fireHitEffectPrefab : iceHitEffectPrefab;
        if (selectedHitEffect == null)
        {
            selectedHitEffect = cannonHitEffectPrefab;
        }

        hazard.Initialize(gameManager, cellSize, variant, selectedProjectile, selectedHitEffect);
    }

    private void SpawnExit(Vector2 position)
    {
        GameObject exit = new GameObject("Exit");
        exit.transform.position = position + new Vector2(0.5f * cellSize, -0.5f * cellSize);
        exit.transform.SetParent(transform);

        BoxCollider2D trigger = exit.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 2.6f, cellSize * 1.8f);
        trigger.offset = Vector2.zero;

        ExitZone exitZone = exit.AddComponent<ExitZone>();
        exitZone.Initialize(gameManager);

        SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.9f, 0.8f, 0.2f, 0.85f);
        renderer.sortingOrder = 4;

        GameObject text = new GameObject("Label");
        text.transform.SetParent(exit.transform);
        text.transform.localPosition = new Vector3(0f, 1f * cellSize, -0.1f);

        TextMeshPro tmp = text.AddComponent<TextMeshPro>();
        tmp.text = "EXIT";
        tmp.color = new Color(238f / 255f, 221f / 255f, 130f / 255f, 150f / 255f);
        tmp.fontSize = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
    }

    private void CreateSpawnMarker(Vector2 position, string name)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = position + new Vector2(0.5f * cellSize, -0.5f * cellSize);
        marker.transform.SetParent(transform);
    }

    private void SpawnDialogueTrigger()
    {
        // Row 42 is at index 11 (0-indexed), first dot is at x=1
        int targetRow = 11;
        int targetCol = 1;

        if (targetRow >= Layout.Length || targetCol >= Layout[targetRow].Length)
        {
            Debug.LogWarning("Dialogue trigger position is out of bounds.");
            return;
        }

        // Calculate world position (same as BuildMaze uses)
        Vector2 cellPosition = new Vector2(targetCol * cellSize, -targetRow * cellSize);
        Vector2 worldPosition = cellPosition + new Vector2(0.5f * cellSize, -0.5f * cellSize);

        // Create trigger zone GameObject
        GameObject triggerZone = new GameObject("DialogueTriggerZone");
        triggerZone.transform.position = worldPosition;
        triggerZone.transform.SetParent(transform);

        // Add BoxCollider2D for trigger
        BoxCollider2D trigger = triggerZone.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
        trigger.offset = Vector2.zero;

        // Add DialogueTriggerZone component
        DialogueTriggerZone dialogueTrigger = triggerZone.AddComponent<DialogueTriggerZone>();
    }

    private const float CameraVerticalPadding = 0.8f;

    private void CenterMaze(string[] layout)
    {
        if (layout == null || layout.Length == 0) return;

        float width = layout[0].Length * cellSize;
        float height = layout.Length * cellSize;

        Vector3 center = new Vector3(
            (width - cellSize) / 2f,
            -(height - cellSize) / 2f,
            -10f
        );
        center.y += CameraVerticalPadding;

        if (Camera.main != null)
        {
            Camera.main.transform.position = center;

            float verticalSize = (height / 2f) + 1f + CameraVerticalPadding;
            float horizontalSize = ((width / 2f) + 1f) / Camera.main.aspect;
            Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }
    }
}
