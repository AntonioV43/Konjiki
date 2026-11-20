using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProjectileModifierHandler : MonoBehaviour
{
    [SerializeField] private List<ProjectileModifierSO> activeModifiers = new List<ProjectileModifierSO>();

    private class OrbitSlot
    {
        public Projectile instance;
        public float resupplyTimer;
    }

    private PlayerController playerController;
    private readonly List<OrbitSlot> orbitSlots = new List<OrbitSlot>();
    private float currentOrbitAngle;

    public IReadOnlyList<ProjectileModifierSO> ActiveModifiers => activeModifiers;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnDisable()
    {
        ClearAllOrbitals();
    }

    public void AddModifier(ProjectileModifierSO modifier)
    {
        if (modifier == null) return;
        activeModifiers.Add(modifier);
    }

    public void RemoveModifier(ProjectileModifierSO modifier)
    {
        if (modifier == null) return;
        activeModifiers.Remove(modifier);
    }

    public ProjectileSpawnPlan BuildSpawnPlan(float baseSpeed, float baseRange, int baseDamage)
    {
        ProjectileSpawnPlan plan = new ProjectileSpawnPlan
        {
            burstCount = 1,
            radialCount = 1,
            spreadCount = 1,
            spreadAngle = 0f,
            burstInterval = 0f,
            speed = baseSpeed,
            range = baseRange,
            sizeMultiplier = 1f,
            damage = baseDamage
        };

        // Tahap 1: Utils
        foreach (var mod in activeModifiers.Where(m => m.category == ModifierCategory.Utils))
            mod.ApplyUtils(ref plan);

        // Tahap 2: Quantity
        foreach (var mod in activeModifiers.Where(m => m.category == ModifierCategory.Quantity))
            mod.ApplyQuantity(ref plan);

        plan.burstCount = Mathf.Max(1, plan.burstCount);
        plan.radialCount = Mathf.Max(1, plan.radialCount);
        plan.spreadCount = Mathf.Max(1, plan.spreadCount);
        plan.range = Mathf.Max(0.1f, plan.range);
        return plan;
    }

    public void ApplyTrajectoryModifiers(int index, int totalCount, ref Vector2 direction, ref Vector3 spawnOffset)
    {
        foreach (var mod in activeModifiers.Where(m => m.category == ModifierCategory.Trajectory))
            mod.ApplyTrajectory(index, totalCount, ref direction, ref spawnOffset);

        if (direction.sqrMagnitude > 0.0001f)
            direction.Normalize();
    }

    public ProjectileRuntimeState CreateRuntimeState(bool isOrbiting = false)
    {
        var state = new ProjectileRuntimeState { isOrbiting = isOrbiting };
        state.activeModifiers.AddRange(activeModifiers);
        return state;
    }

    #region Orbit Auto-Spawner & Resupply

    private void FixedUpdate()
    {
        UpdateOrbitSystem();
    }

    private void UpdateOrbitSystem()
    {
        if (playerController == null || playerController.projectilePrefab == null) return;

        int totalOrbitals = 0;
        float radius = 0f;
        float angularSpeed = 0f;
        float damageMult = 0f;
        float resupplyCooldown = float.MaxValue;
        int orbitModCount = 0;

        foreach (var mod in activeModifiers)
        {
            if (mod is TrajectoryModifierSO traj && traj.trajectoryType == TrajectoryType.Orbit)
            {
                totalOrbitals += traj.orbitCount;
                radius = Mathf.Max(radius, traj.orbitRadius);
                angularSpeed = Mathf.Max(angularSpeed, traj.orbitAngularSpeed);
                damageMult += traj.orbitDamageMultiplier;
                resupplyCooldown = Mathf.Min(resupplyCooldown, traj.orbitResupplyCooldown);
                orbitModCount++;
            }
        }

        if (orbitModCount == 0 || totalOrbitals <= 0)
        {
            if (orbitSlots.Count > 0)
                ClearAllOrbitals();
            return;
        }

        float finalDamageMult = Mathf.Clamp(damageMult / orbitModCount, 0.05f, 1f);

        // Sesuaikan jumlah slot jika modifier Orbit baru ditambahkan atau dihapus
        while (orbitSlots.Count < totalOrbitals)
            orbitSlots.Add(new OrbitSlot { instance = null, resupplyTimer = 0f });

        while (orbitSlots.Count > totalOrbitals)
        {
            int last = orbitSlots.Count - 1;
            if (orbitSlots[last].instance != null)
                Destroy(orbitSlots[last].instance.gameObject);
            orbitSlots.RemoveAt(last);
        }

        currentOrbitAngle = Mathf.Repeat(currentOrbitAngle + angularSpeed * Time.fixedDeltaTime, 360f);
        float angleStep = 360f / totalOrbitals;
        Vector2 center = transform.position;

        for (int i = 0; i < totalOrbitals; i++)
        {
            OrbitSlot slot = orbitSlots[i];
            float angleDeg = currentOrbitAngle + (angleStep * i);
            float rad = angleDeg * Mathf.Deg2Rad;

            Vector2 radialDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 targetPos = center + radialDir * radius;
            Vector2 tangentDir = angularSpeed >= 0f
                ? new Vector2(-radialDir.y, radialDir.x)
                : new Vector2(radialDir.y, -radialDir.x);

            if (slot.instance != null)
            {
                slot.instance.SetOrbitTransform(targetPos, tangentDir);
            }
            else
            {
                slot.resupplyTimer -= Time.fixedDeltaTime;
                if (slot.resupplyTimer <= 0f)
                {
                    slot.instance = SpawnOrbitalProjectile(targetPos, tangentDir, finalDamageMult);
                    slot.resupplyTimer = resupplyCooldown;
                }
            }
        }
    }

    private Projectile SpawnOrbitalProjectile(Vector2 spawnPos, Vector2 tangentDir, float damageMultiplier)
    {
        ProjectileSpawnPlan plan = BuildSpawnPlan(
            playerController.projectileSpeed,
            playerController.projectileRange,
            playerController.Stats.Attack
        );

        int orbitDamage = Mathf.Max(1, Mathf.RoundToInt(plan.damage * damageMultiplier));

        float angle = Mathf.Atan2(tangentDir.y, tangentDir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle - 90f);

        GameObject projObj = Instantiate(playerController.projectilePrefab, spawnPos, rot);
        projObj.transform.localScale *= plan.sizeMultiplier;

        if (projObj.TryGetComponent<Projectile>(out var proj))
        {
            ProjectileRuntimeState state = CreateRuntimeState(isOrbiting: true);
            proj.Init(playerController, tangentDir, plan.speed, plan.range, orbitDamage, state);
            return proj;
        }

        return null;
    }

    private void ClearAllOrbitals()
    {
        for (int i = 0; i < orbitSlots.Count; i++)
        {
            if (orbitSlots[i].instance != null)
                Destroy(orbitSlots[i].instance.gameObject);
        }
        orbitSlots.Clear();
    }

    #endregion
}