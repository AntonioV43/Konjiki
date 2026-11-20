using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(InputControll))]
public class PlayerController : MonoBehaviour, IStatOwner
{
    #region State Machines

    private enum MoveState { Idle, Moving }
    private enum DashState { None, Dashing, Cooldown }

    private MoveState moveState = MoveState.Idle;
    private DashState dashState = DashState.None;

    #endregion

    #region Inspector Settings

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public bool normalizeDiagonal = true;

    [Header("Rotation")]
    public bool rotateTowardsMouse = true;
    public bool rotateTowardsMovement = false;
    public float rotationSpeed = 720f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Header("Attack Settings")]
    [Tooltip("Jumlah tembakan per detik")]
    public float fireRate = 3f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 12f;
    [Tooltip("Jarak tempuh maksimal peluru dalam satuan unit")]
    public float projectileRange = 12f;

    [Header("Stats (Single Source of Damage)")]
    [SerializeField] private CharacterStats stats = new CharacterStats { Name = "Player", MaxHP = 100, Attack = 10 };

    [Header("UI")]
    [SerializeField] private UI_HealthBar healthBar;

    #endregion

    #region References & Runtime State

    private InputControll input;
    private Rigidbody2D rb;
    private Camera mainCam;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private float dashTimer;
    private float dashCooldownTimer;
    private float nextAttackTime;

    public float AttackInterval => 1f / Mathf.Max(0.01f, fireRate);

    private ProjectileModifierHandler modifierHandler;

    #endregion

    #region IStatOwner

    public CharacterStats Stats => stats;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        input = GetComponent<InputControll>();
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        modifierHandler = GetComponent<ProjectileModifierHandler>();
    }

    private void OnEnable()
    {
        stats.ResetToFull();
        StartCoroutine(RegisterWhenReady());
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        yield return new WaitUntil(() => StatManager.Instance != null);

        StatManager.Instance.Register(this);
        StatManager.Instance.OnHPChanged += HandleHPChanged;
        StatManager.Instance.OnDeath += HandleDeath;

        if (healthBar != null)
            healthBar.Initialize(this);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (StatManager.Instance != null)
        {
            StatManager.Instance.OnHPChanged -= HandleHPChanged;
            StatManager.Instance.OnDeath -= HandleDeath;
            StatManager.Instance.Unregister(this);
        }
    }

    private void Update()
    {
        ReadInput();

        UpdateMoveState();
        UpdateDashState();
        UpdateAttack();

        if (dashState != DashState.Dashing)
        {
            if (rotateTowardsMouse)
                RotateTowardsMouse();
            else if (rotateTowardsMovement && moveInput.sqrMagnitude > 0.01f)
                RotateTowardsMovement();
        }
    }

    private void FixedUpdate()
    {
        if (dashState == DashState.Dashing)
            DashMove();
        else
            Move();
    }

    #endregion

    #region Input & Aim Helper

    private void ReadInput()
    {
        moveInput = input.move;

        if (moveInput.sqrMagnitude > 0.01f)
            lastMoveDirection = moveInput.normalized;
    }

    private bool TryGetMouseWorldPosition(out Vector3 mouseWorldPos)
    {
        mouseWorldPos = Vector3.zero;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || Mouse.current == null) return false;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mouseScreenPos);
        Plane xyPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));

        if (xyPlane.Raycast(ray, out float distance))
        {
            mouseWorldPos = ray.GetPoint(distance);
            return true;
        }

        return false;
    }

    #endregion

    #region Move & Rotation

    private void UpdateMoveState()
    {
        moveState = moveInput.sqrMagnitude > 0.01f ? MoveState.Moving : MoveState.Idle;
    }

    private void Move()
    {
        Vector2 direction = moveInput;

        if (normalizeDiagonal && direction.sqrMagnitude > 1f)
            direction.Normalize();

        ApplyVelocity(direction * moveSpeed);
    }

    private void RotateTowardsMouse()
    {
        if (!TryGetMouseWorldPosition(out Vector3 mouseWorldPos)) return;

        Vector2 lookDirection = (Vector2)(mouseWorldPos - transform.position);
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle - 90f);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
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
        ApplyVelocity(lastMoveDirection * dashSpeed);
    }

    #endregion

    #region Attack (Fire Rate Interval)

    private void UpdateAttack()
    {
        bool wantsAttack = input.attackHeld || input.attackBuffered;
        input.attackBuffered = false;

        if (wantsAttack && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + AttackInterval;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        ProjectileSpawnPlan plan = modifierHandler != null
            ? modifierHandler.BuildSpawnPlan(projectileSpeed, projectileRange, stats.Attack)
            : new ProjectileSpawnPlan
            {
                burstCount = 1,
                radialCount = 1,
                spreadCount = 1,
                spreadAngle = 0f,
                burstInterval = 0f,
                speed = projectileSpeed,
                range = projectileRange,
                sizeMultiplier = 1f,
                damage = stats.Attack
            };

        StartCoroutine(SpawnProjectileSequence(plan));
    }

    private System.Collections.IEnumerator SpawnProjectileSequence(ProjectileSpawnPlan plan)
    {
        WaitForSeconds wait = plan.burstInterval > 0f ? new WaitForSeconds(plan.burstInterval) : null;

        for (int b = 0; b < plan.burstCount; b++)
        {
            Vector3 baseSpawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 aimDir = GetConsistentShootDirection(baseSpawnPos);

            float radialStep = 360f / plan.radialCount;
            for (int r = 0; r < plan.radialCount; r++)
            {
                Vector2 radialDir = RotateVector(aimDir, radialStep * r);

                for (int s = 0; s < plan.spreadCount; s++)
                {
                    float spreadOffset = 0f;
                    if (plan.spreadCount > 1)
                    {
                        float normalizedPos = (float)s / (plan.spreadCount - 1) - 0.5f;
                        spreadOffset = normalizedPos * plan.spreadAngle;
                    }

                    Vector2 finalDir = RotateVector(radialDir, spreadOffset);
                    Vector3 spawnOffset = Vector3.zero;

                    if (modifierHandler != null)
                        modifierHandler.ApplyTrajectoryModifiers(s, plan.spreadCount, ref finalDir, ref spawnOffset);

                    SpawnSingleProjectile(baseSpawnPos + spawnOffset, finalDir, plan);
                }
            }

            if (wait != null && b < plan.burstCount - 1)
                yield return wait;
        }
    }

    private void SpawnSingleProjectile(Vector3 spawnPos, Vector2 shootDir, ProjectileSpawnPlan plan)
    {
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        Quaternion projRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        GameObject projObj = Instantiate(projectilePrefab, spawnPos, projRotation);
        projObj.transform.localScale *= plan.sizeMultiplier;

        if (projObj.TryGetComponent<Projectile>(out var proj))
        {
            ProjectileRuntimeState state = modifierHandler != null
                ? modifierHandler.CreateRuntimeState(isOrbiting: false)
                : new ProjectileRuntimeState();

            proj.Init(this, shootDir, plan.speed, plan.range, plan.damage, state);
        }
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y).normalized;
    }

    private Vector2 GetConsistentShootDirection(Vector3 spawnPos)
    {
        if (rotateTowardsMouse && TryGetMouseWorldPosition(out Vector3 mouseWorldPos))
        {
            Vector2 aimDir = (Vector2)(mouseWorldPos - spawnPos);
            if (aimDir.sqrMagnitude > 0.0001f)
                return aimDir.normalized;
        }

        if (rotateTowardsMouse)
            return ((Vector2)transform.up).sqrMagnitude > 0.0001f ? ((Vector2)transform.up).normalized : Vector2.up;

        return lastMoveDirection.sqrMagnitude > 0.0001f ? lastMoveDirection.normalized : Vector2.down;
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
    }

    #endregion

    #region Example Hooks

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            StatManager.Instance.Damage(this, 15);
    }

    #endregion
}