using UnityEngine;

public enum PlayerRole
{
    Fireboy = 0,
    Watergirl = 1
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CoopPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionBoxScale = 0.75f;

    [Header("Visuals")]
    [SerializeField] private Color fireboyColor = new Color(0.93f, 0.39f, 0.18f);
    [SerializeField] private Color watergirlColor = new Color(0.2f, 0.45f, 0.95f);

    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private GameManager _gameManager;
    private PlayerRole _role;
    private Vector2 _moveInput;
    private bool _movementEnabled;

    public PlayerRole Role => _role;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();

        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (collisionMask == 0)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            collisionMask = wallLayer >= 0 ? (1 << wallLayer) : Physics2D.AllLayers;
        }
    }

    public void Initialize(PlayerRole role, GameManager manager)
    {
        _role = role;
        _gameManager = manager;
        _movementEnabled = false;

        ApplyRoleVisuals();
        _gameManager?.RegisterPlayer(this);
    }

    private void Update()
    {
        if (!_movementEnabled)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = ReadInput();
        if (_moveInput.sqrMagnitude > 1f)
        {
            _moveInput.Normalize();
        }
    }

    private Vector2 ReadInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (_role == PlayerRole.Fireboy)
        {
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
        }

        return new Vector2(horizontal, vertical);
    }

    private void FixedUpdate()
    {
        if (_moveInput == Vector2.zero) return;

        Vector2 direction = _moveInput.normalized;
        float distance = moveSpeed * Time.fixedDeltaTime;
        Vector2 targetPosition = _rigidbody.position + direction * distance;

        Vector2 boxSize = GetColliderSize() * collisionBoxScale;

        RaycastHit2D hit = Physics2D.BoxCast(
            _rigidbody.position,
            boxSize,
            0f,
            direction,
            distance + 0.01f, // Small epsilon for better precision
            collisionMask
        );

        if (hit.collider != null && TryHandleSpecialObstacle(hit.collider))
        {
            // Re-check collision after handling special obstacle
            hit = Physics2D.BoxCast(
                _rigidbody.position,
                boxSize,
                0f,
                direction,
                distance + 0.01f,
                collisionMask
            );
        }

        if (hit.collider == null)
        {
            _rigidbody.MovePosition(targetPosition);
        }
        else if (hit.distance > 0.01f)
        {
            // If there's a collision but still room to move, move closer
            _rigidbody.MovePosition(_rigidbody.position + direction * (hit.distance - 0.01f));
        }
    }

    private Vector2 GetColliderSize()
    {
        return _collider switch
        {
            BoxCollider2D box => box.size,
            CircleCollider2D circle => Vector2.one * circle.radius * 2f,
            CapsuleCollider2D capsule => capsule.size,
            _ => Vector2.one * 0.9f
        };
    }

    private bool TryHandleSpecialObstacle(Collider2D collider)
    {
        if (collider == null) return false;

        if (collider.TryGetComponent(out IceWall iceWall))
        {
            return iceWall.TryMelt(_role);
        }

        if (collider.TryGetComponent(out FireWall fireWall))
        {
            return fireWall.TryExtinguish(_role);
        }

        return false;
    }

    private void ApplyRoleVisuals()
    {
        if (!TryGetComponent(out SpriteRenderer renderer)) return;

        renderer.color = _role == PlayerRole.Fireboy ? fireboyColor : watergirlColor;
        renderer.sortingOrder = 5;
    }

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;
        if (!enabled)
        {
            _moveInput = Vector2.zero;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_gameManager == null) return;
        if (!collision.collider.TryGetComponent(out CoopPlayerController other)) return;
        if (other == this) return;

        _gameManager.OnPlayersTouched();
    }
}
