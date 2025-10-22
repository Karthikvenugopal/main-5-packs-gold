using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KnifeEnemy : MonoBehaviour
{
    private enum State
    {
        Idle,
        Charging,
        Dashing,
        Recovering
    }

    [Header("Targeting")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float loseInterestRadius = 8f;

    [Header("Dash Behaviour")]
    [SerializeField] private float chargeDuration = 0.5f;
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float recoverDuration = 0.75f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Player Interaction")]
    [SerializeField] private float knockbackDistance = 1.35f;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerAbilityController cachedPlayerAbilities;

    private State state = State.Idle;
    private float stateTimer;
    private Vector2 dashDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (obstacleLayers == 0)
        {
            obstacleLayers = LayerMask.GetMask("Wall");
        }
    }

    private void Update()
    {
        AcquirePlayerReference();
        UpdateStateMachine(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (state == State.Dashing)
        {
            Vector2 delta = dashDirection * dashSpeed * Time.fixedDeltaTime;
            Vector2 target = rb.position + delta;

            RaycastHit2D hit = Physics2D.CircleCast(rb.position, 0.25f, dashDirection, delta.magnitude, obstacleLayers);
            if (hit.collider != null)
            {
                EnterRecoverState();
                return;
            }

            rb.MovePosition(target);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.TryGetComponent(out PlayerAbilityController abilityController))
        {
            return;
        }

        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        HandlePlayerCollision(abilityController, playerRb);
    }

    private void UpdateStateMachine(float deltaTime)
    {
        switch (state)
        {
            case State.Idle:
                if (PlayerWithinRadius(detectionRadius))
                {
                    BeginCharge();
                }
                break;

            case State.Charging:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                {
                    BeginDash();
                }
                else if (!PlayerWithinRadius(loseInterestRadius))
                {
                    state = State.Idle;
                }
                break;

            case State.Dashing:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                {
                    EnterRecoverState();
                }
                break;

            case State.Recovering:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                {
                    state = State.Idle;
                }
                break;
        }
    }

    private void BeginCharge()
    {
        state = State.Charging;
        stateTimer = chargeDuration;
        dashDirection = GetDirectionToPlayer();
    }

    private void BeginDash()
    {
        state = State.Dashing;
        stateTimer = dashDuration;
        dashDirection = GetDirectionToPlayer();
        if (dashDirection == Vector2.zero)
        {
            dashDirection = Vector2.right;
        }
    }

    private void EnterRecoverState()
    {
        state = State.Recovering;
        stateTimer = recoverDuration;
        rb.linearVelocity = Vector2.zero;
    }

    private void AcquirePlayerReference()
    {
        if (playerTransform != null) return;

        PlayerController2D controller = FindFirstObjectByType<PlayerController2D>();
        if (controller != null)
        {
            playerTransform = controller.transform;
            cachedPlayerAbilities = controller.GetComponent<PlayerAbilityController>();
        }
    }

    private bool PlayerWithinRadius(float radius)
    {
        if (playerTransform == null) return false;
        return Vector2.Distance(transform.position, playerTransform.position) <= radius;
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (playerTransform == null)
        {
            return Vector2.zero;
        }

        Vector2 direction = (playerTransform.position - transform.position);
        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        return direction.normalized;
    }

    private void HandlePlayerCollision(PlayerAbilityController abilityController, Rigidbody2D playerRb)
    {
        if (playerRb != null)
        {
            Vector2 pushDir = (playerRb.position - rb.position).normalized;
            if (pushDir.sqrMagnitude < 0.01f)
            {
                pushDir = dashDirection.sqrMagnitude > 0f ? dashDirection : Vector2.up;
            }

            Vector2 target = playerRb.position + pushDir * knockbackDistance;
            playerRb.MovePosition(target);
        }

        bool hasGarlic = abilityController.HasAbility(IngredientType.Garlic);
        if (hasGarlic)
        {
            dashDirection = -dashDirection;
            EnterRecoverState();
            return;
        }

        abilityController.TryConsumeAnyAbility(out _);
        EnterRecoverState();
    }
}
