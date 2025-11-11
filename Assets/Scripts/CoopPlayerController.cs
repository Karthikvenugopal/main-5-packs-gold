using System.Collections;
using System.Collections.Generic;
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

    [Header("Hazard Damage")]
    [SerializeField] private float hazardDamageCooldown = 0.5f;

    [Header("Feedback")]
    [SerializeField, Range(1, 6)] private int hurtFlashCount = 4;
    [SerializeField, Range(0.01f, 0.5f)] private float hurtFlashOnDuration = 0.3f;
    [SerializeField, Range(0.01f, 0.5f)] private float hurtFlashOffDuration = 0.3f;
    [SerializeField, Range(0f, 1f)] private float hurtFlashGreyBlend = 0.4f;
    [SerializeField, Range(0f, 1f)] private float hurtFlashBrightnessBoost = 0.09f;
    [Header("Hazard Pushback")]
    [Tooltip("Distance (in world units) the player is pushed away from their own obstacle.")]
    [SerializeField, Range(0.1f, 1.5f)] private float hazardPushDistance = 0.45f;
    [Tooltip("Max angular deviation (degrees) applied to the push direction to keep it from feeling perfectly straight.")]
    [SerializeField, Range(0f, 60f)] private float hazardPushAngularVariance = 20f;
    [Tooltip("Initial speed applied after the push so the player bounces off nearby walls.")]
    [SerializeField, Range(1f, 10f)] private float hazardBounceSpeed = 3.5f;
    [Tooltip("How long bouncing remains active after the collision.")]
    [SerializeField, Range(0.1f, 1f)] private float hazardBounceDuration = 0.22f;
    [Tooltip("Energy retained on each bounce (1 = no slowdown, 0 = stop immediately).")]
    [SerializeField, Range(0f, 1f)] private float hazardBounceElasticity = 0.75f;
    [Header("Hazard Input Lock")]
    [Tooltip("Seconds to ignore user movement input after being hurt by own obstacle.")]
    [SerializeField, Range(0.5f, 5f)] private float hazardInputLockDuration = 1.5f;

    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private GameManager _gameManager;
    private GameManagerTutorial _gameManagerTutorial;
    private PlayerRole _role;
    private Vector2 _moveInput;
    private bool _movementEnabled;
    private Coroutine _hurtFlashRoutine;
    private bool _pendingPushOverride;
    private Vector2 _bounceVelocity;
    private float _bounceTimeRemaining;
    private float _inputLockRemaining;

    private readonly Dictionary<int, float> _lastHazardDamageTimes = new();

    public PlayerRole Role => _role;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

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
        _gameManagerTutorial = null;
        _movementEnabled = false;

        ApplyRoleVisuals();
        _gameManager?.RegisterPlayer(this);
    }

    public void Initialize(PlayerRole role, GameManagerTutorial manager)
    {
        _role = role;
        _gameManagerTutorial = manager;
        _gameManager = null;
        _movementEnabled = false;

        ApplyRoleVisuals();
        _gameManagerTutorial?.RegisterPlayer(this);
    }

    private void Update()
    {
        if (!_movementEnabled)
        {
            _moveInput = Vector2.zero;
            return;
        }

        if (_inputLockRemaining > 0f)
        {
            _inputLockRemaining = Mathf.Max(0f, _inputLockRemaining - Time.deltaTime);
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
        if (_bounceTimeRemaining > 0f && _bounceVelocity.sqrMagnitude > 0.0001f)
        {
            SimulateBounce(Time.fixedDeltaTime);
            return;
        }

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
            distance + 0.01f,
            collisionMask
        );

        if (hit.collider != null && TryHandleSpecialObstacle(hit.collider))
        {
            hit = Physics2D.BoxCast(
                _rigidbody.position,
                boxSize,
                0f,
                direction,
                distance + 0.01f,
                collisionMask
            );
        }

        if (_pendingPushOverride)
        {
            _pendingPushOverride = false;
            return;
        }

        if (hit.collider == null)
        {
            _rigidbody.MovePosition(targetPosition);
        }
        else if (hit.distance > 0.01f)
        {
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
            if (_role == PlayerRole.Fireboy && iceWall.TryMelt(_role))
            {
                return true;
            }

            if (_role == PlayerRole.Watergirl)
            {
                TryApplyHazardDamage(collider);
            }

            return false;
        }

        if (collider.TryGetComponent(out FireWall fireWall))
        {
            if (_role == PlayerRole.Fireboy)
            {
                TryApplyHazardDamage(collider);
                return false;
            }

            if (fireWall.TryExtinguish(_role))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyRoleVisuals()
    {
        if (_spriteRenderer == null && !TryGetComponent(out _spriteRenderer)) return;

        _spriteRenderer.color = GetRoleColor();
        _spriteRenderer.sortingOrder = 5;
    }

    private Color GetRoleColor()
    {
        return _role == PlayerRole.Fireboy ? fireboyColor : watergirlColor;
    }

    public void PlayHurtFlash()
    {
        if (!isActiveAndEnabled) return;
        if (_spriteRenderer == null && !TryGetComponent(out _spriteRenderer)) return;

        if (_hurtFlashRoutine != null)
        {
            StopCoroutine(_hurtFlashRoutine);
        }

        _hurtFlashRoutine = StartCoroutine(HurtFlashRoutine());
    }

    private IEnumerator HurtFlashRoutine()
    {
        Color originalColor = GetRoleColor();
        Color flashColor = BuildHurtFlashColor(originalColor);

        int flashes = Mathf.Max(1, hurtFlashCount);
        float onDuration = Mathf.Max(0.01f, hurtFlashOnDuration);
        float offDuration = Mathf.Max(0.01f, hurtFlashOffDuration);

        for (int i = 0; i < flashes; i++)
        {
            _spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(onDuration);
            _spriteRenderer.color = originalColor;

            if (i < flashes - 1)
            {
                yield return new WaitForSeconds(offDuration);
            }
        }

        _hurtFlashRoutine = null;
    }

    private Color BuildHurtFlashColor(Color roleColor)
    {
        float brightness = Mathf.Clamp01(hurtFlashBrightnessBoost);
        float greyBlend = Mathf.Clamp01(hurtFlashGreyBlend);

        Color brightened = Color.Lerp(roleColor, Color.white, brightness);
        Color greyTarget = Color.Lerp(brightened, Color.gray, greyBlend);

        greyTarget.a = roleColor.a;
        return greyTarget;
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
        if (_gameManager == null && _gameManagerTutorial == null) return;
        if (!collision.collider.TryGetComponent(out CoopPlayerController other)) return;
        if (other == this) return;

        if (GetInstanceID() < other.GetInstanceID())
        {
            _gameManager?.OnPlayersTouched(this, other);
            _gameManagerTutorial?.OnPlayersTouched(this, other);
        }
    }

    private bool TryApplyHazardDamage(Collider2D hazard)
    {
        if (hazard == null || (_gameManager == null && _gameManagerTutorial == null)) return false;

        int hazardId = hazard.GetInstanceID();
        float now = Time.time;

        if (_lastHazardDamageTimes.TryGetValue(hazardId, out float lastTime))
        {
            if (now - lastTime < Mathf.Max(0f, hazardDamageCooldown))
            {
                return false;
            }
        }

        _lastHazardDamageTimes[hazardId] = now;
        _gameManagerTutorial?.DamagePlayer(_role, 1);
        GameManager.DamageCause cause = GameManager.DamageCause.Unknown;
        bool shouldPushback = false;
        if (hazard.TryGetComponent<FireWall>(out _))
        {
            cause = GameManager.DamageCause.FireWall;
            shouldPushback = _role == PlayerRole.Fireboy;
        }
        else if (hazard.TryGetComponent<IceWall>(out _))
        {
            cause = GameManager.DamageCause.IceWall;
            shouldPushback = _role == PlayerRole.Watergirl;
        }
        _gameManager.DamagePlayer(_role, 1, cause, hazard.transform.position);
        if (shouldPushback)
        {
            ApplyHazardPushback(hazard);
        }
        return true;
    }

    private void ApplyHazardPushback(Collider2D hazard)
    {
        if (_rigidbody == null) return;

        Vector2 origin = _rigidbody.position;
        Vector2 closestPoint = hazard.ClosestPoint(origin);
        Vector2 direction = origin - closestPoint;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = origin - (Vector2)hazard.transform.position;
        }
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }
        direction.Normalize();
        if (hazardPushAngularVariance > 0.01f)
        {
            float variance = UnityEngine.Random.Range(-hazardPushAngularVariance, hazardPushAngularVariance);
            direction = Quaternion.Euler(0f, 0f, variance) * direction;
        }

        float distance = Mathf.Max(0.01f, hazardPushDistance);
        Vector2 target = origin + direction * distance;

        // Snap using MovePosition so physics interpolation stays smooth.
        _rigidbody.MovePosition(target);
        _moveInput = Vector2.zero;
        _pendingPushOverride = true;
        _bounceVelocity = direction * hazardBounceSpeed;
        _bounceTimeRemaining = hazardBounceDuration;
        _inputLockRemaining = Mathf.Max(_inputLockRemaining, hazardInputLockDuration);
    }

    private void SimulateBounce(float deltaTime)
    {
        if (_rigidbody == null)
        {
            StopBounce();
            return;
        }

        Vector2 position = _rigidbody.position;
        Vector2 velocity = _bounceVelocity;
        Vector2 displacement = velocity * deltaTime;
        float distance = displacement.magnitude;
        Vector2 direction = distance > 0f ? displacement / distance : Vector2.zero;
        Vector2 boxSize = GetColliderSize() * collisionBoxScale;

        if (distance > 0f && direction != Vector2.zero)
        {
            RaycastHit2D hit = Physics2D.BoxCast(position, boxSize, 0f, direction, distance, collisionMask);
            if (hit.collider == null)
            {
                position += displacement;
            }
            else
            {
                float moveDistance = Mathf.Max(0f, hit.distance - 0.01f);
                position += direction * moveDistance;
                Vector2 reflected = Vector2.Reflect(velocity, hit.normal) * hazardBounceElasticity;
                velocity = reflected;
            }
        }
        else
        {
            position += displacement;
        }

        _rigidbody.MovePosition(position);
        _bounceVelocity = velocity;
        _bounceTimeRemaining = Mathf.Max(0f, _bounceTimeRemaining - deltaTime);

        if (_bounceTimeRemaining <= 0f || _bounceVelocity.sqrMagnitude < 0.0001f)
        {
            StopBounce();
        }
    }

    private void StopBounce()
    {
        _bounceVelocity = Vector2.zero;
        _bounceTimeRemaining = 0f;
        _pendingPushOverride = false;
    }
}
