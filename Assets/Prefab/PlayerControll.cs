using UnityEngine;

[RequireComponent(typeof(InputControll))]
public class PlayerControll : MonoBehaviour
{
    // --- Independent state machines ---
    private enum MoveState { Idle, Moving }
    private enum DashState { None, Dashing, Cooldown }
    private enum AttackState { None, Attacking }

    private InputControll input;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public bool normalizeDiagonal = true;

    [Header("Rotation")]
    public bool rotateTowardsMovement = true;
    public float rotationSpeed = 720f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Header("Attack Settings")]
    public float attackDuration = 0.3f; // duration of attack "busy" state (doesn't block movement)

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private MoveState moveState = MoveState.Idle;
    private DashState dashState = DashState.None;
    private AttackState attackState = AttackState.None;

    private float dashTimer;
    private float dashCooldownTimer;
    private float attackTimer;

    private void Awake()
    {
        input = GetComponent<InputControll>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        move();

        UpdateMoveState();
        UpdateDashState();
        UpdateAttackState();

        // Rotation: allowed any time you're not mid-dash (dash direction is locked)
        if (rotateTowardsMovement && moveInput.sqrMagnitude > 0.01f && dashState != DashState.Dashing)
            RotateTowardsMovement();
    }

    private void FixedUpdate()
    {
        // Dash always overrides normal movement while active.
        // Attack does NOT block movement — you can walk and attack at the same time.
        if (dashState == DashState.Dashing)
            DashMove();
        else
            Move();
    }

    private void move()
    {
        moveInput = input.move;

        if (moveInput.sqrMagnitude > 0.01f)
            lastMoveDirection = moveInput.normalized;
    }

    // ---------------- Move state ----------------

    private void UpdateMoveState()
    {
        moveState = moveInput.sqrMagnitude > 0.01f ? MoveState.Moving : MoveState.Idle;
    }

    // ---------------- Dash state ----------------

    private void UpdateDashState()
    {
        switch (dashState)
        {
            case DashState.None:
                if (input.dashPressed)
                    EnterDash();
                break;

            case DashState.Dashing:
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                    ExitDash();
                break;

            case DashState.Cooldown:
                dashCooldownTimer -= Time.deltaTime;
                if (dashCooldownTimer <= 0f)
                    dashState = DashState.None;
                break;
        }
    }

    private void EnterDash()
    {
        dashState = DashState.Dashing;
        dashTimer = dashDuration;
        input.dashPressed = false;
    }

    private void ExitDash()
    {
        dashState = DashState.Cooldown;
        dashCooldownTimer = dashCooldown;
    }

    // ---------------- Attack state ----------------

    private void UpdateAttackState()
    {
        switch (attackState)
        {
            case AttackState.None:
                if (input.attackPressed)
                    EnterAttack();
                break;

            case AttackState.Attacking:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                    attackState = AttackState.None;
                break;
        }
    }

    private void EnterAttack()
    {
        attackState = AttackState.Attacking;
        attackTimer = attackDuration;
        input.attackPressed = false;
        // hook your actual attack/animation/hitbox trigger here
    }

    // ---------------- Movement ----------------

    private void Move()
    {
        Vector2 direction = moveInput;

        if (normalizeDiagonal && direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector2 velocity = direction * moveSpeed;

        if (rb != null)
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        else
            transform.position += new Vector3(velocity.x, velocity.y, 0f) * Time.fixedDeltaTime;
    }

    private void DashMove()
    {
        Vector2 velocity = lastMoveDirection * dashSpeed;

        if (rb != null)
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        else
            transform.position += new Vector3(velocity.x, velocity.y, 0f) * Time.fixedDeltaTime;
    }

    private void RotateTowardsMovement()
    {
        float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle - 90f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}