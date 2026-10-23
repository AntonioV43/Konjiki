using UnityEngine;

public class EnemyController : MonoBehaviour, IStatOwner
{
    [Header("Stats")]
    [SerializeField] private CharacterStats stats =
        new CharacterStats
        {
            Name = "Goblin",
            MaxHP = 40,
            Attack = 8,
            Defense = 2
        };

    [Header("UI")]
    [SerializeField] private UI_HealthBar healthBar;
    public CharacterStats Stats => stats;

    private void OnEnable()
    {
        stats.ResetToFull();
        StartCoroutine(RegisterWhenReady());
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        yield return new WaitUntil(() => StatManager.Instance != null);

        StatManager.Instance.Register(this);

        StatManager.Instance.OnDeath += HandleDeath;

        if (healthBar != null)
            healthBar.Initialize(this);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (StatManager.Instance != null)
        {
            StatManager.Instance.OnDeath -= HandleDeath;
            StatManager.Instance.Unregister(this);
        }
    }

    // Called when player attacks this enemy
    public void TakeHitFrom(PlayerController player)
    {
        StatManager.Instance.DealDamageWithDefense(
            player,
            this,
            player.Stats.Attack
        );
    }

    private void HandleDeath(IStatOwner owner)
    {
        if (owner != (IStatOwner)this)
            return;

        Debug.Log($"{stats.Name} died.");

        // Buff cards sekarang dibuka oleh RoomController saat semua musuh habis
        Destroy(gameObject);
    }
}