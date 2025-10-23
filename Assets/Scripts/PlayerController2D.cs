using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private PlayerAbilityController abilityController;

    public LayerMask wallLayer;

    private BoxCollider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        abilityController = GetComponent<PlayerAbilityController>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        //if (Mathf.Abs(moveX) > 0.01f) moveY = 0f;

        moveInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        if (moveInput == Vector2.zero) return;

        float speedMultiplier = 1f;
        if (abilityController != null)
        {
            speedMultiplier = abilityController.GetMoveSpeedMultiplier();
        }

        Vector2 moveAmount = moveInput * moveSpeed * speedMultiplier * Time.fixedDeltaTime;
        Vector2 nextPos = rb.position + moveAmount;

        RaycastHit2D hit = Physics2D.BoxCast(rb.position, col.size * 0.9f, 0f, moveInput, moveAmount.magnitude, wallLayer);
        bool blocked = hit.collider != null;

        if (blocked) 
        {
            if (hit.collider.CompareTag("RightWall"))
            {
                GameManagerTutorial tutorialManager = FindFirstObjectByType<GameManagerTutorial>();
                if (tutorialManager != null)
                {
                    tutorialManager.OnPlayerHitRightWall();
                    return; 
                }
            }

            if (abilityController != null)
            {
                if (hit.collider.TryGetComponent(out IceWall iceWall))
                {
                    if (iceWall.TryMelt(abilityController))
                    {
                        blocked = false;
                    }
                    else
                    {
                        GameManagerTutorial tutorialManager = FindFirstObjectByType<GameManagerTutorial>();
                        if (tutorialManager != null)
                        {
                            tutorialManager.OnPlayerHitIceWall();
                        }
                    }
                }
                else if (hit.collider.TryGetComponent(out PeanutButterPatch peanutPatch))
                {
                    if (peanutPatch.TryScoop(abilityController))
                    {
                        blocked = false;
                    }
                }
            }
        }

        if (!blocked)
        {
            rb.MovePosition(nextPos);
        }
    }
}
