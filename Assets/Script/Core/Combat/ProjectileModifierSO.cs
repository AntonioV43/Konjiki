using UnityEngine;

public abstract class ProjectileModifierSO : ScriptableObject
{
    [Header("Modifier Info")]
    public string modifierName;
    public ModifierCategory category;
    [TextArea] public string description;
    public Sprite icon;

    // 1. Utils: ubah size, speed, lifetime/range
    public virtual void ApplyUtils(ref ProjectileSpawnPlan plan) { }

    // 2. Quantity: tambah jumlah peluru & jarak sejajar
    public virtual void ApplyQuantity(ref ProjectileSpawnPlan plan) { }

    // 3. Trajectory: atur arah & posisi spawn tiap indeks peluru
    public virtual void ApplyTrajectory(int index, int totalCount, ref Vector2 direction, ref Vector3 spawnOffset) { }

    // 4. Behaviour & Trajectory runtime: setup homing, piercing, bounce, spiral
    public virtual void OnProjectileInit(Projectile projectile, ProjectileRuntimeState state) { }

    // 5 & 6. Elemental & OnHit: efek saat peluru mengenai musuh
    public virtual void OnProjectileHit(Projectile projectile, IStatOwner attacker, EnemyController target, Vector2 hitPoint) { }
}