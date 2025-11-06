using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Level3Manager : MonoBehaviour
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

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TokenPlacementManager tokenPlacementManager;

    private static readonly string[] Layout =
    {
        "###########################",
        "#..............#..#......F#",
        "#...#....#######..#.......#",
        "#...#....#.....#..#.###...#",
        "#...#....#.....#..#.#.....#",
        "#...#....#.....#..###.#####",
        "#...#....#..........#.#...#",
        "#...#.........####..#.#...#",
        "#...###########..#..#.#...#",
        "#.............#..#..#.#...#",
        "#.............#.....#.#...#",
        "#.............#.....#.#...#",
        "#...###.......#...........#",
        "#.....#..........#........#",
        "#W....#..........#........#",
        "###########################"
    };

    private static readonly ChainPairDefinition[] ChainPairs =
    {
        new ChainPairDefinition(new Vector2Int(8, 3), new Vector2Int(9, 7)),
        new ChainPairDefinition(new Vector2Int(11, 5), new Vector2Int(13, 5)),
        new ChainPairDefinition(new Vector2Int(9, 11), new Vector2Int(5, 13))
    };

    private const string FireSpawnName = "FireboySpawn";
    private const string WaterSpawnName = "WatergirlSpawn";

    private readonly Dictionary<Vector2Int, Vector2> _cellOrigins = new Dictionary<Vector2Int, Vector2>();

    private void Start()
    {
        BuildMaze(Layout);
        SetupChainPairs();
        CenterMaze(Layout);
        tokenPlacementManager?.SpawnTokens();
        gameManager?.OnLevelReady();
    }

    private void BuildMaze(string[] layout)
    {
        if (layout == null || layout.Length == 0) return;

        HashSet<Vector2Int> chainIceCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> chainFireCells = new HashSet<Vector2Int>();

        foreach (ChainPairDefinition definition in ChainPairs)
        {
            chainIceCells.Add(definition.Ice);
            chainFireCells.Add(definition.Fire);
        }

        for (int y = 0; y < layout.Length; y++)
        {
            string row = layout[y];
            for (int x = 0; x < row.Length; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector2 cellPosition = GetCellPosition(gridPosition);
                _cellOrigins[gridPosition] = cellPosition;

                char cell = row[x];
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

                    case 'E':
                        SpawnFloor(cellPosition);
                        SpawnExit(cellPosition);
                        break;

                    case 'I':
                        SpawnFloor(cellPosition);
                        if (!chainIceCells.Contains(gridPosition))
                        {
                            SpawnIceWall(cellPosition);
                        }
                        break;

                    case 'F':
                        SpawnFloor(cellPosition);
                        if (!chainFireCells.Contains(gridPosition))
                        {
                            SpawnFireWall(cellPosition);
                        }
                        break;

                    default:
                        SpawnFloor(cellPosition);
                        break;
                }
            }
        }
    }

    private void SetupChainPairs()
    {
        if (iceWallPrefab == null || fireWallPrefab == null) return;

        foreach (ChainPairDefinition definition in ChainPairs)
        {
            if (!_cellOrigins.TryGetValue(definition.Ice, out Vector2 icePosition)) continue;
            if (!_cellOrigins.TryGetValue(definition.Fire, out Vector2 firePosition)) continue;

            GameObject pairObject = new GameObject($"ChainWallPair_{definition.Ice.x}_{definition.Ice.y}");
            pairObject.transform.SetParent(transform);

            ChainWallPair pair = pairObject.AddComponent<ChainWallPair>();
            pair.Initialize(
                icePosition,
                firePosition,
                SpawnIceWall,
                SpawnFireWall
            );
        }
    }

    private Vector2 GetCellPosition(Vector2Int gridPosition)
    {
        return new Vector2(gridPosition.x * cellSize, -gridPosition.y * cellSize);
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

    private GameObject SpawnIceWall(Vector2 position)
    {
        GameObject prefab = iceWallPrefab != null ? iceWallPrefab : wallPrefab;
        if (prefab == null) return null;

        GameObject iceWall = Instantiate(prefab, position, Quaternion.identity, transform);
        iceWall.layer = LayerMask.NameToLayer("Wall");

        if (!iceWall.TryGetComponent(out IceWall component))
        {
            component = iceWall.AddComponent<IceWall>();
        }

        return iceWall;
    }

    private GameObject SpawnFireWall(Vector2 position)
    {
        if (fireWallPrefab == null)
        {
            Debug.LogWarning("Fire wall prefab is not assigned.");
            return null;
        }

        GameObject fireWall = Instantiate(fireWallPrefab, position, Quaternion.identity, transform);
        fireWall.layer = LayerMask.NameToLayer("Wall");
        return fireWall;
    }

    private void SpawnExit(Vector2 position)
    {
        Vector3 center = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );

        GameObject exit = exitPrefab != null
            ? Instantiate(exitPrefab, center, Quaternion.identity, transform)
            : CreateFallbackExit(center);

        if (exit.TryGetComponent(out ExitZone exitZone))
        {
            exitZone.Initialize(gameManager);
        }
        else
        {
            exitZone = exit.AddComponent<ExitZone>();
            exitZone.Initialize(gameManager);
        }
    }

    private GameObject CreateFallbackExit(Vector3 center)
    {
        GameObject exit = new GameObject("Exit");
        exit.transform.SetParent(transform);
        exit.transform.position = center;

        BoxCollider2D trigger = exit.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 1.8f, cellSize * 1.4f);
        trigger.offset = Vector2.zero;

        SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.9f, 0.8f, 0.2f, 0.85f);
        renderer.sortingOrder = 4;

        GameObject label = new GameObject("Label");
        label.transform.SetParent(exit.transform);
        label.transform.localPosition = new Vector3(0f, 0.85f * cellSize, -0.1f);

        TextMeshPro tmp = label.AddComponent<TextMeshPro>();
        tmp.text = "EXIT";
        tmp.color = new Color(238f / 255f, 221f / 255f, 130f / 255f, 150f / 255f);
        tmp.fontSize = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;

        ExitZone exitZone = exit.AddComponent<ExitZone>();
        exitZone.Initialize(gameManager);

        return exit;
    }

    private void CreateSpawnMarker(Vector2 position, string name)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );
        marker.transform.SetParent(transform);
    }

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

        if (Camera.main != null)
        {
            Camera.main.transform.position = center;

            float verticalSize = (height / 2f) + 1f;
            float horizontalSize = ((width / 2f) + 1f) / Camera.main.aspect;
            Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }
    }

    private readonly struct ChainPairDefinition
    {
        public Vector2Int Ice { get; }
        public Vector2Int Fire { get; }

        public ChainPairDefinition(Vector2Int ice, Vector2Int fire)
        {
            Ice = ice;
            Fire = fire;
        }
    }
}
