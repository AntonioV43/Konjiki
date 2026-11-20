using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Collision Layers")]
    public LayerMask hitMask;  // Set ke layer "Enemy"
    public LayerMask wallMask; // Set ke layer "Wall" pada Tilemap tembok

    private IStatOwner attacker;
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float traveledDistance;
    private int damage;
    private float elapsedTime;
    private Rigidbody2D rb;
    private CircleCollider2D circleCol;
    private ProjectileRuntimeState state;
    private readonly Dictionary<int, float> hitNextAllowedTime = new Dictionary<int, float>();

    public Vector2 Direction => direction;
    public float Speed => speed;
    public float MaxRange => maxRange;
    public float TraveledDistance => traveledDistance;
    public int Damage => damage;
    public ProjectileRuntimeState State => state;

    private void Awake()
    {
        TryGetComponent(out circleCol);

        if (TryGetComponent(out rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
        }
    }

    public void Init(IStatOwner owner, Vector2 dir, float moveSpeed, float travelRange, int dmg, ProjectileRuntimeState runtimeState = null)
    {
        attacker = owner;
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        speed = Mathf.Max(0.1f, moveSpeed);
        maxRange = Mathf.Max(0.1f, travelRange);
        traveledDistance = 0f;
        damage = dmg;
        elapsedTime = 0f;
        state = runtimeState ?? new ProjectileRuntimeState();
        hitNextAllowedTime.Clear();

        foreach (var mod in state.activeModifiers)
            mod.OnProjectileInit(this, state);

        UpdateRotation(direction);

        if (!state.isOrbiting)
        {
            if (wallMask.value != 0 && IsOverlappingWall())
            {
                Destroy(gameObject);
                return;
            }

            Destroy(gameObject, (maxRange / speed) + 2f);
        }
    }

    public void SetDirection(Vector2 newDir)
    {
        if (newDir.sqrMagnitude <= 0.0001f) return;
        direction = newDir.normalized;
        UpdateRotation(direction);
    }

    public void SetOrbitTransform(Vector2 worldPos, Vector2 tangentDir)
    {
        if (rb != null)
            rb.MovePosition(worldPos);
        else
            transform.position = worldPos;

        if (tangentDir.sqrMagnitude > 0.0001f)
        {
            direction = tangentDir.normalized;
            UpdateRotation(direction);
        }
    }

    private float GetColliderRadius()
    {
        return circleCol != null
            ? circleCol.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y)
            : 0.1f;
    }

    private bool IsOverlappingWall()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallMask);
        filter.useTriggers = true;

        Collider2D[] overlaps = new Collider2D[1];
        return Physics2D.OverlapCircle(transform.position, GetColliderRadius(), filter, overlaps) > 0;
    }

    private void FixedUpdate()
    {
        // Pergerakan peluru Orbit dikendalikan langsung oleh ProjectileModifierHandler
        if (state != null && state.isOrbiting)
            return;

        float dt = Time.fixedDeltaTime;
        elapsedTime += dt;

        Vector2 moveStep;
        if (state != null && state.waveFrequency > 0f && state.waveAmplitude > 0f)
        {
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float waveOffset = Mathf.Sin(elapsedTime * state.waveFrequency) * state.waveAmplitude;
            Vector2 finalVelocity = (direction * speed) + (perpendicular * waveOffset);
            moveStep = finalVelocity * dt;
        }
        else
        {
            moveStep = direction * (speed * dt);
        }

        float stepDistance = moveStep.magnitude;
        if (stepDistance <= 0.0001f) return;

        float remainingRange = maxRange - traveledDistance;
        if (stepDistance >= remainingRange)
        {
            moveStep = moveStep.normalized * remainingRange;
            stepDistance = remainingRange;
        }

        if (wallMask.value != 0 && TryHandleWallSweep(moveStep.normalized, stepDistance))
            return;

        Vector2 nextPos = (Vector2)transform.position + moveStep;
        if (rb != null)
            rb.MovePosition(nextPos);
        else
            transform.position = nextPos;

        traveledDistance += stepDistance;
        UpdateRotation(moveStep);

        if (traveledDistance >= maxRange)
            Destroy(gameObject);
    }

    private bool TryHandleWallSweep(Vector2 moveDir, float distance)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallMask);
        filter.useTriggers = true;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        int hitCount = Physics2D.CircleCast(transform.position, GetColliderRadius(), moveDir, filter, hits, distance + 0.02f);

        if (hitCount == 0) return false;

        RaycastHit2D hit = hits[0];
        if (Vector2.Dot(moveDir, hit.normal) >= 0f) return false;

        traveledDistance += hit.distance;

        if (state != null && state.remainingBounce > 0 && traveledDistance < maxRange)
        {
            state.remainingBounce--;
            Vector2 safePos = hit.centroid + hit.normal * 0.02f;

            if (rb != null)
                rb.MovePosition(safePos);
            else
                transform.position = safePos;

            Vector2 reflected = Vector2.Reflect(direction, hit.normal).normalized;
            SetDirection(reflected);
            return true;
        }

        Destroy(gameObject);
        return true;
    }

    private void UpdateRotation(Vector2 lookDir)
    {
        if (lookDir.sqrMagnitude <= 0.0001f) return;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayerBit = 1 << other.gameObject.layer;

        if ((otherLayerBit & wallMask) != 0)
        {
            if (state != null && state.isOrbiting) return;
            if (state == null || state.remainingBounce <= 0)
                Destroy(gameObject);
            return;
        }

        if ((otherLayerBit & hitMask) == 0)
            return;

        if (!other.TryGetComponent<EnemyController>(out var enemy))
            return;

        int targetId = enemy.GetInstanceID();
        if (hitNextAllowedTime.TryGetValue(targetId, out float nextAllowed) && Time.time < nextAllowed)
            return;

        hitNextAllowedTime[targetId] = state.isOrbiting ? Time.time + 0.45f : float.PositiveInfinity;

        if (StatManager.Instance != null)
            StatManager.Instance.DealDamageWithDefense(attacker, enemy, damage);

        Vector2 hitPoint = other.ClosestPoint(transform.position);
        foreach (var mod in state.activeModifiers)
            mod.OnProjectileHit(this, attacker, enemy, hitPoint);

        if (state.remainingPierce > 0)
        {
            state.remainingPierce--;
            return;
        }

        if (!state.isOrbiting && state.bounceOnEnemy && state.remainingBounce > 0)
        {
            state.remainingBounce--;
            Vector2 normal = ((Vector2)transform.position - hitPoint).normalized;
            if (normal.sqrMagnitude < 0.001f) normal = -direction;

            Vector2 reflected = Vector2.Reflect(direction, normal).normalized;
            transform.position += (Vector3)(normal * 0.1f);
            SetDirection(reflected);
            return;
        }

        Destroy(gameObject);
    }
}