using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Lightweight level controller that simply builds the maze layout defined below.
/// This mirrors the behaviour of the Level1 builder so the layout can be iterated quickly.
/// </summary>
public class Level5Manager : MonoBehaviour, ISequentialHazardManager
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
    [Header("Steam Components")]
    [SerializeField] private GameObject steamAreaPrefab;
    [SerializeField] private GameObject steamWallPrefab;
    [Header("Tokens")]
    [SerializeField] private GameObject fireTokenPrefab;
    [SerializeField] private GameObject waterTokenPrefab;

    [Header("Hazard Outlines")]
    [SerializeField] private bool showSequenceOutlines = true;
    [SerializeField] private Color fireOutlineColor = new Color(0.95f, 0.45f, 0.15f, 0.9f);
    [SerializeField] private Color iceOutlineColor = new Color(0.4f, 0.75f, 1f, 0.9f);
    [SerializeField, Min(0.001f)] private float outlineWidth = 0.05f;

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

        // "########################",
        // "#S...#MF..........LLLLE#",
        // "#.##.#F##I##I###########",
        // "#.##.#I##F##F###########",
        // "#.##.#........I.......##",
        // "#.##F################.##",
        // "#F##.#.............##.##",
        // "#..................##F##",
        // "##.....#I#####.....##.##",
        // "##2222.#.....I........##",
        // "########.######....##.##",
        // "#W.F.....######111.##.##",
        // "########################"

        "##################",
        "#f..I.4###f..wF.S#",
        "#Iw##fF###FI######",
        "#.I##Fw###.w.Ff.4#",
        "#..II..FMI..######",
        "#F#II#I###..Iw.Ff#",
        "#.#w.#f###.#######",
        "#W#11#....1#######",
        "######LLLLL#######",
        "######LLLLL#######",
        "#######.E.########",
        "##################"
    };

    private const string FireSpawnName = "FireboySpawn";
    private const string WaterSpawnName = "WatergirlSpawn";

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    private static readonly SequenceDefinition[] TriggerSequences =
    {
        new SequenceDefinition(
            "FireIcePair_(21,7)-(14,5)",
            new[]
            {
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(21, 7)),
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(14, 5))
            },
            loop: true
        )
    };

    private readonly Dictionary<Vector2Int, Vector2> _cellCenters = new Dictionary<Vector2Int, Vector2>();
    private readonly HashSet<Vector2Int> _sequenceReservedCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, SequenceActionType> _sequenceCellTypes = new Dictionary<Vector2Int, SequenceActionType>();
    private readonly Dictionary<Vector2Int, GameObject> _hazardOutlines = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<int, SequenceState> _sequenceStates = new Dictionary<int, SequenceState>();
    private readonly Dictionary<int, Coroutine> _pendingSpawnCoroutines = new Dictionary<int, Coroutine>();
    private bool _tearingDown;
    private Material _outlineMaterial;
    private Vector2 _fireOutlineOffset = Vector2.zero;
    private Vector2 _iceOutlineOffset = Vector2.zero;
    private Vector2 _fireOutlineSize = Vector2.one;
    private Vector2 _iceOutlineSize = Vector2.one;
    private bool _exitPlaced;
    private float _swapCooldown = 0f;
    private const float SwapCooldownDuration = 0.5f; // Prevent rapid swapping
    private int _swapCount = 0;
    private const int MaxSwaps = 4;

    private void Start()
    {
        CacheHazardOutlineData();
        CacheSequenceReservedCells();
        BuildMaze(Layout);
        CenterMaze(Layout);
        // InitializeSequences(); // Temporarily disabled
        // tokenPlacementManager?.SpawnTokens(); // Disabled during Level 5 maze design pass
        
        // Recount tokens after maze is built to update the UI counter
        // Use a coroutine to ensure all tokens are fully instantiated
        StartCoroutine(RecountTokensAfterBuild());
        
        gameManager?.OnLevelReady();
        UpdateSwapCounterUI();
    }

    private IEnumerator RecountTokensAfterBuild()
    {
        // Wait one frame to ensure all tokens are fully instantiated
        yield return null;
        
        // Recount tokens so the UI counter shows the correct total
        if (gameManager != null)
        {
            gameManager.RecountTokensInScene();
        }
    }

    private void Update()
    {
        // Only allow position swapping in Level 5
        if (!IsLevel5())
        {
            return;
        }

        // Update cooldown timer
        if (_swapCooldown > 0f)
        {
            _swapCooldown -= Time.deltaTime;
        }

        // Check for space bar press
        if (Input.GetKeyDown(KeyCode.Space) && _swapCooldown <= 0f)
        {
            if (_swapCount < MaxSwaps)
            {
                SwapPlayerPositions();
                _swapCooldown = SwapCooldownDuration;
            }
            else
            {
                Debug.Log("Level5Manager: Maximum swap limit reached!");
                UpdateSwapCounterUI();
            }
        }
    }

    private bool IsLevel5()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string sceneLower = currentScene.ToLowerInvariant();
        return sceneLower == "level5scene" || sceneLower == "level5";
    }

    private void SwapPlayerPositions()
    {
        // Find both players
        CoopPlayerController fireboy = null;
        CoopPlayerController watergirl = null;

        CoopPlayerController[] players = FindObjectsOfType<CoopPlayerController>();
        
        foreach (var player in players)
        {
            if (player == null) continue;
            
            if (player.Role == PlayerRole.Fireboy)
            {
                fireboy = player;
            }
            else if (player.Role == PlayerRole.Watergirl)
            {
                watergirl = player;
            }
        }

        // Only swap if both players are found
        if (fireboy == null || watergirl == null)
        {
            Debug.LogWarning("Level5Manager: Cannot swap positions - one or both players not found.");
            return;
        }

        // Store positions
        Vector3 fireboyPosition = fireboy.transform.position;
        Vector3 watergirlPosition = watergirl.transform.position;

        // Swap positions
        fireboy.transform.position = watergirlPosition;
        watergirl.transform.position = fireboyPosition;

        _swapCount++;
        Debug.Log($"Level5Manager: Player positions swapped! ({_swapCount}/{MaxSwaps} swaps used)");
        UpdateSwapCounterUI();
    }

    private void UpdateSwapCounterUI()
    {
        if (gameManager == null)
        {
            return;
        }

        int remainingSwaps = Mathf.Max(0, MaxSwaps - _swapCount);
        gameManager.UpdateSwapCounterDisplay(remainingSwaps, MaxSwaps);
    }

    private void CacheHazardOutlineData()
    {
        _fireOutlineOffset = Vector2.zero;
        _iceOutlineOffset = Vector2.zero;
        _fireOutlineSize = Vector2.one * cellSize;
        _iceOutlineSize = Vector2.one * cellSize;

        PopulateOutlineDataForPrefab(fireWallPrefab, ref _fireOutlineOffset, ref _fireOutlineSize);
        PopulateOutlineDataForPrefab(iceWallPrefab, ref _iceOutlineOffset, ref _iceOutlineSize);
    }

    private void PopulateOutlineDataForPrefab(GameObject prefab, ref Vector2 offset, ref Vector2 size)
    {
        if (prefab == null) return;

        SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.sprite != null ? renderer.sprite.bounds : renderer.bounds;
            offset = (Vector2)bounds.center;
            size = (Vector2)(bounds.extents * 2f);
        }
        else
        {
            offset = new Vector2(0.5f * cellSize, -0.5f * cellSize);
            size = new Vector2(cellSize, cellSize);
        }
    }

    private void CacheSequenceReservedCells()
    {
        _sequenceReservedCells.Clear();
        _sequenceCellTypes.Clear();

        foreach (SequenceDefinition definition in TriggerSequences)
        {
            foreach (SequenceStep step in definition.Steps)
            {
                if (step.ActionType == SequenceActionType.Ice || step.ActionType == SequenceActionType.Fire)
                {
                    _sequenceReservedCells.Add(step.Cell);
                    _sequenceCellTypes[step.Cell] = step.ActionType;
                }
            }
        }
    }

    private void BuildMaze(string[] layout)
    {
        if (layout == null || layout.Length == 0) return;

        for (int row = 0; row < layout.Length; row++)
        {
            string line = layout[row];
            for (int col = 0; col < line.Length; col++)
            {
                Vector2Int gridPosition = new Vector2Int(col, row);
                Vector2 cellPosition = GetCellPosition(gridPosition);
                _cellCenters[gridPosition] = cellPosition;

                bool reservedForSequence = _sequenceReservedCells.Contains(gridPosition);
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
                        if (!reservedForSequence)
                        {
                            SpawnIceWall(cellPosition);
                        }
                        break;

                    case 'F':
                        SpawnFloor(cellPosition);
                        // Always spawn static firewalls in the layout, sequences are currently disabled
                        GameObject fireWall = SpawnFireWall(cellPosition);
                        if (fireWall == null)
                        {
                            Debug.LogWarning($"Level5Manager: Failed to spawn firewall at grid position ({col}, {row}). fireWallPrefab may be null.", this);
                        }
                        break;

                    case '1':
                        SpawnFloor(cellPosition);
                        SpawnCannon(cellPosition, CannonVariant.Fire);
                        break;

                    case '2':
                        SpawnFloor(cellPosition);
                        SpawnCannon(cellPosition, CannonVariant.Ice);
                        break;

                    case '4':
                        SpawnFloor(cellPosition);
                        SpawnLevel4Cannon(cellPosition, CannonVariant.Ice);
                        break;

                    case 'E':
                        SpawnFloor(cellPosition);
                        SpawnExit(cellPosition);
                        break;

                    case 'M':       // steam area
                        SpawnFloor(cellPosition);
                        SpawnSteamArea(cellPosition);
                        break;

                    case 'L':       // steam wall
                        SpawnFloor(cellPosition);
                        SpawnSteamWall(cellPosition);
                        break;

                    case 'f':       // fire token
                        SpawnFloor(cellPosition);
                        SpawnFireToken(cellPosition);
                        break;

                    case 'w':       // water token
                        SpawnFloor(cellPosition);
                        SpawnWaterToken(cellPosition);
                        break;

                    default:
                        SpawnFloor(cellPosition);
                        break;
                }

                // if (reservedForSequence)
                // {
                //     EnsureHazardOutline(gridPosition, cellPosition);
                // }
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
        GameObject prefab = fireWallPrefab != null ? fireWallPrefab : wallPrefab;
        if (prefab == null) return null;

        GameObject fireWall = Instantiate(prefab, position, Quaternion.identity, transform);
        fireWall.layer = LayerMask.NameToLayer("Wall");

        if (!fireWall.TryGetComponent(out FireWall component))
        {
            component = fireWall.AddComponent<FireWall>();
        }

        return fireWall;
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
        trigger.size = new Vector2(cellSize * 1.8f, cellSize * 1.4f);
        trigger.offset = Vector2.zero;

        SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.9f, 0.8f, 0.2f, 0.85f);
        renderer.sortingOrder = 4;

        GameObject text = new GameObject("Label");
        text.transform.SetParent(exit.transform);
        text.transform.localPosition = new Vector3(0f, 0.85f * cellSize, -0.1f);

        TextMeshPro tmp = text.AddComponent<TextMeshPro>();
        tmp.text = "EXIT";
        tmp.color = new Color(238f / 255f, 221f / 255f, 130f / 255f, 150f / 255f);
        tmp.fontSize = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;

        return exit;
    }

    private void SpawnCannon(Vector2 position, CannonVariant variant)
    {
        // Position cannon at the bottom of the cell (center - 0.5 * cellSize) so it fires upward
        // This aligns it properly with the row and allows projectiles to travel up the column
        Vector3 worldPosition = new(
            position.x,
            position.y - 0.5f * cellSize,  // Move down to bottom of cell for upward firing
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

    private void SpawnLevel4Cannon(Vector2 position, CannonVariant variant)
    {
        // Move half a tile up: position.y is top of cell, -0.5 is center. 
        // "Half a tile up" from center (-0.5) is 0.0 (top of cell).
        // Or simply add 0.5f * cellSize to the previous center calculation.
        Vector3 worldPosition = new Vector3(
            position.x + 0.5f * cellSize,
            position.y, // Was position.y - 0.5f * cellSize
            0f
        );

        GameObject selectedPrefab = variant == CannonVariant.Fire ? fireCannonPrefab : iceCannonPrefab;
        if (selectedPrefab == null)
        {
            selectedPrefab = cannonPrefab;
        }

        // Rotate 90 degrees to the left (Counter-Clockwise)
        Quaternion rotation = Quaternion.Euler(0, 0, 90);

        GameObject cannon = selectedPrefab != null
            ? Instantiate(selectedPrefab, worldPosition, rotation, transform)
            : new GameObject(variant == CannonVariant.Fire ? "FireCannon" : "IceCannon");

        if (cannon.transform.parent != transform)
        {
            cannon.transform.SetParent(transform);
        }

        cannon.transform.position = worldPosition;
        cannon.transform.rotation = rotation;

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
            position.x,
            position.y,
            0f
        );
    }

    private void SpawnSteamArea(Vector2 position)
    {
        if (steamAreaPrefab == null)
        {
            Debug.LogWarning("Level5Manager: Steam area prefab is not assigned.", this);
            return;
        }

        Vector3 worldPosition = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );

        GameObject area = Instantiate(steamAreaPrefab, worldPosition, Quaternion.identity, transform);

        // Scale down the steam area to fit better within the cell size
        area.transform.localScale = new Vector3(cellSize * 0.75f, cellSize * 0.75f, 1f);

        if (area.TryGetComponent(out SpriteRenderer renderer))
        {
            renderer.sortingOrder = 2;          // Above walls
            renderer.color = new Color(0.85f, 0.65f, 0.95f, 0.45f);
        }
    }

    private GameObject SpawnSteamWall(Vector2 position)
    {
        if (steamWallPrefab == null)
        {
            Debug.LogWarning("Level5Manager: Steam wall prefab is not assigned.", this);
            return null;
        }

        GameObject wall = Instantiate(steamWallPrefab, position, Quaternion.identity, transform);
        wall.layer = LayerMask.NameToLayer("Wall");   // Make it block like a normal wall

        if (wall.TryGetComponent(out SpriteRenderer renderer))
        {
            renderer.sortingOrder = 1;                // Same layer as normal walls
        }

        return wall;
    }

    private void SpawnFireToken(Vector2 position)
    {
        if (fireTokenPrefab == null)
        {
            Debug.LogWarning("Level5Manager: Fire token prefab is not assigned.", this);
            return;
        }

        Instantiate(fireTokenPrefab, position, Quaternion.identity, transform);
    }

    private void SpawnWaterToken(Vector2 position)
    {
        if (waterTokenPrefab == null)
        {
            Debug.LogWarning("Level5Manager: Water token prefab is not assigned.", this);
            return;
        }

        Instantiate(waterTokenPrefab, position, Quaternion.identity, transform);
    }

    private Vector2 GetCellPosition(Vector2Int gridPosition)
    {
        float worldX = (gridPosition.x + 0.5f) * cellSize;
        float worldY = -(gridPosition.y + 0.5f) * cellSize;
        return new Vector2(worldX, worldY);
    }

    private void EnsureHazardOutline(Vector2Int cell, Vector2 cellPosition)
    {
        if (!showSequenceOutlines || _hazardOutlines.ContainsKey(cell)) return;
        if (!_sequenceCellTypes.TryGetValue(cell, out SequenceActionType type)) return;

        GameObject outline = new GameObject($"HazardOutline_{cell.x}_{cell.y}");
        outline.transform.SetParent(transform);
        Vector2 offset = type == SequenceActionType.Fire ? _fireOutlineOffset : _iceOutlineOffset;
        Vector2 size = type == SequenceActionType.Fire ? _fireOutlineSize : _iceOutlineSize;
        outline.transform.position = new Vector3(
            cellPosition.x + offset.x,
            cellPosition.y + offset.y,
            0f
        );

        LineRenderer renderer = outline.AddComponent<LineRenderer>();
        renderer.useWorldSpace = false;
        renderer.loop = true;
        renderer.positionCount = 5;
        renderer.startWidth = renderer.endWidth = Mathf.Max(0.001f, outlineWidth);
        Material material = GetOutlineMaterial();
        if (material != null)
        {
            renderer.material = material;
        }
        Color outlineColor = type == SequenceActionType.Fire ? fireOutlineColor : iceOutlineColor;
        renderer.startColor = renderer.endColor = outlineColor;
        renderer.sortingOrder = 4;

        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        Vector3[] corners =
        {
            new Vector3(-halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f)
        };
        renderer.SetPositions(corners);

        _hazardOutlines[cell] = outline;
    }

    private Material GetOutlineMaterial()
    {
        if (_outlineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("Level5Manager: Unable to find a shader for hazard outlines.", this);
                return null;
            }

            _outlineMaterial = new Material(shader);
        }

        return _outlineMaterial;
    }

    private void SetHazardOutlineVisible(Vector2Int cell, bool visible)
    {
        if (_hazardOutlines.TryGetValue(cell, out GameObject outline))
        {
            outline.SetActive(visible && showSequenceOutlines);
        }
    }

    private void InitializeSequences()
    {
        _sequenceStates.Clear();

        for (int i = 0; i < TriggerSequences.Length; i++)
        {
            SequenceDefinition definition = TriggerSequences[i];
            _sequenceStates[i] = new SequenceState(definition);
            SpawnNextSequenceStep(i);
        }
    }

    private void SpawnNextSequenceStep(int sequenceId)
    {
        if (!_sequenceStates.TryGetValue(sequenceId, out SequenceState state)) return;

        while (true)
        {
            if (state.CurrentIndex >= state.Definition.Steps.Length)
            {
                if (state.Definition.Loop)
                {
                    state.CurrentIndex = 0;
                }
                else
                {
                    break;
                }
            }

            SequenceStep step = state.Definition.Steps[state.CurrentIndex];

            if (!_cellCenters.TryGetValue(step.Cell, out Vector2 origin))
            {
                Debug.LogWarning($"Level5Manager: Missing cell origin for sequence step at {step.Cell}.", this);
                SetHazardOutlineVisible(step.Cell, true);
                state.CurrentIndex++;
                continue;
            }

            if (IsPlayerBlockingCell(origin))
            {
                bool displaced = TryDisplaceBlockingPlayers(step.Cell, step.ActionType);
                if (!displaced || IsPlayerBlockingCell(origin))
                {
                    if (!_pendingSpawnCoroutines.ContainsKey(sequenceId))
                    {
                        _pendingSpawnCoroutines[sequenceId] = StartCoroutine(WaitForSequenceCellClear(sequenceId, step.Cell));
                    }
                    _sequenceStates[sequenceId] = state;
                    return;
                }
            }

            GameObject spawned;
            switch (step.ActionType)
            {
                case SequenceActionType.Ice:
                    spawned = SpawnIceWall(origin);
                    break;

                case SequenceActionType.Fire:
                    spawned = SpawnFireWall(origin);
                    break;

                default:
                    Debug.LogWarning($"Level5Manager: Unsupported sequence action {step.ActionType}.", this);
                    state.CurrentIndex++;
                    continue;
            }

            if (spawned == null)
            {
                Debug.LogWarning($"Level5Manager: Failed to spawn {step.ActionType} for sequence '{state.Definition.Name}'.", this);
                SetHazardOutlineVisible(step.Cell, true);
                state.CurrentIndex++;
                continue;
            }

            // Hide outline before attaching member to prevent visual glitch
            SetHazardOutlineVisible(step.Cell, false);
            AttachSequenceMember(spawned, sequenceId);
            state.ActiveObject = spawned;
            _sequenceStates[sequenceId] = state;
            Debug.Log($"Level5Manager: Sequence '{state.Definition.Name}' spawned {step.ActionType} at cell {step.Cell}.", this);
            return;
        }

        // Save state if we exit the loop
        _sequenceStates[sequenceId] = state;
    }

    public void NotifySequenceHazardCleared(int sequenceId)
    {
        if (_tearingDown) return;
        if (!_sequenceStates.TryGetValue(sequenceId, out SequenceState state)) return;

        // Don't process if we're already waiting for a cell to clear
        if (_pendingSpawnCoroutines.ContainsKey(sequenceId))
        {
            return;
        }

        // Ensure we have a valid active object before clearing
        if (state.ActiveObject == null)
        {
            return;
        }

        int clearedIndex = state.CurrentIndex;
        if (clearedIndex < state.Definition.Steps.Length)
        {
            Vector2Int clearedCell = state.Definition.Steps[clearedIndex].Cell;
            SetHazardOutlineVisible(clearedCell, true);
        }

        state.ActiveObject = null;
        state.CurrentIndex++;
        _sequenceStates[sequenceId] = state;
        SpawnNextSequenceStep(sequenceId);
    }

    private void AttachSequenceMember(GameObject target, int sequenceId)
    {
        if (target == null) return;

        SequentialHazardMember member = target.GetComponent<SequentialHazardMember>();
        if (member == null)
        {
            member = target.AddComponent<SequentialHazardMember>();
        }

        member.Initialize(this, sequenceId);
    }

    private IEnumerator WaitForSequenceCellClear(int sequenceId, Vector2Int cell)
    {
        while (!_tearingDown)
        {
            if (!_cellCenters.TryGetValue(cell, out Vector2 origin))
            {
                break;
            }

            if (!IsPlayerBlockingCell(origin))
            {
                break;
            }

            SequenceActionType? actionType = _sequenceCellTypes.TryGetValue(cell, out SequenceActionType typeFromMap)
                ? typeFromMap
                : (SequenceActionType?)null;

            if (TryDisplaceBlockingPlayers(cell, actionType) && !IsPlayerBlockingCell(origin))
            {
                break;
            }

            yield return null;
        }

        _pendingSpawnCoroutines.Remove(sequenceId);

        if (!_tearingDown)
        {
            SpawnNextSequenceStep(sequenceId);
        }
    }

    private bool IsPlayerBlockingCell(Vector2 cellCenter)
    {
        Vector2 size = new Vector2(cellSize * 0.8f, cellSize * 0.8f);
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(cellCenter, size, 0f);

        foreach (Collider2D collider in overlaps)
        {
            if (collider != null && collider.GetComponent<CoopPlayerController>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryDisplaceBlockingPlayers(Vector2Int blockingCell, SequenceActionType? actionType = null)
    {
        if (!_cellCenters.ContainsKey(blockingCell))
        {
            return false;
        }

        Vector3 center = GetCellCenter(blockingCell);
        Vector2 size = new Vector2(cellSize * 0.8f, cellSize * 0.8f);
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, 0f);
        bool displacedAny = false;

        foreach (Collider2D collider in overlaps)
        {
            if (collider == null) continue;
            CoopPlayerController player = collider.GetComponent<CoopPlayerController>();
            if (player == null) continue;

            if (!ShouldDisplacePlayer(actionType, player))
            {
                continue;
            }

            if (TryFindSafeDisplacementPosition(blockingCell, player.transform.position, out Vector3 destination))
            {
                TeleportPlayer(player, destination);
                displacedAny = true;
            }
        }

        return displacedAny;
    }

    private bool ShouldDisplacePlayer(SequenceActionType? actionType, CoopPlayerController player)
    {
        if (!actionType.HasValue || player == null)
        {
            return true;
        }

        return actionType switch
        {
            SequenceActionType.Fire => player.Role == PlayerRole.Watergirl,
            SequenceActionType.Ice => player.Role == PlayerRole.Fireboy,
            _ => true
        };
    }

    private bool TryFindSafeDisplacementPosition(Vector2Int blockingCell, Vector3 playerPosition, out Vector3 destination)
    {
        destination = Vector3.zero;
        Vector3 blockedCenter = GetCellCenter(blockingCell);
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { blockingCell };

        foreach (Vector2Int direction in GetDirectionalPriority(playerPosition, blockedCenter))
        {
            Vector2Int neighbor = blockingCell + direction;
            if (visited.Add(neighbor))
            {
                frontier.Enqueue(neighbor);
            }
        }

        while (frontier.Count > 0)
        {
            Vector2Int candidate = frontier.Dequeue();

            if (!IsCellWithinLayoutBounds(candidate))
            {
                continue;
            }

            if (!IsCellWalkable(candidate))
            {
                continue;
            }

            if (_sequenceReservedCells.Contains(candidate))
            {
                continue;
            }

            if (HasBreathingRoom(candidate, blockingCell))
            {
                destination = GetCellCenter(candidate);
                return true;
            }

            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int next = candidate + direction;
                if (visited.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        return false;
    }

    private IEnumerable<Vector2Int> GetDirectionalPriority(Vector3 playerPosition, Vector3 blockedCenter)
    {
        List<Vector2Int> ordered = new List<Vector2Int>(CardinalDirections.Length);
        Vector2 offset = playerPosition - blockedCenter;

        if (offset.sqrMagnitude < 0.0001f)
        {
            ordered.AddRange(CardinalDirections);
            return ordered;
        }

        bool favorHorizontal = Mathf.Abs(offset.x) >= Mathf.Abs(offset.y);
        Vector2Int primary = favorHorizontal
            ? (offset.x >= 0f ? Vector2Int.right : Vector2Int.left)
            : (offset.y >= 0f ? Vector2Int.down : Vector2Int.up);
        Vector2Int secondary = favorHorizontal
            ? (offset.y >= 0f ? Vector2Int.down : Vector2Int.up)
            : (offset.x >= 0f ? Vector2Int.right : Vector2Int.left);
        Vector2Int oppositePrimary = new Vector2Int(-primary.x, -primary.y);
        Vector2Int oppositeSecondary = new Vector2Int(-secondary.x, -secondary.y);

        ordered.Add(primary);
        ordered.Add(secondary);
        ordered.Add(oppositeSecondary);
        ordered.Add(oppositePrimary);

        return ordered;
    }

    private bool IsCellWithinLayoutBounds(Vector2Int cell)
    {
        if (cell.y < 0 || cell.y >= Layout.Length) return false;
        string row = Layout[cell.y];
        if (string.IsNullOrEmpty(row)) return false;
        return cell.x >= 0 && cell.x < row.Length;
    }

    private bool IsCellWalkable(Vector2Int cell)
    {
        if (!IsCellWithinLayoutBounds(cell)) return false;
        char tile = Layout[cell.y][cell.x];
        return tile != '#';
    }

    private bool HasBreathingRoom(Vector2Int cell, Vector2Int blockedCell)
    {
        foreach (Vector2Int direction in CardinalDirections)
        {
            Vector2Int neighbor = cell + direction;
            if (neighbor == blockedCell) continue;
            if (!IsCellWithinLayoutBounds(neighbor)) continue;
            if (IsCellWalkable(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetCellCenter(Vector2Int cell)
    {
        Vector2 center2D = GetCellPosition(cell);
        return new Vector3(center2D.x, center2D.y, 0f);
    }

    private void TeleportPlayer(CoopPlayerController player, Vector3 destination)
    {
        if (player == null) return;

        destination.z = player.transform.position.z;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.position = destination;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        player.transform.position = destination;
    }

    private void OnDisable()
    {
        _tearingDown = true;
        foreach (KeyValuePair<int, Coroutine> pending in _pendingSpawnCoroutines)
        {
            if (pending.Value != null)
            {
                StopCoroutine(pending.Value);
            }
        }
        _pendingSpawnCoroutines.Clear();
    }

    private void OnDestroy()
    {
        _tearingDown = true;
    }

    private readonly struct SequenceDefinition
    {
        public SequenceDefinition(string name, SequenceStep[] steps, bool loop = false)
        {
            Name = name;
            Steps = steps;
            Loop = loop;
        }

        public string Name { get; }
        public SequenceStep[] Steps { get; }
        public bool Loop { get; }
    }

    private readonly struct SequenceStep
    {
        public SequenceStep(SequenceActionType actionType, Vector2Int cell)
        {
            ActionType = actionType;
            Cell = cell;
        }

        public SequenceActionType ActionType { get; }
        public Vector2Int Cell { get; }
    }

    private struct SequenceState
    {
        public SequenceState(SequenceDefinition definition)
        {
            Definition = definition;
            CurrentIndex = 0;
            ActiveObject = null;
        }

        public SequenceDefinition Definition { get; }
        public int CurrentIndex { get; set; }
        public GameObject ActiveObject { get; set; }
    }

    private enum SequenceActionType
    {
        Ice,
        Fire,
        Exit
    }
}
