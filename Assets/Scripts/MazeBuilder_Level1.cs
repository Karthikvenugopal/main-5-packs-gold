using UnityEngine;

public class MazeBuilder_Level1 : MonoBehaviour
{
    [Header("Maze Settings")]
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    [Header("Ingredient Prefabs")]
    public GameObject chiliPickupPrefab;
    public GameObject butterPickupPrefab;
    public GameObject breadPickupPrefab;
    [Header("Enemies")]
    public GameObject rollingPinPrefab;
    [Header("Obstacle Prefabs")]
    public GameObject iceWallPrefab;
    public GameObject stickyZonePrefab;
    public GameObject waterPatchPrefab;
    [Header("Ability Durations")]
    public float chiliDurationSeconds = 0f;
    public float butterDurationSeconds = 6f;

    [Header("Dependencies")]
    public GameManager gameManager;

    void Start()
    {
        string[] maze =
        {   
            "#########################",
            "#S#######################",
            "#............#####......#",
            "##..######.........#...B#",
            "#........#.^.####..#.^.##",
            "#...R....#.....##.......#",
            "#.......####~~~~~~~#....#",
            "#.......#...~~~~~~~#....#",
            "######.###..####...#....#",
            "#.....W............#....#",
            "#########..#R.....#######",
            "#..........######......##",
            "#.B.....#..###......#####",
            "#####~~~~~~.#..######..##",
            "#....~~~~~~..W.#.......##",
            "#......#####.....########",
            "#......#.......P........#",
            "#######################E#"
        };

        BuildMaze(maze);
        
        int ingredientCount = GameObject.FindGameObjectsWithTag("Ingredient").Length;

        if (gameManager != null)
        {
            gameManager.StartLevel(ingredientCount);
        }
    }

    void BuildMaze(string[] layout)
    {
        for (int y = 0; y < layout.Length; y++)
        {
            string line = layout[y];
            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];
                Vector2 pos = new Vector2(x * cellSize, -y * cellSize);

                switch (c)
                {
                    case '#':
                        SpawnWall(pos);
                        break;

                    case 'S':
                        SpawnFloor(pos);
                        CreateSpawnMarker(pos);
                        break;

                    case 'C':
                        SpawnFloor(pos);
                        SpawnIngredient(pos, IngredientType.Chili, chiliDurationSeconds);
                        break;

                    case 'B':
                        SpawnFloor(pos);
                        SpawnIngredient(pos, IngredientType.Butter, butterDurationSeconds);
                        break;

                    case 'I':
                        SpawnFloor(pos);
                        SpawnIceWall(pos);
                        break;

                    case 'W':
                        SpawnFloor(pos);
                        SpawnWaterPatch(pos);
                        break;

                    case 'R': 
                        SpawnFloor(pos);
                        SpawnIngredient(pos, IngredientType.Bread, 0f);
                        break;

                    case '~':
                        SpawnFloor(pos);
                        SpawnStickyZone(pos);
                        break;

                    case 'E':
                        SpawnFloor(pos);
                        SpawnExit(pos);
                        break;

                    case 'P': //move right
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.right);
                        break;
                    case 'p': //move left
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.left);
                        break;
                    case '^'://move up
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.up);
                        break;
                    case 'v'://move down
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.down);
                        break;

                    case '.':
                    case ' ':
                    default:
                        SpawnFloor(pos);
                        break;
                }
            }
        }
        CenterMaze(layout);
    }

    void SpawnWall(Vector2 position)
    {
        if (wallPrefab == null) return;
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, transform);
        wall.layer = LayerMask.NameToLayer("Wall");
    }

    void SpawnFloor(Vector2 position)
    {
        if (floorPrefab == null) return;
        Instantiate(floorPrefab, position, Quaternion.identity, transform);
    }

    void SpawnExit(Vector2 position)
    {
        GameObject exit = new GameObject("Exit");

        exit.transform.position = position + new Vector2(0.6f, -0.6f);  
        exit.transform.SetParent(transform);

        BoxCollider2D col = exit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        col.size = new Vector2(0.6f, 0.6f);
        col.offset = Vector2.zero;

        exit.tag = "Exit";
        exit.AddComponent<ExitTrigger>();

        SpriteRenderer sr = exit.AddComponent<SpriteRenderer>();
        sr.color = new Color(0f, 1f, 0f, 0.25f);
        sr.sortingOrder = 5;
    }




    void SpawnIngredient(Vector2 position, IngredientType type, float durationSeconds)
    {
        GameObject prefab = null;

        switch (type)
        {
            case IngredientType.Chili:
                prefab = chiliPickupPrefab;
                break;

            case IngredientType.Butter:
                prefab = butterPickupPrefab;
                break;

            case IngredientType.Bread:
                prefab = breadPickupPrefab;
                break;
        }

        GameObject ingredient = prefab != null
            ? Instantiate(prefab, position, Quaternion.identity, transform)
            : CreateRuntimeIngredient(type, position);

        ingredient.tag = "Ingredient";

        ConfigureIngredientObject(ingredient, type);

        if (!ingredient.TryGetComponent(out IngredientPickup pickup))
        {
            pickup = ingredient.AddComponent<IngredientPickup>();
        }

        pickup.Configure(type, durationSeconds);
    }
    void SpawnRollingPin(Vector2 position, Vector2 direction)
    {
        if (rollingPinPrefab == null) return;
        GameObject pin = Instantiate(rollingPinPrefab, position, Quaternion.identity, transform);
        RollingPinEnemy enemy = pin.GetComponent<RollingPinEnemy>();
        if (enemy != null) enemy.SetInitialDirection(direction);
    }




    void SpawnWaterPatch(Vector2 position)
    {
        GameObject source = waterPatchPrefab != null ? waterPatchPrefab : wallPrefab;
        if (source == null) return;

        GameObject water = Instantiate(source, position + Vector2.down * 0.55f, Quaternion.identity, transform);


        if (water.TryGetComponent(out SpriteRenderer sr))
        {
            sr.color = new Color(0.4f, 0.6f, 1f, 0.75f);
        }

        if (!water.TryGetComponent(out WaterPatch wp))
        {
            wp = water.AddComponent<WaterPatch>();
        }

        water.layer = LayerMask.NameToLayer("Wall");
    }


    GameObject CreateRuntimeIngredient(IngredientType type, Vector2 position)
    {
        GameObject ingredient = new GameObject($"{type}Pickup");
        ingredient.transform.SetParent(transform);
        ingredient.transform.position = position;

        ingredient.AddComponent<SpriteRenderer>();

        return ingredient;
    }

    void ConfigureIngredientObject(GameObject ingredient, IngredientType type)
    {
        if (ingredient == null) return;

        SpriteRenderer sr = ingredient.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = ingredient.GetComponentInChildren<SpriteRenderer>();
        }

        if (sr != null)
        {
            sr.sortingOrder = 1;

            Sprite customSprite = IngredientVisualFactory.GetSprite(type);
            if (customSprite != null)
            {
                sr.sprite = customSprite;
                sr.color = Color.white;
            }
            else if (floorPrefab != null && floorPrefab.TryGetComponent(out SpriteRenderer floorSR))
            {
                sr.sprite = floorSR.sprite;
                sr.color = type switch
                {
                    IngredientType.Chili => new Color(0.88f, 0.24f, 0.16f, 1f),
                    IngredientType.Butter => new Color(0.99f, 0.91f, 0.47f, 1f),
                    IngredientType.Bread => new Color(0.74f, 0.47f, 0.27f, 1f),
                    IngredientType.Garlic => new Color(0.9f, 0.92f, 0.82f, 1f),
                    IngredientType.Chocolate => new Color(0.46f, 0.28f, 0.16f, 1f),
                    _ => sr.color
                };
            }
        }

        float pickupScale = Mathf.Max(0.1f, cellSize * 1.2f);
        Vector3 targetScale = IngredientVisualFactory.GetScale(type, pickupScale);
        ingredient.transform.localScale = targetScale;

        CircleCollider2D circleCollider = ingredient.GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = ingredient.AddComponent<CircleCollider2D>();
        }

        circleCollider.isTrigger = true;

        float maxScale = Mathf.Max(targetScale.x, targetScale.y);
        float minScale = Mathf.Min(targetScale.x, targetScale.y);
        if (maxScale > 0f)
        {
            float desiredWorldRadius = minScale * 0.5f;
            circleCollider.radius = desiredWorldRadius / maxScale;
        }
    }

    void SpawnIceWall(Vector2 position)
    {
        GameObject source = iceWallPrefab != null ? iceWallPrefab : wallPrefab;
        if (source == null) return;

        GameObject ice = Instantiate(source, position, Quaternion.identity, transform);

        if (ice.TryGetComponent(out SpriteRenderer sr))
        {
            sr.color = new Color(0.62f, 0.84f, 1f, 1f);
        }

        if (!ice.TryGetComponent(out IceWall iceWall))
        {
            iceWall = ice.AddComponent<IceWall>();
        }

        ice.layer = LayerMask.NameToLayer("Wall");
    }

    void SpawnStickyZone(Vector2 position)
    {
        GameObject zone = stickyZonePrefab != null ? Instantiate(stickyZonePrefab, position, Quaternion.identity, transform) : CreateRuntimeStickyZone(position);
        if (!zone.TryGetComponent<StickyZone>(out _))
        {
            zone.AddComponent<StickyZone>();
        }

        float zoneScale = Mathf.Max(0.1f, cellSize);
        zone.transform.localScale = new Vector3(zoneScale, zoneScale, 1f);
    }

    GameObject CreateRuntimeStickyZone(Vector2 position)
    {
        GameObject zone = new GameObject("StickyZone");
        zone.transform.SetParent(transform);
        zone.transform.position = position;

        SpriteRenderer sr = zone.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;

        if (floorPrefab != null && floorPrefab.TryGetComponent(out SpriteRenderer floorSR))
        {
            sr.sprite = floorSR.sprite;
        }

        sr.color = new Color(0.98f, 0.8f, 0.2f, 0.65f);

        BoxCollider2D collider = zone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        return zone;
    }

    void CreateSpawnMarker(Vector2 position)
    {
        GameObject spawnMarker = new GameObject("PlayerSpawn");
        spawnMarker.transform.position = position;
        spawnMarker.transform.SetParent(transform);
    }

    void CenterMaze(string[] layout)
    {
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
