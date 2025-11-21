using UnityEngine;

/// <summary>
/// Lightweight level controller that simply builds the maze layout defined below.
/// This mirrors the behaviour of the Level1 builder so the layout can be iterated quickly.
/// </summary>
public class Level5Manager : MonoBehaviour
{
    [Header("Maze Settings")]
    [Min(0.1f)]
    [SerializeField] private float cellSize = 1f;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject iceWallPrefab;
    [SerializeField] private GameObject fireWallPrefab;
    [SerializeField] private GameObject exitPrefab;
    [Header("Projectile Hazards")]
    [SerializeField] private GameObject cannonPrefab;
    [SerializeField] private GameObject fireCannonPrefab;
    [SerializeField] private GameObject iceCannonPrefab;
    [SerializeField] private GameObject cannonProjectilePrefab;
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject iceProjectilePrefab;
    [SerializeField] private GameObject cannonHitEffectPrefab;
    [SerializeField] private GameObject fireHitEffectPrefab;
    [SerializeField] private GameObject iceHitEffectPrefab;

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TokenPlacementManager tokenPlacementManager;

    private static readonly string[] Layout =
    {
        // "############################",
        // "# S....#.....I......#####E1#",
        // "###.##.#.###.###.#.#.....#.#",
        // "#...##.#...#...#.#.#.F.T.#.#",
        // "#.#####.###.#.#.#.#.###.#.##",
        // "#.....#.....#.#.#.#...#.#..#",
        // "###.#.#######.#.#.###.#.####",
        // "#W..#.....F...#.#.....#E2..#",
        // "###.###########.#######.####",
        // "############################"

        "###########################",
        "#S...#....................#",
        "#.##.#.##.##.##.###########",
        "#.##.#.##.##.##.###########",
        "#.##.#..................###",
        "#.##F################...###",
        "#F##.#.............##...###",
        "#..................##...###",
        "##.....#I#####..........###",
        "##222..#.....I..........###",
        "########.######....##...###",
        "#W.I.....######111.##...###",
        "###########################"
    };

    private const string FireSpawnName = "FireboySpawn";
    private const string WaterSpawnName = "WatergirlSpawn";

    private bool _exitPlaced;

    private void Start()
    {
        BuildMaze(Layout);
        CenterMaze(Layout);
        // tokenPlacementManager?.SpawnTokens(); // Disabled during Level 5 maze design pass
        gameManager?.OnLevelReady();
    }

    private void BuildMaze(string[] layout)
    {
        if (layout == null || layout.Length == 0) return;

        for (int row = 0; row < layout.Length; row++)
        {
            string line = layout[row];
            for (int col = 0; col < line.Length; col++)
            {
                Vector2 cellPosition = new(col * cellSize, -row * cellSize);
                char cell = line[col];

                switch (cell)
                {
                    case '#':
                        SpawnWall(cellPosition);
                        break;

                    case '.':
                        SpawnFloor(cellPosition);
                        break;

                    case 'S':
                        SpawnFloor(cellPosition);
                        CreateSpawnMarker(cellPosition, FireSpawnName);
                        break;

                    case 'W':
                        SpawnFloor(cellPosition);
                        CreateSpawnMarker(cellPosition, WaterSpawnName);
                        break;

                    case 'I':
                        SpawnFloor(cellPosition);
                        SpawnIceWall(cellPosition);
                        break;

                    case 'F':
                        SpawnFloor(cellPosition);
                        SpawnFireWall(cellPosition);
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

    private void CenterMaze(string[] layout)
    {
        if (Camera.main == null || layout == null || layout.Length == 0) return;

        float width = layout[0].Length * cellSize;
        float height = layout.Length * cellSize;

        Vector3 center = new(
            (width - cellSize) * 0.5f,
            -(height - cellSize) * 0.5f,
            -10f
        );

        Camera.main.transform.position = center;

        float verticalSize = (height / 2f) + 1f;
        float horizontalSize = ((width / 2f) + 1f) / Camera.main.aspect;
        Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
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
        GameObject prefab = fireWallPrefab != null ? fireWallPrefab : wallPrefab;
        if (prefab == null) return;

        GameObject fireWall = Instantiate(prefab, position, Quaternion.identity, transform);
        fireWall.layer = LayerMask.NameToLayer("Wall");

        if (!fireWall.TryGetComponent(out FireWall component))
        {
            component = fireWall.AddComponent<FireWall>();
        }
    }

    private void SpawnExit(Vector2 position)
    {
        if (_exitPlaced) return;
        _exitPlaced = true;

        Vector3 world = new(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );

        GameObject exit = exitPrefab != null
            ? Instantiate(exitPrefab, world, Quaternion.identity, transform)
            : CreateFallbackExit(world);

        if (!exit.TryGetComponent(out ExitZone exitZone))
        {
            exitZone = exit.AddComponent<ExitZone>();
        }

        exitZone.Initialize(gameManager);
    }

    private GameObject CreateFallbackExit(Vector3 position)
    {
        GameObject exit = new("Exit");
        exit.transform.SetParent(transform);
        exit.transform.position = position;

        BoxCollider2D trigger = exit.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 1.5f, cellSize * 1.5f);

        SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.9f, 0.8f, 0.2f, 0.85f);
        renderer.sortingOrder = 4;

        return exit;
    }

    private void SpawnCannon(Vector2 position, CannonVariant variant)
    {
        Vector3 worldPosition = new(
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

    private void CreateSpawnMarker(Vector2 position, string name)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(transform);
        marker.transform.position = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );
    }
}
