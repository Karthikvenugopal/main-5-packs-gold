using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private PlayerAbilityController abilityController;

    public LayerMask wallLayer;

    private BoxCollider2D col;

    [Header("Mobile Controls")]
    [SerializeField] private bool enableMobileControlsInEditor = false;
    [SerializeField] private float mobileDeadZonePixels = 40f;
    [SerializeField] private float mobileMaxDragDistancePixels = 160f;

    private Vector2 touchStartPosition;
    private bool touchActive;

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
        if (ShouldUseMobileInput())
        {
            moveInput = ReadMobileMoveInput();
        }
        else
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            moveInput = new Vector2(moveX, moveY).normalized;
        }

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
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
                else if (hit.collider.TryGetComponent(out JamPatch jamPatch))
                {

                    if (jamPatch.TryScoop(abilityController))

                    {
                        blocked = false; 
                    }
                    else 
                    {
                        
                        GameManagerTutorial tutorialManager = FindFirstObjectByType<GameManagerTutorial>();
                        if (tutorialManager != null)
                        {
                            
                            tutorialManager.OnPlayerHitPeanutButter(); 
                        }
                    }
                }
            }
        }

        if (!blocked)
        {
            rb.MovePosition(nextPos);
        }
    }

    private bool ShouldUseMobileInput()
    {
        if (Application.isMobilePlatform) return true;
        return enableMobileControlsInEditor;
    }

    private Vector2 ReadMobileMoveInput()
    {
        if (Input.touchCount == 0)
        {
            touchActive = false;
            return Vector2.zero;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began || !touchActive)
        {
            touchActive = true;
            touchStartPosition = touch.position;
        }
        else if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
        {
            touchActive = false;
            return Vector2.zero;
        }

        Vector2 drag = touch.position - touchStartPosition;

        if (drag.sqrMagnitude < mobileDeadZonePixels * mobileDeadZonePixels)
        {
            return Vector2.zero;
        }

        float magnitudeRatio = 1f;

        if (mobileMaxDragDistancePixels > Mathf.Epsilon)
        {
            float max = mobileMaxDragDistancePixels;
            float magnitude = Mathf.Min(drag.magnitude, max);
            if (magnitude > 0f)
            {
                drag = drag.normalized * magnitude;
                magnitudeRatio = Mathf.Clamp01(magnitude / max);
            }
        }

        return drag.normalized * magnitudeRatio;
    }
}
