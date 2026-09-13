using UnityEngine;

// Example enemy using the StatManager
public class EnemyController : MonoBehaviour, IStatOwner
{
    [SerializeField] private CharacterStats stats = new CharacterStats { Name = "Goblin", MaxHP = 40, Attack = 8, Defense = 2 };
    [SerializeField] private HealthBar healthBar; // e.g. a world-space bar above the enemy's head
    public CharacterStats Stats => stats;

    private void OnEnable()
    {
        stats.ResetToFull();
        StatManager.Instance.Register(this);

        if (healthBar != null)
            healthBar.Initialize(this);
    }

    private void OnDisable()
    {
        StatManager.Instance.Unregister(this);
    }

    // Example: player attacks this enemy
    public void TakeHitFrom(PlayerController player)
    {
        StatManager.Instance.DealDamageWithDefense(player, this, player.Stats.Attack);
    }
}