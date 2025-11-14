using System;
using System.Collections;
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
    [SerializeField] private Vector2 fireCannonPositionOffset = Vector2.zero;

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TokenPlacementManager tokenPlacementManager;

    [Header("Hazard Outlines")]
    [SerializeField] private bool showSequenceOutlines = true;
    [SerializeField] private Color fireOutlineColor = new Color(0.95f, 0.45f, 0.15f, 0.9f);
    [SerializeField] private Color iceOutlineColor = new Color(0.4f, 0.75f, 1f, 0.9f);
    [SerializeField, Min(0.001f)] private float outlineWidth = 0.05f;

    private static readonly string[] Layout =
    {
        "#########################",
        "##...#.#.#.............##",
        "##...#.#.#.#.#.#.#.#....#",
        "#....#...#.#.#.#.#.#...##",
        "##.....#...#.#.#.#.#...##",
        "##...#.#.#.#.#.#.#.#...##",
        "##...#.###.#...#.#.#...F#",
        "#....#...#.#.#.#.#.#...##",
        "##...#.#.#.#.#.#.#.#...##",
        "##...#W#.#.#.#...#.#...##",
        "##222#.#.....#.#...#111##",
        "#########################"
   
    };

    private const string FireSpawnName = "FireboySpawn";
    private const string WaterSpawnName = "WatergirlSpawn";

    private static readonly Vector2Int FireSpawnCell = FindSpawnCellInLayout('F');
    private static readonly Vector2Int WaterSpawnCell = FindSpawnCellInLayout('W');

    private static readonly Vector2Int CenterExitCell = new Vector2Int(7, 6);

    private static readonly SequenceDefinition[] TriggerSequences =
    {
        new SequenceDefinition(
            "FireIcePair_(11,1)-(10,2)",
            new[]
            {
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(11, 1)),
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(10, 2))
            },
            loop: true
        ),
        new SequenceDefinition(
            "IceFirePair_(15,1)-(14,3)",
            new[]
            {
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(15, 1)),
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(14, 3))
            },
            loop: true
        ),
        new SequenceDefinition(
            "IceFirePair_(9,4)-(10,5)",
            new[]
            {
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(9, 4)),
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(10, 5))
            },
            loop: true
        ),
        new SequenceDefinition(
            "FireIcePair_(7,7)-(9,10)",
            new[]
            {
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(7, 7)),
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(9, 10))
            },
            loop: true
        ),
        new SequenceDefinition(
            "FireIcePair_(14,7)-(15,9)",
            new[]
            {
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(14, 7)),
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(15, 9))
            },
            loop: true
        ),
        new SequenceDefinition(
            "IceFirePair_(19,1)-(18,9)",
            new[]
            {
                new SequenceStep(SequenceActionType.Ice, new Vector2Int(19, 1)),
                new SequenceStep(SequenceActionType.Fire, new Vector2Int(18, 9))
            },
            loop: true
        )
    };

    private readonly Dictionary<Vector2Int, Vector2> _cellOrigins = new Dictionary<Vector2Int, Vector2>();
    private readonly HashSet<Vector2Int> _sequenceReservedCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, SequenceActionType> _sequenceCellTypes = new Dictionary<Vector2Int, SequenceActionType>();
    private readonly Dictionary<Vector2Int, GameObject> _hazardOutlines = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<int, SequenceState> _sequenceStates = new Dictionary<int, SequenceState>();
    private readonly Dictionary<int, Coroutine> _pendingSpawnCoroutines = new Dictionary<int, Coroutine>();
    private bool _tearingDown;
    private bool _exitPlaced;
    private Material _outlineMaterial;
    private Vector2 _fireOutlineOffset = Vector2.zero;
    private Vector2 _iceOutlineOffset = Vector2.zero;
    private Vector2 _fireOutlineSize = Vector2.one;
    private Vector2 _iceOutlineSize = Vector2.one;

    private void Start()
    {
        if (!IsValidCell(FireSpawnCell))
        {
            Debug.LogWarning("Level3Manager: Could not find 'F' spawn marker in layout; Fireboy spawn may be missing.", this);
        }

        if (!IsValidCell(WaterSpawnCell))
        {
            Debug.LogWarning("Level3Manager: Could not find 'W' spawn marker in layout; Watergirl spawn may be missing.", this);
        }

        CacheSequenceReservedCells();
        CacheHazardOutlineData();
        BuildMaze(Layout);
        CreateAdditionalSpawnMarkers();
        PlaceCentralExit();
        InitializeSequences();
        CenterMaze(Layout);
        tokenPlacementManager?.SpawnTokens();
        DisableComingSoonBanner();
        gameManager?.OnLevelReady();
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

        for (int y = 0; y < layout.Length; y++)
        {
            string row = layout[y];
            for (int x = 0; x < row.Length; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector2 cellPosition = GetCellPosition(gridPosition);
                _cellOrigins[gridPosition] = cellPosition;

                bool reservedForSequence = _sequenceReservedCells.Contains(gridPosition);
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
                        if (!reservedForSequence)
                        {
                            SpawnIceWall(cellPosition);
                        }
                        break;

                    case 'F':
                        SpawnFloor(cellPosition);
                        if (gridPosition == FireSpawnCell)
                        {
                            CreateSpawnMarker(cellPosition, FireSpawnName);
                            break;
                        }

                        if (!reservedForSequence)
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

                    default:
                        SpawnFloor(cellPosition);
                        break;
                }

                // Additional spawn marker for Fireboy if the layout cell is the chosen coordinate.
                if (gridPosition == FireSpawnCell && GameObject.Find(FireSpawnName) == null)
                {
                    CreateSpawnMarker(cellPosition, FireSpawnName);
                }

                if (reservedForSequence)
                {
                    EnsureHazardOutline(gridPosition, cellPosition);
                }
            }
        }
    }

    private void CreateAdditionalSpawnMarkers()
    {
        if (IsValidCell(FireSpawnCell) && GameObject.Find(FireSpawnName) == null && _cellOrigins.TryGetValue(FireSpawnCell, out Vector2 fireOrigin))
        {
            CreateSpawnMarker(fireOrigin, FireSpawnName);
        }

        if (IsValidCell(WaterSpawnCell) && GameObject.Find(WaterSpawnName) == null && _cellOrigins.TryGetValue(WaterSpawnCell, out Vector2 waterOrigin))
        {
            CreateSpawnMarker(waterOrigin, WaterSpawnName);
        }
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
                Debug.LogWarning("Level3Manager: Unable to find a shader for hazard outlines.", this);
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

    private void PlaceCentralExit()
    {
        if (_exitPlaced) return;

        if (_cellOrigins.TryGetValue(CenterExitCell, out Vector2 origin))
        {
            SpawnExit(origin);
        }
        else
        {
            Debug.LogWarning($"Level3Manager: Unable to place central exit; missing cell {CenterExitCell}.", this);
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

            if (!_cellOrigins.TryGetValue(step.Cell, out Vector2 origin))
            {
                Debug.LogWarning($"Level3Manager: Missing cell origin for sequence step at {step.Cell}.", this);
                SetHazardOutlineVisible(step.Cell, true);
                state.CurrentIndex++;
                continue;
            }

            if (IsPlayerBlockingCell(origin))
            {
                if (!_pendingSpawnCoroutines.ContainsKey(sequenceId))
                {
                    _pendingSpawnCoroutines[sequenceId] = StartCoroutine(WaitForSequenceCellClear(sequenceId, step.Cell));
                }
                _sequenceStates[sequenceId] = state;
                return;
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
                    Debug.LogWarning($"Level3Manager: Unsupported sequence action {step.ActionType}.", this);
                    state.CurrentIndex++;
                    continue;
            }

            if (spawned == null)
            {
                Debug.LogWarning($"Level3Manager: Failed to spawn {step.ActionType} for sequence '{state.Definition.Name}'.", this);
                SetHazardOutlineVisible(step.Cell, true);
                state.CurrentIndex++;
                continue;
            }

            SetHazardOutlineVisible(step.Cell, false);
            AttachSequenceMember(spawned, sequenceId);
            state.ActiveObject = spawned;
            Debug.Log($"Level3Manager: Sequence '{state.Definition.Name}' spawned {step.ActionType} at cell {step.Cell}.", this);
            break;
        }

        _sequenceStates[sequenceId] = state;
    }

    internal void NotifySequenceHazardCleared(int sequenceId)
    {
        if (_tearingDown) return;
        if (!_sequenceStates.TryGetValue(sequenceId, out SequenceState state)) return;

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

    private IEnumerator WaitForSequenceCellClear(int sequenceId, Vector2Int cell)
    {
        while (!_tearingDown)
        {
            if (!_cellOrigins.TryGetValue(cell, out Vector2 origin))
            {
                break;
            }

            if (!IsPlayerBlockingCell(origin))
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

    private bool IsPlayerBlockingCell(Vector2 origin)
    {
        Vector2 center = new Vector2(
            origin.x + 0.5f * cellSize,
            origin.y - 0.5f * cellSize
        );

        Vector2 size = new Vector2(cellSize * 0.8f, cellSize * 0.8f);
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D collider in overlaps)
        {
            if (collider != null && collider.GetComponent<CoopPlayerController>() != null)
            {
                return true;
            }
        }

        return false;
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

    private void DisableComingSoonBanner()
    {
        GameObject banner = GameObject.Find("ComingSoonText");
        if (banner != null)
        {
            banner.SetActive(false);
        }
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
            Debug.LogWarning("Level3Manager: Fire wall prefab is not assigned.", this);
            return null;
        }

        GameObject fireWall = Instantiate(fireWallPrefab, position, Quaternion.identity, transform);
        fireWall.layer = LayerMask.NameToLayer("Wall");
        return fireWall;
    }

    private GameObject SpawnExit(Vector2 position)
    {
        Vector3 center = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );

        if (_exitPlaced)
        {
            return null;
        }

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

        _exitPlaced = true;
        return exit;
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

    private void SpawnCannon(Vector2 position, CannonVariant variant)
    {
        Vector3 worldPosition = new Vector3(
            position.x + 0.5f * cellSize,
            position.y - 0.5f * cellSize,
            0f
        );
        Vector2 offsetUnits = fireCannonPositionOffset;
        if (offsetUnits.sqrMagnitude > 0f)
        {
            worldPosition += new Vector3(offsetUnits.x * cellSize, offsetUnits.y * cellSize, 0f);
        }

        GameObject selectedPrefab = variant == CannonVariant.Fire ? fireCannonPrefab : iceCannonPrefab;
        if (selectedPrefab == null)
        {
            selectedPrefab = cannonPrefab;
        }

        Quaternion spawnRotation = Quaternion.identity;

        GameObject cannon = selectedPrefab != null
            ? Instantiate(selectedPrefab, worldPosition, spawnRotation, transform)
            : new GameObject(variant == CannonVariant.Fire ? "FireCannon" : "IceCannon");

        if (cannon.transform.parent != transform)
        {
            cannon.transform.SetParent(transform);
        }

        cannon.transform.position = worldPosition;
        cannon.transform.rotation = spawnRotation;

        if (!cannon.TryGetComponent(out CannonHazard hazard))
        {
            hazard = cannon.AddComponent<CannonHazard>();
        }

        GameObject projectileOverride = variant == CannonVariant.Fire ? fireProjectilePrefab : iceProjectilePrefab;
        if (projectileOverride == null)
        {
            projectileOverride = cannonProjectilePrefab;
        }

        GameObject hitEffectOverride = variant == CannonVariant.Fire ? fireHitEffectPrefab : iceHitEffectPrefab;
        if (hitEffectOverride == null)
        {
            hitEffectOverride = cannonHitEffectPrefab;
        }

        hazard.Initialize(gameManager, cellSize, variant, projectileOverride, hitEffectOverride);
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

    private static Vector2Int FindSpawnCellInLayout(char marker)
    {
        for (int y = 0; y < Layout.Length; y++)
        {
            string row = Layout[y];
            int x = row.IndexOf(marker);
            if (x >= 0)
            {
                return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private static bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0;
    }
}
