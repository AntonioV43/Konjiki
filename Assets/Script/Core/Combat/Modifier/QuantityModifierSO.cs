using UnityEngine;

public enum QuantityPattern
{
    SpreadMultishot, // Menembakkan N peluru sekaligus membentuk cone sudut
    Burst,           // Menembakkan N peluru secara beruntun dalam jeda singkat
    Radial           // Menembakkan N peluru ke segala arah (360 derajat) secara merata
}

[CreateAssetMenu(fileName = "NewQuantityModifier", menuName = "Roguelike/Projectile Modifiers/Quantity")]
public class QuantityModifierSO : ProjectileModifierSO
{
    [Header("Quantity Pattern")]
    public QuantityPattern pattern = QuantityPattern.SpreadMultishot;

    [Tooltip("Jumlah peluru tambahan (misal +2 pada Spread = 3 peluru cone, +7 pada Radial = 8 arah)")]
    [Min(1)] public int additionalProjectiles = 2;

    [Header("Spread / Multishot Settings")]
    [Tooltip("Total sudut cone dalam derajat")]
    [Range(1f, 360f)] public float spreadAngle = 30f;
    [Tooltip("Tambahan sudut jika Spread diambil lebih dari sekali")]
    [Range(0f, 90f)] public float stackSpreadBonus = 10f;

    [Header("Burst Settings")]
    [Tooltip("Jeda waktu antar peluru dalam satu rangkaian Burst (detik)")]
    [Min(0.01f)] public float burstInterval = 0.08f;

    [Header("Damage Scaling")]
    [Tooltip("Pengali damage per peluru (1 = 100% damage dasar)")]
    [Range(0.1f, 2f)] public float damageMultiplier = 1f;

    private void Reset()
    {
        category = ModifierCategory.Quantity;
    }

    public override void ApplyQuantity(ref ProjectileSpawnPlan plan)
    {
        plan.damage = Mathf.Max(1, Mathf.RoundToInt(plan.damage * damageMultiplier));

        switch (pattern)
        {
            case QuantityPattern.SpreadMultishot:
                plan.spreadCount += additionalProjectiles;
                plan.spreadAngle = plan.spreadAngle > 0f
                    ? Mathf.Min(360f, plan.spreadAngle + stackSpreadBonus)
                    : spreadAngle;
                break;

            case QuantityPattern.Burst:
                plan.burstCount += additionalProjectiles;
                plan.burstInterval = plan.burstInterval > 0f
                    ? Mathf.Min(plan.burstInterval, burstInterval)
                    : burstInterval;
                break;

            case QuantityPattern.Radial:
                plan.radialCount += additionalProjectiles;
                break;
        }
    }
}