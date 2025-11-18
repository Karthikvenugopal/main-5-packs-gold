using UnityEngine;
using TMPro;

/// <summary>
/// Template maze builder for Level 4.
/// Replace the Layout array with your ASCII maze and the builder will spawn walls,
/// floors, hazards, cannons, player spawns, and the exit automatically.
/// </summary>
public class MazeBuilder_Level4 : MonoBehaviour
{
    [Header("Maze Settings")]
    [Min(0.1f)]
    [SerializeField] private float cellSize = 1f;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject iceWallPrefab;
    [SerializeField] private GameObject fireWallPrefab;
    [SerializeField] private GameObject cannonPrefab;
    [SerializeField] private GameObject fireCannonPrefab;
    [SerializeField] private GameObject iceCannonPrefab;
    [SerializeField] private GameObject cannonProjectilePrefab;
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject iceProjectilePrefab;
    [SerializeField] private GameObject cannonHitEffectPrefab;
    [SerializeField] private GameObject fireHitEffectPrefab;
    [SerializeField] private GameObject iceHitEffectPrefab;
    [SerializeField] private GameObject exitPrefab;

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TokenPlacementManager tokenPlacementManager;

    /// <summary>
    /// Replace this with the actual Level 4 ASCII maze.
    /// Legend (feel free to extend):
    /// # = Wall
    /// . = Floor
    /// F = Ember spawn
    /// W = Aqua spawn
    /// I = Ice wall (meltable by Ember)
    /// H = Fire wall (douseable by Aqua)
    /// 1 = Fire cannon
    /// 2 = Ice cannon
    /// E = Exit
    /// </summary>
    private static readonly string[] Layout =
    {
        "##########",
        "#F......W#",
        "#........#",
        "#....E...#",
        "##########"
    };

    private const string FireSpawnName = "FireboySpawn";
    private const string WaterSpawnName = "WatergirlSpawn";

    private void Start()
    {
        BuildMaze(Layout);
        CenterMaze(Layout);
        tokenPlacementManager?.SpawnTokens();
        gameManager?.OnLevelReady();
    }

    private void BuildMaze(string[] layout)
    {
        if (layout == null) return;

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

                    case 'H':
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
        if (prefab == null)
        {
            Debug.LogWarning("MazeBuilder_Level4: Fire wall prefab is not assigned.");
            return;
        }

        GameObject fireWall = Instantiate(prefab, position, Quaternion.identity, transform);
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
        if (exitPrefab != null)
        {
            Instantiate(exitPrefab, position + new Vector2(0.5f * cellSize, -0.5f * cellSize), Quaternion.identity, transform);
            return;
        }

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
}
