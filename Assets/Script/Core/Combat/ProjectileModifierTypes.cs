using System.Collections.Generic;
using UnityEngine;

public enum ModifierCategory
{
    Quantity,
    Trajectory,
    Behaviour,
    Elemental,
    OnHit,
    Utils
}

public struct ProjectileSpawnPlan
{
    public int burstCount;      // Gelombang tembakan beruntun (default 1)
    public int radialCount;     // Sebaran melingkar 360 derajat (default 1)
    public int spreadCount;     // Peluru dalam satu cone sudut (default 1)
    public float spreadAngle;   // Total sudut cone dalam derajat
    public float burstInterval; // Jeda antar tembakan pada mode Burst
    public float speed;
    public float range;
    public float sizeMultiplier;
    public int damage;
}

public class ProjectileRuntimeState
{
    public int remainingPierce = 0;
    public int remainingBounce = 0;
    public bool bounceOnEnemy = false;
    public float homingStrength = 0f;
    public float waveFrequency = 0f;
    public float waveAmplitude = 0f;
    public bool isOrbiting = false;
    public List<ProjectileModifierSO> activeModifiers = new List<ProjectileModifierSO>();
}