using UnityEngine;

public enum TrajectoryType
{
    Wave,     // Bergerak zigzag menggunakan gelombang Sinus
    Ricochet, // Memantul saat mengenai dinding (dan opsional musuh)
    Orbit     // Mengelilingi player terus-menerus dengan auto-resupply tanpa perlu attack
}

[CreateAssetMenu(fileName = "NewTrajectoryModifier", menuName = "Roguelike/Projectile Modifiers/Trajectory")]
public class TrajectoryModifierSO : ProjectileModifierSO
{
    [Header("Trajectory Type")]
    public TrajectoryType trajectoryType = TrajectoryType.Wave;

    [Header("Wave (Sin) Settings")]
    [Tooltip("Frekuensi osilasi zigzag (semakin tinggi semakin rapat)")]
    [Min(0f)] public float waveFrequency = 14f;
    [Tooltip("Lebar simpangan zigzag")]
    [Min(0f)] public float waveAmplitude = 6f;

    [Header("Ricochet / Bounce Settings")]
    [Tooltip("Jumlah maksimal pantulan sebelum peluru hancur")]
    [Min(1)] public int bounceCount = 2;
    [Tooltip("Aktifkan jika peluru juga memantul saat mengenai musuh")]
    public bool bounceOnEnemy = false;

    [Header("Orbit Settings")]
    [Tooltip("Jumlah peluru orbit yang aktif mengelilingi player")]
    [Min(1)] public int orbitCount = 2;
    [Tooltip("Jarak radius orbit dari pusat player")]
    [Min(0.5f)] public float orbitRadius = 1.8f;
    [Tooltip("Kecepatan putar orbit (derajat per detik)")]
    public float orbitAngularSpeed = 200f;
    [Tooltip("Pengali damage peluru orbit (misal 0.4 = 40% damage dasar)")]
    [Range(0.05f, 1f)] public float orbitDamageMultiplier = 0.4f;
    [Tooltip("Waktu resupply otomatis setelah peluru orbit hancur (detik)")]
    [Min(0.2f)] public float orbitResupplyCooldown = 2.5f;

    private void Reset()
    {
        category = ModifierCategory.Trajectory;
    }

    public override void OnProjectileInit(Projectile projectile, ProjectileRuntimeState state)
    {
        if (trajectoryType == TrajectoryType.Wave)
        {
            state.waveFrequency = Mathf.Max(state.waveFrequency, waveFrequency);
            state.waveAmplitude += waveAmplitude;
        }
        else if (trajectoryType == TrajectoryType.Ricochet)
        {
            state.remainingBounce += bounceCount;
            state.bounceOnEnemy |= bounceOnEnemy;
        }
    }
}