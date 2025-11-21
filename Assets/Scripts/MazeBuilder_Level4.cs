using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Template maze builder for Level 4.
/// Replace the Layout array with your ASCII maze and the builder will spawn walls,
/// floors, hazards, cannons, player spawns, and the exit automatically.
/// </summary>
public class MazeBuilder_Level4 : MonoBehaviour, IPairedHazardManager
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

    [Header("Paired Hazards")]
    [SerializeField] private bool showPairedHazardOutlines = true;
    [SerializeField] private Color fireOutlineColor = new Color(0.95f, 0.45f, 0.15f, 0.9f);
    [SerializeField] private Color iceOutlineColor = new Color(0.4f, 0.75f, 1f, 0.9f);
    [SerializeField, Min(0.001f)] private float outlineWidth = 0.05f;

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
        "################################", // row 0
        "#F##..............#............#", // row 1
        "#.##.#####.####.###.######.###.#", // row 2
        "#............##.....######...#.#", // row 3
        "##.#.############.##########.#.#", // row 4
        "#W.#.###...######.##.....#...#.#", // row 5
        "####.###.#.######.##.#.#.#.#.#.#", // row 6
        "####.....#.####...##.#.#.#.#.#.#", // row 7
        "#....#.###.####.####.#.#.#.#.#.#", // row 8
        "##########......###..........#.#", // row 9
        "###################.##########.#", // row 10
        "#################...##########.#", // row 11
        "#..################...########.#", // row 12
        "#E......###########.#.########.#", // row 13
        "#######....#####....#..........#", // row 14
        "##########.......#####.#######.#", // row 15
        "################################"  // row 16
    };

    private static readonly PairedHazardDefinition[] PairedHazardConfigurations =
    {
        new PairedHazardDefinition(
            new Vector2Int(4, 3),
            PairedHazardType.Ice,
            new Vector2Int(12, 1),
            PairedHazardType.Fire),
        new PairedHazardDefinition(
            new Vector2Int(7, 7),
            PairedHazardType.Ice,
            new Vector2Int(10, 7),
            PairedHazardType.Fire),
        new PairedHazardDefinition(
            new Vector2Int(27, 5),
            PairedHazardType.Fire,
            new Vector2Int(26, 6),
            PairedHazardType.Ice),
        new PairedHazardDefinition(
            new Vector2Int(26, 6),
            PairedHazardType.Fire,
            new Vector2Int(24, 6),
            PairedHazardType.Ice),
        new PairedHazardDefinition(
            new Vector2Int(24, 6),
            PairedHazardType.Fire,
            new Vector2Int(22, 6),
            PairedHazardType.Ice),
        new PairedHazardDefinition(
            new Vector2Int(20, 9),
            PairedHazardType.Fire,
            new Vector2Int(19, 10),
            PairedHazardType.Ice),
        new PairedHazardDefinition(
            new Vector2Int(28, 9),
            PairedHazardType.Fire,
            new Vector2Int(23, 9),
            PairedHazardType.Ice)
    };

    private readonly List<PairedHazardState> _pairedHazardStates = new List<PairedHazardState>();
    private readonly Dictionary<Vector2Int, PairedCellReservation> _pairedCellLookup = new Dictionary<Vector2Int, PairedCellReservation>();
    private Material _outlineMaterial;
    private Vector2 _fireOutlineOffset = Vector2.zero;
    private Vector2 _iceOutlineOffset = Vector2.zero;
    private Vector2 _fireOutlineSize = Vector2.one;
    private Vector2 _iceOutlineSize = Vector2.one;
    private bool _tearingDown;

    private const string FireSpawnName = "FireboySpawn";
    private const string WaterSpawnName = "WatergirlSpawn";

    private void Start()
    {
        PreparePairedHazards();
        CacheHazardOutlineData();
        BuildMaze(Layout);
        InitializePairedHazards();
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
                Vector2Int gridPosition = new Vector2Int(x, y);
                bool reservedForPair = _pairedCellLookup.ContainsKey(gridPosition);

                if (reservedForPair && cell == '#')
                {
                    Debug.LogWarning($"MazeBuilder_Level4: Reserved paired hazard cell {gridPosition} is marked as a wall in the layout. Converting it to a floor so the hazard can spawn.", this);
                    SpawnFloor(cellPosition);
                    continue;
                }

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
                        if (!reservedForPair)
                        {
                            SpawnIceWall(cellPosition);
                        }
                        break;

                    case 'H':
                        SpawnFloor(cellPosition);
                        if (!reservedForPair)
                        {
                            SpawnFireWall(cellPosition);
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
        if (prefab == null)
        {
            Debug.LogWarning("MazeBuilder_Level4: Fire wall prefab is not assigned.");
            return null;
        }

        GameObject fireWall = Instantiate(prefab, position, Quaternion.identity, transform);
        fireWall.layer = LayerMask.NameToLayer("Wall");
        return fireWall;
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

    private void PreparePairedHazards()
    {
        _pairedCellLookup.Clear();
        _pairedHazardStates.Clear();

        if (PairedHazardConfigurations == null || PairedHazardConfigurations.Length == 0 || Layout == null || Layout.Length == 0)
        {
            return;
        }

        for (int i = 0; i < PairedHazardConfigurations.Length; i++)
        {
            PairedHazardDefinition definition = PairedHazardConfigurations[i];
            if (!IsCellWithinLayout(definition.firstCell) || !IsCellWithinLayout(definition.secondCell))
            {
                Debug.LogWarning($"MazeBuilder_Level4: Ignoring paired hazard entry #{i} because one of the cells is outside the layout bounds.", this);
                continue;
            }

            if (_pairedCellLookup.ContainsKey(definition.firstCell) || _pairedCellLookup.ContainsKey(definition.secondCell))
            {
                Debug.LogWarning($"MazeBuilder_Level4: Cells {definition.firstCell} or {definition.secondCell} are already reserved for another paired hazard. Skipping duplicate entry #{i}.", this);
                continue;
            }

            HazardCell first = new HazardCell(definition.firstCell, GetCellPosition(definition.firstCell), definition.firstType);
            HazardCell second = new HazardCell(definition.secondCell, GetCellPosition(definition.secondCell), definition.secondType);

            PairedHazardState state = new PairedHazardState(first, second);
            int index = _pairedHazardStates.Count;
            _pairedHazardStates.Add(state);

            _pairedCellLookup[first.Cell] = new PairedCellReservation(index, PairedHazardSlot.First);
            _pairedCellLookup[second.Cell] = new PairedCellReservation(index, PairedHazardSlot.Second);
        }
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

    private void InitializePairedHazards()
    {
        if (_pairedHazardStates.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _pairedHazardStates.Count; i++)
        {
            PairedHazardState state = _pairedHazardStates[i];
            state.FirstOutline = CreateHazardOutline(state.First);
            state.SecondOutline = CreateHazardOutline(state.Second);

            GameObject spawned = SpawnPairedHazard(i, PairedHazardSlot.First);
            state.ActiveSlot = PairedHazardSlot.First;
            state.ActiveObject = spawned;

            UpdateOutlineVisibility(state);
        }
    }

    private GameObject SpawnPairedHazard(int pairIndex, PairedHazardSlot slot)
    {
        if (!TryGetPairedState(pairIndex, out PairedHazardState state))
        {
            return null;
        }

        HazardCell target = slot == PairedHazardSlot.First ? state.First : state.Second;
        GameObject spawned = target.Type == PairedHazardType.Fire
            ? SpawnFireWall(target.Origin)
            : SpawnIceWall(target.Origin);

        if (spawned == null)
        {
            Debug.LogWarning($"MazeBuilder_Level4: Failed to spawn {(target.Type == PairedHazardType.Fire ? "fire" : "ice")} wall for paired hazard #{pairIndex} at cell {target.Cell}.", this);
            return null;
        }

        AttachPairedHazardMember(spawned, pairIndex, slot);
        return spawned;
    }

    private void AttachPairedHazardMember(GameObject hazard, int pairIndex, PairedHazardSlot slot)
    {
        if (hazard == null) return;

        PairedHazardMember member = hazard.GetComponent<PairedHazardMember>();
        if (member == null)
        {
            member = hazard.AddComponent<PairedHazardMember>();
        }

        member.Initialize(this, pairIndex, slot);
    }

    private GameObject CreateHazardOutline(HazardCell cell)
    {
        if (!showPairedHazardOutlines)
        {
            return null;
        }

        GameObject outline = new GameObject($"PairedHazardOutline_{cell.Cell.x}_{cell.Cell.y}");
        outline.transform.SetParent(transform);

        Vector2 offset = cell.Type == PairedHazardType.Fire ? _fireOutlineOffset : _iceOutlineOffset;
        Vector2 size = cell.Type == PairedHazardType.Fire ? _fireOutlineSize : _iceOutlineSize;

        outline.transform.position = new Vector3(
            cell.Origin.x + offset.x,
            cell.Origin.y + offset.y,
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

        Color outlineColor = cell.Type == PairedHazardType.Fire ? fireOutlineColor : iceOutlineColor;
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

        outline.SetActive(showPairedHazardOutlines);
        return outline;
    }

    private void UpdateOutlineVisibility(PairedHazardState state)
    {
        if (!showPairedHazardOutlines)
        {
            if (state.FirstOutline != null) state.FirstOutline.SetActive(false);
            if (state.SecondOutline != null) state.SecondOutline.SetActive(false);
            return;
        }

        bool firstActive = state.ActiveSlot == PairedHazardSlot.First && state.ActiveObject != null;
        if (state.FirstOutline != null)
        {
            state.FirstOutline.SetActive(!firstActive);
        }

        bool secondActive = state.ActiveSlot == PairedHazardSlot.Second && state.ActiveObject != null;
        if (state.SecondOutline != null)
        {
            state.SecondOutline.SetActive(!secondActive);
        }
    }

    private Material GetOutlineMaterial()
    {
        if (_outlineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("MazeBuilder_Level4: Unable to find a shader for paired hazard outlines.", this);
                return null;
            }

            _outlineMaterial = new Material(shader);
        }

        return _outlineMaterial;
    }

    public void NotifyPairedHazardCleared(int pairIndex, PairedHazardSlot slot)
    {
        if (_tearingDown) return;
        if (!TryGetPairedState(pairIndex, out PairedHazardState state)) return;
        if (state.ActiveSlot != slot)
        {
            return;
        }

        state.ActiveObject = null;
        PairedHazardSlot nextSlot = slot == PairedHazardSlot.First ? PairedHazardSlot.Second : PairedHazardSlot.First;

        GameObject spawned = SpawnPairedHazard(pairIndex, nextSlot);
        state.ActiveSlot = nextSlot;
        state.ActiveObject = spawned;

        UpdateOutlineVisibility(state);
    }

    private bool TryGetPairedState(int index, out PairedHazardState state)
    {
        if (index >= 0 && index < _pairedHazardStates.Count)
        {
            state = _pairedHazardStates[index];
            return state != null;
        }

        state = null;
        return false;
    }

    private Vector2 GetCellPosition(Vector2Int gridPosition)
    {
        return new Vector2(gridPosition.x * cellSize, -gridPosition.y * cellSize);
    }

    private bool IsCellWithinLayout(Vector2Int cell)
    {
        if (Layout == null || Layout.Length == 0) return false;
        if (cell.y < 0 || cell.y >= Layout.Length) return false;

        string row = Layout[cell.y];
        if (string.IsNullOrEmpty(row)) return false;

        return cell.x >= 0 && cell.x < row.Length;
    }

    private void OnDisable()
    {
        _tearingDown = true;
    }

    private void OnDestroy()
    {
        _tearingDown = true;
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

    [System.Serializable]
    private struct PairedHazardDefinition
    {
        public PairedHazardDefinition(Vector2Int firstCell, PairedHazardType firstType, Vector2Int secondCell, PairedHazardType secondType)
        {
            this.firstCell = firstCell;
            this.firstType = firstType;
            this.secondCell = secondCell;
            this.secondType = secondType;
        }

        [Tooltip("Grid coordinate (x,y) of the wall that appears first. (0,0) is the top-left of the layout.")]
        public Vector2Int firstCell;
        [Tooltip("Type of the wall spawned at the first coordinate.")]
        public PairedHazardType firstType;
        [Tooltip("Grid coordinate (x,y) of the wall that appears after the first is destroyed.")]
        public Vector2Int secondCell;
        [Tooltip("Type of the wall spawned at the second coordinate.")]
        public PairedHazardType secondType;
    }

    private class PairedHazardState
    {
        public PairedHazardState(HazardCell first, HazardCell second)
        {
            First = first;
            Second = second;
            ActiveSlot = PairedHazardSlot.First;
        }

        public HazardCell First { get; }
        public HazardCell Second { get; }
        public PairedHazardSlot ActiveSlot { get; set; }
        public GameObject ActiveObject { get; set; }
        public GameObject FirstOutline { get; set; }
        public GameObject SecondOutline { get; set; }
    }

    private readonly struct HazardCell
    {
        public HazardCell(Vector2Int cell, Vector2 origin, PairedHazardType type)
        {
            Cell = cell;
            Origin = origin;
            Type = type;
        }

        public Vector2Int Cell { get; }
        public Vector2 Origin { get; }
        public PairedHazardType Type { get; }
    }

    private readonly struct PairedCellReservation
    {
        public PairedCellReservation(int pairIndex, PairedHazardSlot slot)
        {
            PairIndex = pairIndex;
            Slot = slot;
        }

        public int PairIndex { get; }
        public PairedHazardSlot Slot { get; }
    }
}
