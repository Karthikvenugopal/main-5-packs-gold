using UnityEngine;
using TMPro;
using System.Collections;

public class MazeBuilder_Level1 : MonoBehaviour
{
    [Header("Maze Settings")]
    [Min(0.1f)]
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject iceWallPrefab;
    public GameObject fireWallPrefab;

    [Header("Dependencies")]
    public GameManager gameManager;
    [SerializeField] private TokenPlacementManager tokenPlacementManager;

    private static readonly string[] Layout =
    {
        "####################",
        "#F....##..I...w...E#",
        "###.######w#####I###",
        "###f.H.###...H.w.###",
        "###.##...H.#####.###",
        "###I##.f.#...f..H.##",
        "###.##...#I#########",
        "#W.i.......#########",
        "####################"
    };

    private const float CameraVerticalPadding = 0.4f;
    [SerializeField, Tooltip("Additional downward offset applied to the camera after centering the maze.")]
    private float mazeCameraVerticalOffset = 0.45f;

    private const string FireboySpawnName = "FireboySpawn";
    private const string WatergirlSpawnName = "WatergirlSpawn";

    private void Start()
    {
        BuildMaze(Layout);
        CenterMaze(Layout);
        StartCoroutine(SpawnDialogueTriggerDelayed());
        SpawnEmberControlsDialogue();
        SpawnAquaControlsDialogue();
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
                Vector2 cellCenter = GetCellCenterPosition(x, y);

                switch (cell)
                {
                    case '#':
                        SpawnWall(cellCenter);
                        break;

                    case '.':
                        SpawnFloor(cellCenter);
                        break;

                    case 'F':
                        SpawnFloor(cellCenter);
                        CreateSpawnMarker(cellCenter, FireboySpawnName);
                        break;

                    case 'W':
                        SpawnFloor(cellCenter);
                        CreateSpawnMarker(cellCenter, WatergirlSpawnName);
                        break;

                    case 'I':
                        SpawnFloor(cellCenter);
                        SpawnIceWall(cellCenter);
                        break;

                    case 'H':
                        SpawnFloor(cellCenter);
                        SpawnFireWall(cellCenter);
                        break;

                    case 'E':
                        SpawnFloor(cellCenter);
                        SpawnExit(cellCenter);
                        break;

                    case 'w':
                        SpawnFloor(cellCenter);
                        CreateTokenAnchor(cellCenter, TokenSpriteConfigurator.TokenType.Water);
                        break;

                    case 'f':
                        SpawnFloor(cellCenter);
                        CreateTokenAnchor(cellCenter, TokenSpriteConfigurator.TokenType.Fire);
                        break;

                    default:
                        SpawnFloor(cellCenter);
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

    private void SpawnExit(Vector2 position)
    {
        // Position is already the cell center from GetCellCenterPosition
        // Shift left by half a cell to better center the 2-cell wide portal on the path
        Vector3 worldCenter = new Vector3(position.x - 0.5f * cellSize, position.y, 0f);
        
        ExitPortalFactory.CreateExitPortal(
            transform,
            worldCenter,
            cellSize,
            exitZone => exitZone.Initialize(gameManager)
        );
    }

    private void CreateSpawnMarker(Vector2 position, string name)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = position;
        marker.transform.SetParent(transform);
    }

    private void CreateTokenAnchor(Vector2 position, TokenSpriteConfigurator.TokenType tokenType)
    {
        if (tokenPlacementManager == null)
        {
            Debug.LogWarning("TokenPlacementManager is not assigned. Cannot create token anchor.");
            return;
        }

        GameObject anchorObject = new GameObject($"TokenAnchor_{tokenType}_{position.x}_{position.y}");
        anchorObject.transform.position = position;
        anchorObject.transform.SetParent(tokenPlacementManager.transform);

        TokenAnchor anchor = anchorObject.AddComponent<TokenAnchor>();
        anchor.SetTokenType(tokenType);
    }

    private IEnumerator SpawnDialogueTriggerDelayed()
    {
        // Wait 5 seconds before spawning the dialogue trigger
        yield return new WaitForSeconds(3f);
        SpawnDialogueTrigger();
    }

    private void SpawnDialogueTrigger()
    {
        // Place dialogue trigger early in the level, at row 1 (index 1), column 2
        int targetRow = 1;
        int targetCol = 2;

        if (targetRow >= Layout.Length || targetCol >= Layout[targetRow].Length)
        {
            Debug.LogWarning("Dialogue trigger position is out of bounds.");
            return;
        }

        // Calculate world position (same as BuildMaze uses)
        Vector2 worldPosition = GetCellCenterPosition(targetCol, targetRow);

        // Create trigger zone GameObject
        GameObject triggerZone = new GameObject("DialogueTriggerZone");
        triggerZone.transform.position = worldPosition;
        triggerZone.transform.SetParent(transform);

        // Add BoxCollider2D for trigger
        BoxCollider2D trigger = triggerZone.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
        trigger.offset = Vector2.zero;

        // Add DialogueTriggerZone component with custom text, position, and font size
        DialogueTriggerZone dialogueTrigger = triggerZone.AddComponent<DialogueTriggerZone>();
        dialogueTrigger.SetDialogueText("Collect tokens to boost your score");
        dialogueTrigger.SetFontSize(3.0f);

        // Set fixed dialogue position near the maze center (approximate coordinates)
        Vector2 fixedDialoguePosition = new Vector2(6.8f * cellSize, -2.5f * cellSize);
        dialogueTrigger.SetFixedPosition(fixedDialoguePosition);
        // Immediately trigger the dialogue so it appears even if players moved away from the trigger zone
        dialogueTrigger.TriggerFixedDialogue();
    }

    private void SpawnEmberControlsDialogue()
    {
        // Place dialogue trigger near the Fireboy spawn point (row 1, column 1)
        int targetRow = 1;
        int targetCol = 1;

        if (targetRow >= Layout.Length || targetCol >= Layout[targetRow].Length)
        {
            Debug.LogWarning("Ember controls dialogue trigger position is out of bounds.");
            return;
        }

        // Calculate trigger world position (same as BuildMaze uses)
        Vector2 triggerWorldPosition = GetCellCenterPosition(targetCol, targetRow) + new Vector2(1f * cellSize, 0f);

        // Calculate fixed position (slightly right and higher near the top-left corner)
        // Adjusted for centered player: moved slightly right to avoid overlap, and lowered to match player shift
        Vector2 fixedDialoguePosition = new Vector2(2.2f * cellSize, -0.6f * cellSize);

        // Create trigger zone GameObject
        GameObject triggerZone = new GameObject("EmberControlsDialogueTrigger");
        triggerZone.transform.position = triggerWorldPosition;
        triggerZone.transform.SetParent(transform);

        // Add BoxCollider2D for trigger
        BoxCollider2D trigger = triggerZone.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
        trigger.offset = Vector2.zero;

        // Add DialogueTriggerZone component with custom text and settings
        DialogueTriggerZone dialogueTrigger = triggerZone.AddComponent<DialogueTriggerZone>();
        dialogueTrigger.SetDialogueText("W/A/S/D Moves Ember");
        dialogueTrigger.SetFontSize(3f);
        // Set fixed position mode - dialogue will appear at fixed position when player enters trigger
        dialogueTrigger.SetFixedPosition(fixedDialoguePosition);
        // Immediately trigger the dialogue so it appears right away
        dialogueTrigger.TriggerFixedDialogue();
    }

    private void SpawnAquaControlsDialogue()
    {
        // Place dialogue trigger near the Watergirl spawn point (row 7, column 1)
        int targetRow = 7;
        int targetCol = 1;

        if (targetRow >= Layout.Length || targetCol >= Layout[targetRow].Length)
        {
            Debug.LogWarning("Aqua controls dialogue trigger position is out of bounds.");
            return;
        }

        // Calculate trigger world position (same as BuildMaze uses)
        Vector2 triggerWorldPosition = GetCellCenterPosition(targetCol, targetRow) + new Vector2(1f * cellSize, 0f);

        // Calculate fixed position for dialogue at bottom left corner of maze
        // Adjusted for centered player: moved slightly right to avoid overlap, and lowered to match player shift
        Vector2 fixedDialoguePosition = new Vector2(2.0f * cellSize, -8.5f * cellSize);

        // Create trigger zone GameObject
        GameObject triggerZone = new GameObject("AquaControlsDialogueTrigger");
        triggerZone.transform.position = triggerWorldPosition;
        triggerZone.transform.SetParent(transform);

        // Add BoxCollider2D for trigger
        BoxCollider2D trigger = triggerZone.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
        trigger.offset = Vector2.zero;

        // Add DialogueTriggerZone component with custom text and settings
        DialogueTriggerZone dialogueTrigger = triggerZone.AddComponent<DialogueTriggerZone>();
        dialogueTrigger.SetDialogueText("↑ ← ↓ → moved Aqua");
        dialogueTrigger.SetFontSize(3f);
        // Set fixed position mode - dialogue will appear at fixed position when player enters trigger
        dialogueTrigger.SetFixedPosition(fixedDialoguePosition);
        // Immediately trigger the dialogue so it appears right away
        dialogueTrigger.TriggerFixedDialogue();
    }

    private Vector2 GetCellCenterPosition(int x, int y)
    {
        float worldX = (x + 0.5f) * cellSize;
        float worldY = -(y + 0.5f) * cellSize;
        return new Vector2(worldX, worldY);
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
        center.y += CameraVerticalPadding;
        center.y -= Mathf.Max(0f, mazeCameraVerticalOffset);

        if (Camera.main != null)
        {
            Camera.main.transform.position = center;

            float verticalSize = (height / 2f) + 1f + CameraVerticalPadding;
            float horizontalSize = ((width / 2f) + 1f) / Camera.main.aspect;
            Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }
    }
}
