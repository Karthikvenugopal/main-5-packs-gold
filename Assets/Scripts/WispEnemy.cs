using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
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
    [SerializeField] private LayerMask obstacleLayers; 
    [SerializeField] private float _mazeCellSize = 1.0f; 

    
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
    private Animator _animator;
    private string _currentAnimationState;
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
        _animator = GetComponent<Animator>();
        
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

        SetAnimatorState("Idle");
    }

    private void Update()
    {
        if (!_isPatrolling)
        {
            PerformIdleHover();
            SetAnimatorState("Idle");
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

    
    
    
    private void UpdateDirectionAnimation(Vector2 movement)
    {
        string targetState = MapMovementToState(movement);
        if (string.IsNullOrEmpty(targetState)) return;
        SetAnimatorState(targetState);
    }

    private void SetAnimatorState(string stateName)
    {
        if (_animator == null || string.IsNullOrEmpty(stateName) || _currentAnimationState == stateName) return;
        _animator.Play(stateName);
        _currentAnimationState = stateName;
    }

    private static string MapMovementToState(Vector2 movement)
    {
        if (movement.sqrMagnitude < 0.0001f) return null;
        Vector2 absMovement = new Vector2(Mathf.Abs(movement.x), Mathf.Abs(movement.y));
        if (absMovement.x >= absMovement.y)
        {
            return movement.x >= 0f ? "Right" : "Left";
        }

        return movement.y >= 0f ? "Up" : "Down";
    }
    
    
    private void MoveAlongPatrolRoute()
    {
        Vector2 targetWaypoint = GridToWorldPosition(PatrolGridPoints[_currentWaypointIndex]);
        Vector2 currentPosition = _rb.position;
        
        if (Vector2.Distance(currentPosition, targetWaypoint) <= waypointTolerance)
        {
            
            _rb.MovePosition(targetWaypoint); 
            AdvanceToNextWaypoint();
            targetWaypoint = GridToWorldPosition(PatrolGridPoints[_currentWaypointIndex]);
        }
        
        float stepDistance = patrolSpeed * Time.fixedDeltaTime;
        
        Vector2 remainingDistance = targetWaypoint - currentPosition;
        Vector2 moveVector = Vector2.zero;
        
        
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
        

        UpdateDirectionAnimation(moveVector);

        if (moveVector == Vector2.zero) return;
        
        Vector2 newPosition = currentPosition + moveVector * stepDistance;
        
        
        if (Vector2.Distance(currentPosition, targetWaypoint) < Vector2.Distance(currentPosition, newPosition))
        {
            newPosition = targetWaypoint;
        }

        _rb.MovePosition(newPosition);
    }
    

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
            
            bool isHazard = other.TryGetComponent<IceWall>(out _) || other.TryGetComponent<FireWall>(out _);
            
            if (isHazard)
            {
                
                _isPatrolForward = !_isPatrolForward;
                
                
                if (_isPatrolForward)
                {
                    
                    
                    
                    _currentWaypointIndex--;
                }
                else
                {
                    
                    _currentWaypointIndex--;
                }
                
                
                _currentWaypointIndex = Mathf.Clamp(_currentWaypointIndex, 0, PatrolGridPoints.Length - 1);
            }
        }
    }
}
