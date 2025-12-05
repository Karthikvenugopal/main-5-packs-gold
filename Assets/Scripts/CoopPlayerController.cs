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
[RequireComponent(typeof(Animator))]
public class CoopPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionBoxScale = 0.75f;
    [Header("Steam Animations")]
    [SerializeField] private RuntimeAnimatorController emberSteamAnimatorController;
    [SerializeField] private RuntimeAnimatorController aquaSteamAnimatorController;
    [Tooltip("Scale applied to the circle collider when sampling collisions to keep movement away from corners.")]
    [SerializeField, Range(0.5f, 1f)] private float circleCastScale = 0.85f;

    [Header("Visuals")]
    [SerializeField] private Color fireboyColor = new Color(0.93f, 0.39f, 0.18f);
    [SerializeField] private Color watergirlColor = new Color(0.2f, 0.45f, 0.95f);

    [Header("Animations")]
    [SerializeField] private RuntimeAnimatorController aquaAnimatorController;
    [SerializeField] private RuntimeAnimatorController emberAnimatorController;

    [Header("Ember Collider")]
    [SerializeField] private Vector2 emberColliderSize = new Vector2(0.65f, 0.65f);
    [SerializeField] private Vector2 emberColliderOffset = Vector2.zero;

    [Header("Hazard Damage")]
    [SerializeField] private float hazardDamageCooldown = 0.5f;

    [Header("Feedback")]
    [SerializeField, Range(1, 6)] private int hurtFlashCount = 3;
    [SerializeField, Range(0.01f, 0.5f)] private float hurtFlashOnDuration = 0.25f;
    [SerializeField, Range(0.01f, 0.5f)] private float hurtFlashOffDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float hurtFlashGreyBlend = 0.4f;
    [SerializeField, Range(0f, 1f)] private float hurtFlashBrightnessBoost = 0.09f;
    [Tooltip("Seconds spent easing down to the hurt scale.")]
    [SerializeField, Min(0f)] private float hurtScaleShrinkDuration = 1f;
    [Tooltip("Seconds spent holding the hurt scale before recovery.")]
    [SerializeField, Min(0f)] private float hurtScaleHoldDuration = 1f;
    [Tooltip("Seconds spent easing back to the original scale.")]
    [SerializeField, Min(0f)] private float hurtScaleRecoverDuration = 1f;
    [Tooltip("Multiplier applied to local scale when hurt.")]
    [SerializeField, Range(0.1f, 1f)] private float hurtScaleMultiplier = 0.6f;

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
    private Coroutine _hurtScaleRoutine;
    private Vector3 _initialScale;
    private Animator _animator;
    private string _currentStateName;
    private BoxCollider2D _boxCollider;
    private Vector2 _defaultColliderSize;
    private Vector2 _defaultColliderOffset;
    private bool _isUsingSteamAnimator;
    private CircleCollider2D _circleCollider;
    private Collider2D _collider;
    private Collider2D[] _allColliders;
    private static readonly List<CoopPlayerController> s_Players = new();

    private readonly Dictionary<int, float> _lastHazardDamageTimes = new();

    // 蒸汽状态引用
    private PlayerSteamState _steamState;
    public bool IsInSteamMode => _steamState != null && _steamState.IsInSteamMode;
    private bool _wasInSteamMode;

    public PlayerRole Role => _role;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _circleCollider = GetComponent<CircleCollider2D>();
        _collider = _circleCollider ?? GetComponent<Collider2D>();
        _allColliders = GetComponents<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();
        if (_boxCollider != null)
        {
            _defaultColliderSize = _boxCollider.size;
            _defaultColliderOffset = _boxCollider.offset;
        }

        if (_animator == null)
        {
            _animator = gameObject.AddComponent<Animator>();
        }

        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _initialScale = transform.localScale;

        if (collisionMask == 0)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            collisionMask = wallLayer >= 0 ? (1 << wallLayer) : Physics2D.AllLayers;
        }

        _steamState = GetComponent<PlayerSteamState>();
        if (_steamState == null)
        {
            Debug.LogWarning($"[CoopPlayerController] {name} has NO PlayerSteamState component. Steam mode will never be on.");
        }
        if (!s_Players.Contains(this))
        {
            s_Players.Add(this);
        }
    }

    private void OnEnable()
    {
        if (!s_Players.Contains(this))
        {
            s_Players.Add(this);
        }
    }

    private void OnDisable()
    {
        s_Players.Remove(this);
    }

    public void Initialize(PlayerRole role, GameManager manager)
    {
        _role = role;
        _gameManager = manager;
        _gameManagerTutorial = null;
        _movementEnabled = false;

        ApplyRoleVisuals();
        ConfigureRoleAnimations(role);
        ConfigureRoleCollider(role);
        ResetScaleImmediate();
        _gameManager?.RegisterPlayer(this);
    }

    public void Initialize(PlayerRole role, GameManagerTutorial manager)
    {
        _role = role;
        _gameManagerTutorial = manager;
        _gameManager = null;
        _movementEnabled = false;

        ApplyRoleVisuals();
        ConfigureRoleAnimations(role);
        ConfigureRoleCollider(role);
        ResetScaleImmediate();
        _gameManagerTutorial?.RegisterPlayer(this);
    }

    private void ConfigureRoleAnimations(PlayerRole role)
    {
        ApplyRoleAnimatorController(role);
        _currentStateName = null;
        UpdateMovementAnimation(Vector2.zero);
        ConfigureRoleCollider(role);
        _isUsingSteamAnimator = false;
        RefreshSteamAnimator();
    }

    private void ApplyRoleAnimatorController(PlayerRole role)
    {
        if (_animator == null) return;
        RuntimeAnimatorController controller = role == PlayerRole.Fireboy ? emberAnimatorController : aquaAnimatorController;
        if (controller == null)
        {
            Debug.LogWarning($"[CoopPlayerController] {name} requires an AnimatorController for {role}.", this);
            return;
        }

        _animator.runtimeAnimatorController = controller;
    }

    private void RefreshSteamAnimator()
    {
        if (_animator == null || _role == null) return;
        bool shouldUseSteam = _steamState != null && _steamState.IsInSteamMode;
        if (shouldUseSteam == _isUsingSteamAnimator) return;

        _isUsingSteamAnimator = shouldUseSteam;
        RuntimeAnimatorController controller = null;
        if (shouldUseSteam)
        {
            controller = _role == PlayerRole.Fireboy ? emberSteamAnimatorController : aquaSteamAnimatorController;
        }
        else
        {
            controller = _role == PlayerRole.Fireboy ? emberAnimatorController : aquaAnimatorController;
        }

        if (controller == null)
        {
            controller = _role == PlayerRole.Fireboy ? emberAnimatorController : aquaAnimatorController;
        }

        if (controller != null)
        {
            _animator.runtimeAnimatorController = controller;
        }

        UpdateSteamCollision();
    }

    private void UpdateSteamCollision()
    {
        if (_allColliders == null || _allColliders.Length == 0) return;

        foreach (CoopPlayerController other in s_Players)
        {
            if (other == null || other == this) continue;
            if (other._allColliders == null || other._allColliders.Length == 0) continue;

            bool ignore = IsInSteamMode || other.IsInSteamMode;
            foreach (Collider2D ownCollider in _allColliders)
            {
                if (ownCollider == null) continue;
                foreach (Collider2D otherCollider in other._allColliders)
                {
                    if (otherCollider == null) continue;
                    Physics2D.IgnoreCollision(ownCollider, otherCollider, ignore);
                }
            }
        }
    }

    private void ConfigureRoleCollider(PlayerRole role)
    {
        if (_boxCollider == null) return;

        if (role == PlayerRole.Fireboy)
        {
            _boxCollider.size = emberColliderSize;
            _boxCollider.offset = emberColliderOffset;
        }
        else
        {
            _boxCollider.size = _defaultColliderSize;
            _boxCollider.offset = _defaultColliderOffset;
        }
    }

    private void Update()
    {
        bool currentlyInSteam = IsInSteamMode;
        if (currentlyInSteam && !_wasInSteamMode)
        {
            StopBounce();
        }
        if (currentlyInSteam != _wasInSteamMode)
        {
            _wasInSteamMode = currentlyInSteam;
            RefreshSteamAnimator();
        }

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
        UpdateMovementAnimation(_moveInput);

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
        bool useCircleCast = _circleCollider != null;
        float castRadius = Mathf.Max(0.05f, GetCircleRadius() * circleCastScale);
        float castDistance = distance + 0.01f;

        RaycastHit2D hit = useCircleCast
            ? Physics2D.CircleCast(_rigidbody.position, castRadius, direction, castDistance, collisionMask)
            : Physics2D.BoxCast(_rigidbody.position, boxSize, 0f, direction, castDistance, collisionMask);

        if (hit.collider != null && TryHandleSpecialObstacle(hit.collider))
        {
            hit = useCircleCast
                ? Physics2D.CircleCast(_rigidbody.position, castRadius, direction, castDistance, collisionMask)
                : Physics2D.BoxCast(_rigidbody.position, boxSize, 0f, direction, castDistance, collisionMask);
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

    private void UpdateMovementAnimation(Vector2 direction)
    {
        if (_animator == null) return;

        string targetState = MapDirectionToState(direction);
        if (string.IsNullOrEmpty(targetState)) return;

        if (_currentStateName == targetState) return;

        _animator.Play(targetState);
        _currentStateName = targetState;
    }

    private static string MapDirectionToState(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return "Idle";

        Vector2 abs = new Vector2(Mathf.Abs(direction.x), Mathf.Abs(direction.y));

        if (abs.x >= abs.y)
        {
            return direction.x >= 0f ? "Right" : "Left";
        }

        return direction.y >= 0f ? "Up" : "Down";
    }

    private Vector2 GetColliderSize()
    {
        if (_collider == null) return Vector2.one * 0.9f;

        return _collider switch
        {
            CircleCollider2D circle when circle != null => Vector2.one * GetCircleRadius() * 2f,
            BoxCollider2D box when box != null => box.size,
            CapsuleCollider2D capsule when capsule != null => capsule.size,
            _ => Vector2.one * 0.9f
        };
    }

    private float GetCircleRadius()
    {
        if (_circleCollider == null) return 0.45f;
        float maxScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        return _circleCollider.radius * maxScale;
    }

    private bool TryHandleSpecialObstacle(Collider2D collider)
    {
        if (collider == null) return false;

        // 蒸汽模式：不触发任何特殊墙体逻辑
        if (IsInSteamMode)
        {
            Debug.Log($"[CoopPlayerController] {name} is in STEAM MODE, ignore special obstacle: {collider.name}", this);
            return false;
        }

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
        Color redFlash = Color.red;
        redFlash.a = roleColor.a;
        return redFlash;
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
        // 蒸汽模式：完全免疫 Hazard 伤害
        if (IsInSteamMode)
        {
            Debug.Log($"[CoopPlayerController] {name} in STEAM MODE, ignore hazard damage from {hazard.name}", this);
            return false;
        }

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
        TriggerHurtScaleRoutine();
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
        if (_gameManager != null)
        {
            _gameManager.DamagePlayer(_role, 1, cause, hazard.transform.position);
        }
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

    private void TriggerHurtScaleRoutine()
    {
        if (_hurtScaleRoutine != null)
        {
            StopCoroutine(_hurtScaleRoutine);
        }
        _hurtScaleRoutine = StartCoroutine(HurtScaleRoutine());
    }

    private IEnumerator HurtScaleRoutine()
    {
        Vector3 baseScale = _initialScale == Vector3.zero ? Vector3.one : _initialScale;
        Vector3 targetScale = baseScale * hurtScaleMultiplier;

        float shrinkDuration = Mathf.Max(0f, hurtScaleShrinkDuration);
        if (shrinkDuration <= 0f)
        {
            transform.localScale = targetScale;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < shrinkDuration)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, shrinkDuration));
                float eased = EaseOutQuad(t);
                transform.localScale = Vector3.Lerp(baseScale, targetScale, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
        }

        if (hurtScaleHoldDuration > 0f)
        {
            yield return new WaitForSeconds(hurtScaleHoldDuration);
        }

        float recoverDuration = Mathf.Max(0f, hurtScaleRecoverDuration);
        if (recoverDuration <= 0f)
        {
            transform.localScale = baseScale;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < recoverDuration)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, recoverDuration));
                float eased = EaseInQuad(t);
                transform.localScale = Vector3.Lerp(targetScale, baseScale, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = baseScale;
        }

        _hurtScaleRoutine = null;
    }

    private void ResetScaleImmediate()
    {
        if (_hurtScaleRoutine != null)
        {
            StopCoroutine(_hurtScaleRoutine);
            _hurtScaleRoutine = null;
        }
        Vector3 baseScale = _initialScale == Vector3.zero ? Vector3.one : _initialScale;
        transform.localScale = baseScale;
    }

    /// <summary>
    /// Allows non-wall enemies (like Wisp) to inflict damage on the player.
    /// </summary>
    public void TakeDamageFromEnemy(Collider2D enemyCollider, int damageAmount = 1)
    {
        // 蒸汽模式：敌人也不能掉心
        if (IsInSteamMode)
        {
            Debug.Log($"[CoopPlayerController] {name} in STEAM MODE, ignore enemy damage from {enemyCollider.name}", this);
            return;
        }

        if (_gameManager == null && _gameManagerTutorial == null) return;

        int enemyId = enemyCollider.GetInstanceID();
        float now = Time.time;

        if (_lastHazardDamageTimes.TryGetValue(enemyId, out float lastTime))
        {
            if (now - lastTime < Mathf.Max(0f, hazardDamageCooldown))
            {
                return;
            }
        }

        _lastHazardDamageTimes[enemyId] = now;
        TriggerHurtScaleRoutine();

        GameManager.DamageCause cause = GameManager.DamageCause.Unknown;

        _gameManagerTutorial?.DamagePlayer(_role, damageAmount);

        if (_gameManager != null)
        {
            _gameManager.DamagePlayer(_role, damageAmount, cause, enemyCollider.transform.position);
        }
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private void OnDestroy()
    {
        s_Players.Remove(this);
    }
}
