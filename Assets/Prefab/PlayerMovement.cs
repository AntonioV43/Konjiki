using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float maxJumpTime = 0.2f;       // How long holding extends height
    [SerializeField] private float jumpHoldMultiplier = 0.5f; // Extra upward force while holding
    [SerializeField] private float fallMultiplier = 2.5f;    // Faster fall when descending
    [SerializeField] private float lowJumpMultiplier = 2f;   // Faster fall on short tap

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float coyoteTime = 0.1f;

    private Rigidbody2D rb;
    private InputControll controll;

    private bool isGrounded;
    private float coyoteTimeCounter;

    private bool isJumping;
    private float jumpTimeCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controll = GetComponent<InputControll>();
    }

    private void Update()
    {
        GroundCheck();
        Movement();
        Jump();
        Attack();
    }

    // ─── Ground Check ────────────────────────────────────────────────────────

    private void GroundCheck()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;
    }

    // ─── Movement ────────────────────────────────────────────────────────────

    private void Movement()
    {
        rb.linearVelocity = new Vector2(controll.move.x * speed, rb.linearVelocity.y);
    }

    // ─── Jump ────────────────────────────────────────────────────────────────

    private void Jump()
    {
        // 1. Start jump — only from ground / coyote window
        if (controll.jumpPressed && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            coyoteTimeCounter = 0f;
        }

        // 2. Variable height extension — only while still in the jump window
        if (isJumping)
        {
            if (controll.jumpHeld && jumpTimeCounter > 0f)
            {
                // Add extra upward force each frame the button is held
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y + jumpHoldMultiplier * jumpForce * Time.deltaTime
                );
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                // Button released OR window expired — stop extending
                isJumping = false;
            }
        }

        // 3. Gravity feel — fast fall / low-jump arc
        if (rb.linearVelocity.y < 0f)
        {
            // Falling: apply extra gravity for snappy descent
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !controll.jumpHeld)
        {
            // Rising but button released: cut the arc short
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
        }
    }

    // ─── Attack ──────────────────────────────────────────────────────────────

    private void Attack()
    {
        if (controll.attackPressed)
        {
            Debug.Log("Attack!");
            controll.attackPressed = false;
        }
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}