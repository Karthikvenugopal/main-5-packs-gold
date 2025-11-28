using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class WispEnemy : MonoBehaviour
{
    [Header("Idle Movement")]
    [SerializeField] private float hoverHeight = 0.05f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float pulseMagnitude = 0.05f;
    [SerializeField] private float baseScale = 0.6f;

    [Header("Patrol Behaviour")]
    [SerializeField] private float patrolSpeed = 2.4f;
    [SerializeField] private float waypointTolerance = 0.1f;
    [SerializeField] private LayerMask obstacleLayers; // Kept for OnTriggerEnter2D context
    [SerializeField] private float _mazeCellSize = 1.0f; 

    // --- NEW PARAMETER: Reduce the effective radius Wisp uses to check for walls ---
    [Header("Collision Tuning")]
    [Tooltip("Multiplier to shrink the Wisp's Collider radius for movement checks.")]
    [SerializeField, Range(0.1f, 1f)] private float collisionRadiusMultiplier = 0.6f;

    [Header("Grid Alignment")]
    [Tooltip("Horizontal offset inside each cell (0 = left edge, 0.5 = center).")]
    [SerializeField, Range(0f, 1f)] private float horizontalCellOffset = 0.5f;
    [Tooltip("Vertical offset inside each cell (0 = top edge, 0.5 = center).")]
    [SerializeField, Range(0f, 1f)] private float verticalCellOffset = 0.5f;

    private Vector2 _cellOffset = Vector2.zero;

    private static readonly Vector2[] PatrolGridPoints = new Vector2[]
    {
        // Grid coordinates (X, Y_row_index) based on your image
        new Vector2(1f, 5f), 
        new Vector2(2f, 5f), 
        new Vector2(2f, 3f), 
        new Vector2(4f, 3f), 
        new Vector2(4f, 7f), 
        new Vector2(8f, 7f),
        new Vector2(8f, 5f), 
        new Vector2(10f, 5f),
        new Vector2(10f, 9f),
        new Vector2(15f, 9f),
        new Vector2(15f, 7f),
        new Vector2(17f, 7f),   
    };


    private int _wallLayerIndex = -1;
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private GameManager _gameManager;
    private Transform _currentTarget;
    private bool _isPatrolling = false;
    private int _currentWaypointIndex = 0;
    private bool _isPatrolForward = true;
    private Vector3 _idleAnchor;
    private float _timeOffset;
    private Vector3 _runtimeBaseScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        
        if (_rb != null)
        {
            _rb.isKinematic = true; 
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }
        
        _wallLayerIndex = LayerMask.NameToLayer("Wall"); 
    }

    private void OnEnable()
    {
        WispActivationZone.ZoneTriggered += HandleActivationZoneTriggered;
    }

    private void OnDisable()
    {
        WispActivationZone.ZoneTriggered -= HandleActivationZoneTriggered;
    }

    private Vector2 GridToWorldPosition(Vector2 gridPos)
    {
        float offsetX = Mathf.Clamp01(horizontalCellOffset);
        float offsetY = Mathf.Clamp01(verticalCellOffset);
        float x = (gridPos.x + offsetX + _cellOffset.x) * _mazeCellSize;
        float y = -(gridPos.y + offsetY + _cellOffset.y) * _mazeCellSize; 
        
        return new Vector2(x, y);
    }

    public void ConfigureGridAlignment(float cellSize, Vector2 normalizedOffset)
    {
        ConfigureGridAlignment(cellSize, normalizedOffset, Vector2.zero);
    }

    public void ConfigureGridAlignment(float cellSize, Vector2 normalizedOffset, Vector2 cellOffset)
    {
        if (cellSize > 0f)
        {
            _mazeCellSize = cellSize;
        }

        horizontalCellOffset = Mathf.Clamp01(normalizedOffset.x);
        verticalCellOffset = Mathf.Clamp01(normalizedOffset.y);
        _cellOffset = cellOffset;
    }

    private void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        
        _timeOffset = Random.Range(0f, 10f);
        _runtimeBaseScale = Vector3.one * baseScale;
        transform.localScale = _runtimeBaseScale;

        if (_wallLayerIndex >= 0)
        {
            obstacleLayers = 1 << _wallLayerIndex;
        }
        else
        {
             obstacleLayers = 1 << 0; 
        }

        if (!_isPatrolling && PatrolGridPoints.Length > 0)
        {
             Vector2 startWorldPos = GridToWorldPosition(PatrolGridPoints[0]);
             _idleAnchor = startWorldPos;
             transform.position = _idleAnchor;
        }
        else
        {
             _idleAnchor = transform.position;
        }
    }

    private void Update()
    {
        if (!_isPatrolling)
        {
            PerformIdleHover();
        }
    }
    
    private void FixedUpdate()
    {
        if (_isPatrolling && PatrolGridPoints.Length > 1)
        {
            MoveAlongPatrolRoute();
        }
    }

    private void PerformIdleHover()
    {
        float timeValue = Time.time * hoverSpeed + _timeOffset;
        float pulseScale = 1f + Mathf.Sin(timeValue) * pulseMagnitude;
        transform.localScale = _runtimeBaseScale * pulseScale;
    }
    
    // --- MODIFIED PATROL METHOD: PURE AXIS MOVEMENT (NO RAYCAST BLOCKING) ---
    private void MoveAlongPatrolRoute()
    {
        Vector2 targetWaypoint = GridToWorldPosition(PatrolGridPoints[_currentWaypointIndex]);
        Vector2 currentPosition = _rb.position;
        
        if (Vector2.Distance(currentPosition, targetWaypoint) <= waypointTolerance)
        {
            // Force snap to the target before advancing
            _rb.MovePosition(targetWaypoint); 
            AdvanceToNextWaypoint();
            targetWaypoint = GridToWorldPosition(PatrolGridPoints[_currentWaypointIndex]);
        }
        
        float stepDistance = patrolSpeed * Time.fixedDeltaTime;
        
        Vector2 remainingDistance = targetWaypoint - currentPosition;
        Vector2 moveVector = Vector2.zero;
        
        // --- Enforce Strict Axis Alignment ---
        if (Mathf.Abs(remainingDistance.x) > Mathf.Abs(remainingDistance.y))
        {
            moveVector.x = Mathf.Sign(remainingDistance.x);
            moveVector.y = 0; 
        }
        else
        {
            moveVector.y = Mathf.Sign(remainingDistance.y);
            moveVector.x = 0; 
        }
        // --- End Enforce Strict Axis Alignment ---

        if (moveVector == Vector2.zero) return;
        
        Vector2 newPosition = currentPosition + moveVector * stepDistance;
        
        // Ensure we don't overshoot the final target position
        if (Vector2.Distance(currentPosition, targetWaypoint) < Vector2.Distance(currentPosition, newPosition))
        {
            newPosition = targetWaypoint;
        }

        _rb.MovePosition(newPosition);
    }
    // --- END MODIFIED PATROL METHOD ---

    private void AdvanceToNextWaypoint()
    {
        if (_isPatrolForward)
        {
            _currentWaypointIndex++;
            if (_currentWaypointIndex >= PatrolGridPoints.Length)
            {
                _isPatrolForward = false;
                _currentWaypointIndex = PatrolGridPoints.Length - 1; 
                if (_currentWaypointIndex < 0) _currentWaypointIndex = 0;
            }
        }
        else
        {
            _currentWaypointIndex--;
            if (_currentWaypointIndex < 0)
            {
                _isPatrolForward = true;
                _currentWaypointIndex = 1; 
                if (_currentWaypointIndex >= PatrolGridPoints.Length) _currentWaypointIndex = PatrolGridPoints.Length - 1;
            }
        }
    }

    private void HandleActivationZoneTriggered(Transform player)
    {
        if (player == null) return;
        
        _currentTarget = player;
        _isPatrolling = true;
        
        if (PatrolGridPoints.Length > 0 && !_isPatrolForward)
        {
             _isPatrolForward = true;
             _currentWaypointIndex = 0;
        }
        else if (PatrolGridPoints.Length > 0)
        {
             _currentWaypointIndex = 0; 
        }
    }

    private static bool IsPlayerTag(Component component)
    {
        return component.CompareTag("Player") ||
               component.CompareTag("FirePlayer") ||
               component.CompareTag("WaterPlayer");
    }

    /// <summary>
    /// Handles trigger collisions for the Wisp.
    /// Inflicts damage upon touching the player.
    /// Only triggers a full patrol route REVERSE if it hits a FireWall or IceWall.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerTag(other.GetComponent<Component>())) 
        {
            if (other.TryGetComponent(out CoopPlayerController playerController)) 
            {
                playerController.TakeDamageFromEnemy(other, 1);
            }
            return;
        }
        
        if (_isPatrolling)
        {
            // We check for FireWall/IceWall components because they are triggers 
            bool isHazard = other.TryGetComponent<IceWall>(out _) || other.TryGetComponent<FireWall>(out _);
            
            if (isHazard)
            {
                // 1. Reverse the patrol route immediately
                _isPatrolForward = !_isPatrolForward;
                
                // 2. Adjust index back by 1 step, based on the *new* forward direction.
                if (_isPatrolForward)
                {
                    // If reversing from backward to forward, current index (e.g., 4) should become 4 + 1 = 5
                    // BUT since the index points to the obstacle (4), and we reversed (forward), we want the safe point (3).
                    // The easiest fix is to simply decrement by 1, regardless of the new direction.
                    _currentWaypointIndex--;
                }
                else
                {
                    // If reversing from forward to backward, current index (e.g., 5) should become 5 - 1 = 4
                    _currentWaypointIndex--;
                }
                
                // Final check to handle the edge case of reversing near the start.
                _currentWaypointIndex = Mathf.Clamp(_currentWaypointIndex, 0, PatrolGridPoints.Length - 1);
            }
        }
    }
}
