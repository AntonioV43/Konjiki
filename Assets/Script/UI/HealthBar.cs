using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    // The character this bar is tracking (set via Initialize, e.g. from PlayerController/EnemyController).
    private IStatOwner owner;

    /// <summary>Call this once when the owning character spawns / becomes active.</summary>
    public void Initialize(IStatOwner statOwner)
    {
        owner = statOwner;

        SetMaxHealth(owner.Stats.MaxHP);
        SetHealth(owner.Stats.CurrentHP);
    }

    private void OnEnable()
    {
        StatManager.Instance.OnHPChanged += HandleHPChanged;
    }

    private void OnDisable()
    {
        if (StatManager.Instance != null)
            StatManager.Instance.OnHPChanged -= HandleHPChanged;
    }

    private void HandleHPChanged(IStatOwner changedOwner, int oldHP, int newHP, int maxHP)
    {
        // Ignore updates that aren't for the character this bar belongs to.
        if (changedOwner != owner) return;

        SetMaxHealth(maxHP);
        SetHealth(newHP);
    }

    public void SetMaxHealth(int maxHealth)
    {
        slider.maxValue = maxHealth;
    }

    public void SetHealth(int currentHealth)
    {
        slider.value = currentHealth;
    }
}