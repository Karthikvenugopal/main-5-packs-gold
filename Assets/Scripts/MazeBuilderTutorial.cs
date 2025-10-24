using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class MazeBuilderTutorial : MonoBehaviour
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
    [Header("Obstacle Prefabs")]
    public GameObject iceWallPrefab;
    public GameObject stickyZonePrefab;
    [FormerlySerializedAs("peanutButterPatchPrefab")] public GameObject jamPatchPrefab;
    [Header("Ability Durations")]
    public float chiliDurationSeconds = 0f;
    public float butterDurationSeconds = 12f;

    [Header("Enemies")]
    public GameObject rollingPinPrefab;

    [Header("Dependencies")]
    public GameManager gameManager;

    public Vector2 currentPlayerSpawnPoint;
    private string[] currentBuildingLayout; 

    string[] maze =
    {
        "#############",
        "#...B...~~..#",
        "#..#..#.....#",
        "#..#..#...R.#",
        "#..#I.#.#..##",
        "#..#..#.#.W##",
        "#..#..#.#...#",
        "#S..C...#.C.#",
        "#############"
    };

    
    private string[] tutorialLayout_Step1 = 
    {
        "#######",
        "#S I B#",
        "#######"
    };

    
    private string[] tutorialLayout_Step3 =
    {
        "#########",
        "#S C I B#",
        "#########"
    };

    private string[] tutorialLayout_Step5 =
    {
        "#########",
        "#      S#",
        "####### #",
        "# W     #",
        "# #######",
        "#B#######",
        "#########"
    };

    
    private string[] tutorialLayout_Step6 =
    {
        "#########",
        "#       #",
        "####### #",
        "# W  R S#",
        "# #######",
        "#B#######",
        "#########"
    };

    private string[] tutorialLayout_Step4 =
    {
    "################",
    "#........B.#.#.#",
    "#.###.######.#R#",
    "#I#.#...#.......#",
    "#.#.#~~~#..P....#",
    "#.#.#~~~#.....###",
    "#.#...###...#...#",
    "#.##........#.W.#",
    "#.C#.^.##...#...#",
    "#..#...#....#...#",
    "#.##...###..#...#",
    "#S#.........#...#",
    "#################"
    };


    
    private GameObject generatedMazeContainer;



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
                        SpawnWall(pos,x);
                        break;

                    case 'S':
                        SpawnFloor(pos);
                        CreateSpawnMarker(pos);
                        currentPlayerSpawnPoint = pos;
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
                        SpawnJamPatch(pos);
                        break;

                    case 'R': 
                        SpawnFloor(pos);
                        SpawnIngredient(pos, IngredientType.Bread, 0f);
                        break;

                    case '~':
                        SpawnFloor(pos);
                        SpawnStickyZone(pos);
                        break;

                    case 'P': 
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.right);
                        break;

                    case 'p': 
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.left);
                        break;

                    case '^': 
                        SpawnFloor(pos);
                        SpawnRollingPin(pos, Vector2.up);
                        break;

                    case 'v': 
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

    void SpawnWall(Vector2 position, int x_coordinate)
    {
        if (wallPrefab == null) return;
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, generatedMazeContainer.transform);
        if (currentBuildingLayout == tutorialLayout_Step3)
        {
            
            if (x_coordinate == currentBuildingLayout[0].Length - 1)
            {
                
                wall.tag = "RightWall";
                Debug.Log($"RightWall tag added to wall at X coordinate: {x_coordinate}"); 
            }
        }

        if(wall.TryGetComponent(out SpriteRenderer sr)) sr.sortingOrder = 0;
        wall.layer = LayerMask.NameToLayer("Wall");
    }

    void SpawnFloor(Vector2 position)
    {
        if (floorPrefab == null) return;
        GameObject floor = Instantiate(floorPrefab, position, Quaternion.identity, generatedMazeContainer.transform);
        if(floor.TryGetComponent(out SpriteRenderer sr)) sr.sortingOrder = 0;
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
            ? Instantiate(prefab, position, Quaternion.identity, generatedMazeContainer.transform)
            : CreateRuntimeIngredient(type, position);

        

        ingredient.tag = "Ingredient";

        ConfigureIngredientObject(ingredient, type);

        if (!ingredient.TryGetComponent(out IngredientPickup pickup))
        {
            pickup = ingredient.AddComponent<IngredientPickup>();
        }

        pickup.Configure(type, durationSeconds);
    }


    void SpawnJamPatch(Vector2 position)
    {
        GameObject source = jamPatchPrefab != null ? jamPatchPrefab : wallPrefab;
        if (source == null) return;

        GameObject smear = Instantiate(source, position, Quaternion.identity, generatedMazeContainer.transform);

        if (smear.TryGetComponent(out SpriteRenderer sr))
        {
            sr.color = new Color(0.75f, 0.13f, 0.28f, 0.85f);
        }

        if (!smear.TryGetComponent(out JamPatch patch))
        {
            patch = smear.AddComponent<JamPatch>();
        }

        smear.layer = LayerMask.NameToLayer("Wall");
    }

    void SpawnRollingPin(Vector2 position, Vector2 direction)
    {
        if (rollingPinPrefab == null) return;
        GameObject pin = Instantiate(rollingPinPrefab, position, Quaternion.identity, generatedMazeContainer.transform);
        RollingPinEnemy enemy = pin.GetComponent<RollingPinEnemy>();
        if (enemy != null)
        {
            enemy.SetInitialDirection(direction);
        }
    }

    GameObject CreateRuntimeIngredient(IngredientType type, Vector2 position)
    {
        GameObject ingredient = new GameObject($"{type}Pickup");
        ingredient.transform.SetParent(generatedMazeContainer.transform);
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
            sr.sortingOrder = 2;

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

        float pickupScale = Mathf.Max(0.1f, cellSize * 0.6f);
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

        GameObject ice = Instantiate(source, position, Quaternion.identity, generatedMazeContainer.transform);

        if (ice.TryGetComponent(out SpriteRenderer sr))
        {
            sr.sortingOrder = 1; 
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
        GameObject zone = stickyZonePrefab != null ? Instantiate(stickyZonePrefab, position, Quaternion.identity, generatedMazeContainer.transform) : CreateRuntimeStickyZone(position);

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
        zone.transform.SetParent(generatedMazeContainer.transform);
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
        spawnMarker.transform.SetParent(generatedMazeContainer.transform);
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

    public void ClearMaze()
    {
        if (generatedMazeContainer != null)
        {
            Debug.Log("Destroying the entire old maze container.");
            Destroy(generatedMazeContainer);
        }
    }


    
    public void BuildTutorialLevel(int step)
    {
        ClearMaze();
        string[] layoutToBuild = null;
        generatedMazeContainer = new GameObject("[GeneratedMaze]");

        if (step == 1) layoutToBuild = tutorialLayout_Step1;
        else if (step == 3) layoutToBuild = tutorialLayout_Step3;
        else if (step == 5) layoutToBuild = tutorialLayout_Step5; 
        else if (step == 6) layoutToBuild = tutorialLayout_Step6; 

        this.currentBuildingLayout = layoutToBuild;

        if (layoutToBuild != null)
        {
            BuildMaze(layoutToBuild);
            CenterMaze(layoutToBuild);
        }
    }
}
