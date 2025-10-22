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
        {
            return;
        }

        Vector2 delta = moveDirection * moveSpeed * Time.fixedDeltaTime;
        Vector2 nextPosition = rb.position + delta;

        float checkDistance = delta.magnitude + 0.05f;
        RaycastHit2D hit = Physics2D.CircleCast(rb.position, 0.35f, moveDirection, checkDistance, obstacleLayers);
        if (hit.collider != null)
        {
            ReverseDirection();
            return;
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

        if (!collision.collider.TryGetComponent(out PlayerAbilityController abilityController))
        {
            return;
        }

        HandlePlayerCollision(abilityController, collision.collider.attachedRigidbody);
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
