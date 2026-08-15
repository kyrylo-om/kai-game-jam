using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxMoveSpeed = 12f;
    public float sprintMultiplier = 1.5f; // Added for GDD Sprint mechanic
    public float groundAcceleration = 90f;
    public float groundDeceleration = 65f;
    public float airAcceleration = 55f;
    public float airDeceleration = 40f;

    [Header("Jump")]
    public float jumpForce = 15f;
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.4f;
    public float fallGravityMultiplier = 2.5f;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Wall Slide")]
    public float wallCheckDistance = 0.3f;
    public float maxWallSlideSpeed = 4f;
    public float wallSlideAcceleration = 30f;

    [Header("Edge Climb")]
    public float edgeCheckDistance = 0.3f;
    public float edgeClimbJumpForce = 12f;
    public float grabCooldown = 0.3f;

    private Rigidbody2D rb;
    private Collider2D col;

    // State Tracking
    private bool isGrounded;
    private bool isWallSliding;
    private bool isHanging;
    private int hangDirection;

    // Input Caching (Fixes missed inputs in FixedUpdate)
    private float moveInput;
    private bool isSprinting;
    private bool jumpRequested;
    private bool jumpCanceled;
    private bool dropRequested;

    private float defaultGravityScale;
    private float grabCooldownTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        defaultGravityScale = rb.gravityScale;
    }

    void Update()
    {
        // 1. Gather all input in Update
        moveInput = Input.GetAxisRaw("Horizontal");
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (grabCooldownTimer > 0f)
            grabCooldownTimer -= Time.deltaTime;

        if (isHanging)
        {
            // Drop down with S
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                dropRequested = true;

            // NEW: Pull away from the wall to drop (A or D)
            // If hanging on right wall (1) and moving left (< 0), OR hanging on left wall (-1) and moving right (> 0)
            if ((hangDirection == 1 && moveInput < -0.1f) || (hangDirection == -1 && moveInput > 0.1f))
            {
                dropRequested = true;
            }

            // Jump away from the edge
            if (Input.GetKeyDown(KeyCode.Space))
                jumpRequested = true;

            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            jumpRequested = true;

        if (Input.GetKeyUp(KeyCode.Space))
            jumpCanceled = true;
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

        // 2. Handle Hanging State
        if (isHanging)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;

            if (dropRequested)
            {
                isHanging = false;
                grabCooldownTimer = grabCooldown;
                dropRequested = false;
            }
            else if (jumpRequested)
            {
                isHanging = false;
                rb.linearVelocity = new Vector2(hangDirection * maxMoveSpeed * 0.5f, edgeClimbJumpForce);
                jumpRequested = false;
            }
            return;
        }

        // --- Wall detection ---
        Bounds bounds = col.bounds;
        Vector2 rightOrigin = new Vector2(bounds.max.x, bounds.center.y);
        Vector2 leftOrigin = new Vector2(bounds.min.x, bounds.center.y);
        bool touchingRightWall = Physics2D.Raycast(rightOrigin, Vector2.right, wallCheckDistance, groundLayer);
        bool touchingLeftWall = Physics2D.Raycast(leftOrigin, Vector2.left, wallCheckDistance, groundLayer);
        bool pressingIntoWall = (moveInput > 0.01f && touchingRightWall) || (moveInput < -0.01f && touchingLeftWall);

        isWallSliding = !isGrounded && pressingIntoWall;

        // --- Edge detection ---
        if (!isGrounded && rb.linearVelocity.y < 0f && grabCooldownTimer <= 0f)
            TryEdgeGrab();

        // 3. Process Physics Movement
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            float targetSlideY = rb.linearVelocity.y > 0f ? rb.linearVelocity.y : -maxWallSlideSpeed;
            rb.linearVelocity = new Vector2(0f, Mathf.MoveTowards(rb.linearVelocity.y, targetSlideY, wallSlideAcceleration * Time.fixedDeltaTime));
        }
        else
        {
            // Apply Sprint Multiplier
            float currentMaxSpeed = isSprinting ? maxMoveSpeed * sprintMultiplier : maxMoveSpeed;
            float targetSpeed = moveInput * currentMaxSpeed;

            float accelRate = isGrounded
                ? (Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration)
                : (Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration);

            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }

        // 4. Consume Jump Inputs
        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        jumpRequested = false; // Always consume the request

        if (jumpCanceled && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
        jumpCanceled = false; // Always consume the cancel

        // --- Better falling feel ---
        float gravity = rb.linearVelocity.y < 0f
            ? defaultGravityScale * fallGravityMultiplier
            : defaultGravityScale;
        rb.gravityScale = gravity;
    }

    void TryEdgeGrab()
    {
        Bounds bounds = col.bounds;
        float midY = bounds.center.y;
        float topY = bounds.max.y + 0.05f;

        // Check right side
        {
            Vector2 wallOrigin = new Vector2(bounds.max.x, midY);
            Vector2 gapOrigin = new Vector2(bounds.max.x, topY);
            if (Physics2D.Raycast(wallOrigin, Vector2.right, edgeCheckDistance, groundLayer) &&
               !Physics2D.Raycast(gapOrigin, Vector2.right, edgeCheckDistance, groundLayer))
            {
                isHanging = true;
                hangDirection = 1;
                return;
            }
        }

        // Check left side
        {
            Vector2 wallOrigin = new Vector2(bounds.min.x, midY);
            Vector2 gapOrigin = new Vector2(bounds.min.x, topY);
            if (Physics2D.Raycast(wallOrigin, Vector2.left, edgeCheckDistance, groundLayer) &&
               !Physics2D.Raycast(gapOrigin, Vector2.left, edgeCheckDistance, groundLayer))
            {
                isHanging = true;
                hangDirection = -1;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
