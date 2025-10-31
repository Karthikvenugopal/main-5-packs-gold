using UnityEngine;
using TMPro;

public class MazeBuilder_Tutorial : MonoBehaviour
{
    [Header("Maze Settings")]
    [Min(0.1f)]
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject iceWallPrefab;
    public GameObject fireWallPrefab;
    
    // (新) 添加对教程 Prefab 的引用
    [Header("Tutorial")]
    public GameObject tutorialTriggerPrefab; 

    [Header("Dependencies")]
    public GameManager gameManager;

    // (修改) 在布局中添加了 '1' 和 '2'
    private static readonly string[] Layout =
    {
        "############",
        "#F....I...E#", 
        "###.#1#.####", 
        "#...#...H..#", 
        "###.#.##2###", 
        "#..W#......#",
        "############"
    };

    private const string FireboySpawnName = "FireboySpawn";
    private const string WatergirlSpawnName = "WatergirlSpawn";

    private void Start()
    {
        BuildMaze(Layout);
        CenterMaze(Layout);
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

                    case 'E':
                        SpawnFloor(cellPosition);
                        SpawnExit(cellPosition);
                        break;

                    case '1':
                    case '2':
                    // 你可以添加 '3', '4' 等
                        SpawnFloor(cellPosition); 
                        SpawnTutorialTrigger(cellPosition, cell);
                        break;

                    default:
                        SpawnFloor(cellPosition);
                        break;
                }
            }
        }
    }

    // (新) 用于生成教程触发器的方法
    private void SpawnTutorialTrigger(Vector2 position, char triggerId)
    {
        if (tutorialTriggerPrefab == null)
        {
            Debug.LogWarning("Tutorial Trigger Prefab is not assigned in the Inspector.");
            return;
        }

        // 调整位置，使其居中于单元格
        Vector2 spawnPosition = position + new Vector2(0.5f * cellSize, -0.5f * cellSize);
        GameObject triggerObj = Instantiate(tutorialTriggerPrefab, spawnPosition, Quaternion.identity, transform);

        // 获取触发器脚本并设置文本
        TutorialTrigger trigger = triggerObj.GetComponent<TutorialTrigger>();
        if (trigger != null)
        {
            trigger.SetText(GetTutorialText(triggerId));
        }
        else
        {
            Debug.LogError("Tutorial Trigger Prefab does not have a TutorialTrigger component.");
        }
    }

    // (新) 辅助方法，根据 ID 获取教程文本
    private string GetTutorialText(char triggerId)
    {
        switch (triggerId)
        {
            case '1':
                return "Work together. Ember melts ice.";
            case '2':
                return "Aqua extinguishes fire.";
            // 在这里为 '3', '4' 等添加更多文本
            default:
                return "Default tutorial text.";
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

    private void SpawnExit(Vector2 position)
    {
        GameObject exit = new GameObject("Exit");
        exit.transform.position = position + new Vector2(0.5f * cellSize, -0.5f * cellSize);
        exit.transform.SetParent(transform);

        BoxCollider2D trigger = exit.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 1.8f, cellSize * 1.4f);
        trigger.offset = Vector2.zero;

        ExitZone exitZone = exit.AddComponent<ExitZone>();
        exitZone.Initialize(gameManager);

        SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.9f, 0.8f, 0.2f, 0.85f);
        renderer.sortingOrder = 4;

        GameObject text = new GameObject("Label");
        text.transform.SetParent(exit.transform);
        text.transform.localPosition = new Vector3(0f, 0.85f * cellSize, -0.1f);

        TextMeshPro tmp = text.AddComponent<TextMeshPro>();
        tmp.text = "EXIT";
        tmp.color = Color.black;
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
