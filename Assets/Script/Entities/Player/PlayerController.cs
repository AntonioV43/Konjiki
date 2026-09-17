using UnityEngine;

[RequireComponent(typeof(InputControll))]
public class PlayerController : MonoBehaviour, IStatOwner
{
    #region State Machines

    private enum MoveState { Idle, Moving }
    private enum DashState { None, Dashing, Cooldown }
    private enum AttackState { None, Attacking }

    private MoveState moveState = MoveState.Idle;
    private DashState dashState = DashState.None;
    private AttackState attackState = AttackState.None;

    #endregion

    #region Inspector Settings

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
    public float attackDuration = 0.3f;
    public GameObject projectilePrefab;
    public Transform firePoint; // empty child GameObject positioned in front of the player
    public int attackDamage = 10;

    [Header("Stats")]
    [SerializeField] private CharacterStats stats = new CharacterStats { Name = "Player", MaxHP = 100 };

    [Header("UI")]
    [SerializeField] private HealthBar healthBar; // drag the player's HealthBar in the Inspector

    #endregion

    #region References & Runtime State

    private InputControll input;
    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private float dashTimer;
    private float dashCooldownTimer;
    private float attackTimer;

    #endregion

    #region IStatOwner

    public CharacterStats Stats => stats;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        input = GetComponent<InputControll>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        stats.ResetToFull();
        StatManager.Instance.Register(this);
        StatManager.Instance.OnHPChanged += HandleHPChanged;
        StatManager.Instance.OnDeath += HandleDeath;

        if (healthBar != null)
            healthBar.Initialize(this);
    }

    private void OnDisable()
    {
        if (StatManager.Instance == null) return;

        StatManager.Instance.Unregister(this);
        StatManager.Instance.OnHPChanged -= HandleHPChanged;
        StatManager.Instance.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        ReadInput();

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

    #endregion

    #region Input

    private void ReadInput()
    {
        moveInput = input.move;

        if (moveInput.sqrMagnitude > 0.01f)
            lastMoveDirection = moveInput.normalized;
    }

    #endregion

    #region Move State

    private void UpdateMoveState()
    {
        moveState = moveInput.sqrMagnitude > 0.01f ? MoveState.Moving : MoveState.Idle;
    }

    private void Move()
    {
        Vector2 direction = moveInput;

        if (normalizeDiagonal && direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector2 velocity = direction * moveSpeed;
        ApplyVelocity(velocity);
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

    #endregion

    #region Dash State

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

    private void DashMove()
    {
        Vector2 velocity = lastMoveDirection * dashSpeed;
        ApplyVelocity(velocity);
    }

    #endregion

    #region Attack State

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

        Shoot();
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        Vector2 shootDir = lastMoveDirection; // already tracks facing even when idle
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(shootDir, attackDamage);
    }

    #endregion

    #region Movement Helper

    private void ApplyVelocity(Vector2 velocity)
    {
        if (rb != null)
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        else
            transform.position += new Vector3(velocity.x, velocity.y, 0f) * Time.fixedDeltaTime;
    }

    #endregion

    #region Stat / Health Callbacks

    private void HandleHPChanged(IStatOwner owner, int oldHP, int newHP, int maxHP)
    {
        if (owner != (IStatOwner)this) return;
        Debug.Log($"{stats.Name} HP: {oldHP} -> {newHP}/{maxHP}");
    }

    private void HandleDeath(IStatOwner owner)
    {
        if (owner != (IStatOwner)this) return;
        Debug.Log($"{stats.Name} died.");
        // disable input, play death animation, trigger game over, etc.
    }

    #endregion

    #region Example Hooks

    // Example: player steps on a trap
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            StatManager.Instance.Damage(this, 15);
    }

    #endregion
}