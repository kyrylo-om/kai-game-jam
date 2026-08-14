
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxMoveSpeed = 12f;
    public float groundAcceleration = 90f;
    public float groundDeceleration = 65f;
    public float airAcceleration = 55f;
    public float airDeceleration = 40f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.5f;
    public float sprintAcceleration = 120f;

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
    private bool isGrounded;
    private bool isWallSliding;
    private bool isHanging;
    private int hangDirection;
    private float moveInput;
    private bool isSprinting;
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
        moveInput = Input.GetAxisRaw("Horizontal");
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Tick grab cooldown
        if (grabCooldownTimer > 0f)
            grabCooldownTimer -= Time.deltaTime;

        if (isHanging)
        {
            // Release hang only when pressing S (down)
            if (Input.GetKeyDown(KeyCode.S))
            {
                isHanging = false;
                grabCooldownTimer = grabCooldown;
                return;
            }

            // Jump from edge
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isHanging = false;
                rb.linearVelocity = new Vector2(hangDirection * maxMoveSpeed * 0.5f, edgeClimbJumpForce);
                return;
            }

            return; // No other input while hanging
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Variable jump height — cut velocity short when releasing jump early
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

        if (isHanging)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
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

        // --- Edge detection (only when falling and not on cooldown) ---
        if (!isGrounded && rb.linearVelocity.y < 0f && grabCooldownTimer <= 0f)
            TryEdgeGrab();

        if (isWallSliding)
        {
            // Stick to wall horizontally
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Accelerate downward while sliding, preserve upward momentum
            float targetSlideY = -maxWallSlideSpeed;
            if (rb.linearVelocity.y > 0f)
                targetSlideY = rb.linearVelocity.y;

            rb.linearVelocity = new Vector2(0f,
                Mathf.MoveTowards(rb.linearVelocity.y, targetSlideY, wallSlideAcceleration * Time.fixedDeltaTime));
        }
        else
        {
            // --- Horizontal acceleration / deceleration via velocity smoothing ---
            float speedMultiplier = isSprinting ? sprintMultiplier : 1f;
            float targetSpeed = moveInput * maxMoveSpeed * speedMultiplier;

            float accelRate;
            if (isGrounded)
                accelRate = Mathf.Abs(targetSpeed) > 0.01f
                    ? (isSprinting ? sprintAcceleration : groundAcceleration)
                    : groundDeceleration;
            else
                accelRate = Mathf.Abs(targetSpeed) > 0.01f
                    ? airAcceleration
                    : airDeceleration;

            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }

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

        // Check right side (always, no input required)
        {
            Vector2 wallOrigin = new Vector2(bounds.max.x, midY);
            Vector2 gapOrigin = new Vector2(bounds.max.x, topY);
            bool wallHit = Physics2D.Raycast(wallOrigin, Vector2.right, edgeCheckDistance, groundLayer);
            bool gapClear = !Physics2D.Raycast(gapOrigin, Vector2.right, edgeCheckDistance, groundLayer);

            if (wallHit && gapClear)
            {
                isHanging = true;
                hangDirection = 1;
                return;
            }
        }

        // Check left side (always, no input required)
        {
            Vector2 wallOrigin = new Vector2(bounds.min.x, midY);
            Vector2 gapOrigin = new Vector2(bounds.min.x, topY);
            bool wallHit = Physics2D.Raycast(wallOrigin, Vector2.left, edgeCheckDistance, groundLayer);
            bool gapClear = !Physics2D.Raycast(gapOrigin, Vector2.left, edgeCheckDistance, groundLayer);

            if (wallHit && gapClear)
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
