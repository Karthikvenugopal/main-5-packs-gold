using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RollingPinEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private Vector2 initialDirection = Vector2.right;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Player Interaction")]
    [SerializeField] private float knockbackDistance = 1.15f;
    [SerializeField] private float abilityStealCooldownSeconds = 1.25f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float nextAbilityStealTime;
    private bool hasHitPlayerRecently = false;
    private Collider2D collider2D;
    private readonly RaycastHit2D[] movementHits = new RaycastHit2D[1];
    private ContactFilter2D obstacleFilter;
    private const float MovementCastBuffer = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        collider2D = GetComponent<Collider2D>();

        moveDirection = GetSnappedDirection(initialDirection);

        if (obstacleLayers == 0)
        {
            obstacleLayers = LayerMask.GetMask("Wall");
        }

        obstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleLayers,
            useTriggers = false
        };
    }

    private void FixedUpdate()
    {
        if (moveDirection.sqrMagnitude < Mathf.Epsilon)
            return;

        Vector2 delta = moveDirection * moveSpeed * Time.fixedDeltaTime;
        Vector2 nextPosition = rb.position + delta;

        if (collider2D != null)
        {
            float castDistance = delta.magnitude + MovementCastBuffer;
            int hitCount = collider2D.Cast(moveDirection, obstacleFilter, movementHits, castDistance, true);
            if (hitCount > 0)
            {
                ReverseDirection();
                return;
            }
        }
        else
        {
            float checkDistance = delta.magnitude + MovementCastBuffer;
            RaycastHit2D wallHit = Physics2D.CircleCast(rb.position, 0.5f, moveDirection, checkDistance, obstacleLayers);
            if (wallHit.collider != null)
            {
                ReverseDirection();
                return;
            }
        }

        if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
        {
            Vector2 edgeCheckOrigin = rb.position + moveDirection * 0.5f;
            float edgeCheckDistance = 0.8f;
            int groundMask = LayerMask.GetMask("Wall", "Floor");

            RaycastHit2D edgeHit = Physics2D.Raycast(edgeCheckOrigin, Vector2.down, edgeCheckDistance, groundMask);

            if (!edgeHit.collider)
            {
                ReverseDirection();
                return;
            }
        }


        rb.MovePosition(nextPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & obstacleLayers) != 0)
        {
            ReverseDirection();
            return;
        }

        bool handledByCoopController = false;
        if (collision.collider.TryGetComponent(out CoopPlayerController coopPlayer))
        {
            handledByCoopController = true;
            GameManager gm = FindAnyObjectByType<GameManager>();
            gm?.OnPlayerHitByEnemy();
        }

        if (collision.collider.TryGetComponent(out PlayerAbilityController abilityController))
        {
            bool hasShield = abilityController.HasAbility(IngredientType.Chocolate);

            if (hasHitPlayerRecently) return;
            hasHitPlayerRecently = true;
            Invoke(nameof(ResetPlayerHitFlag), 0.5f); 

            if (!hasShield)
            {
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    gm.OnPlayerHitByEnemy();
                }
            }
            else
            {
                ReverseDirection();
            }
        }

        if (handledByCoopController)
        {
            return;
        }
    }

    private void ResetPlayerHitFlag()
    {
        hasHitPlayerRecently = false;
    }


    private void HandlePlayerCollision(PlayerAbilityController abilityController, Rigidbody2D playerRb)
    {
        bool hasSugarRush = abilityController.HasAbility(IngredientType.Chocolate);

        if (playerRb != null)
        {
            Vector2 pushDir = ((Vector2)playerRb.position - rb.position).normalized;
            if (pushDir.sqrMagnitude < 0.01f)
            {
                pushDir = moveDirection;
            }

            Vector2 targetPos = playerRb.position + pushDir * knockbackDistance;
            playerRb.MovePosition(targetPos);
        }

        if (hasSugarRush)
        {
            ReverseDirection();
            return;
        }

        if (Time.time < nextAbilityStealTime) return;

        if (abilityController.TryConsumeAnyAbility(out IngredientType consumed))
        {
            nextAbilityStealTime = Time.time + abilityStealCooldownSeconds;
            if (consumed == IngredientType.Butter || consumed == IngredientType.Chocolate)
            {
                ReverseDirection();
            }
        }
    }

    private void ReverseDirection()
    {
        moveDirection = -moveDirection;
    }

    public void SetInitialDirection(Vector2 dir)
    {
        initialDirection = dir;

        moveDirection = GetSnappedDirection(initialDirection);
    }


    private static Vector2 GetSnappedDirection(Vector2 direction)
    {
        if (direction == Vector2.zero) return Vector2.right;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return new Vector2(Mathf.Sign(direction.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(direction.y));
    }
}
