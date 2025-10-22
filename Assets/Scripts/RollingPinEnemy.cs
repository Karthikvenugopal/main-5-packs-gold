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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        moveDirection = GetSnappedDirection(initialDirection);

        if (obstacleLayers == 0)
        {
            obstacleLayers = LayerMask.GetMask("Wall");
        }
    }

    private void FixedUpdate()
    {
        if (moveDirection.sqrMagnitude < Mathf.Epsilon)
            return;

        Vector2 delta = moveDirection * moveSpeed * Time.fixedDeltaTime;
        Vector2 nextPosition = rb.position + delta;

        float checkDistance = delta.magnitude + 0.05f;
        RaycastHit2D wallHit = Physics2D.CircleCast(rb.position, 0.35f, moveDirection, checkDistance, obstacleLayers);
        if (wallHit.collider != null)
        {
            ReverseDirection();
            return;
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
