using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    private IStatOwner owner;
    private bool subscribed;

    public void Initialize(IStatOwner statOwner)
    {
        owner = statOwner;
        SetMaxHealth(owner.Stats.MaxHP);
        SetHealth(owner.Stats.CurrentHP);

        if (!subscribed)
        {
            StatManager.Instance.OnHPChanged += HandleHPChanged;
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (StatManager.Instance != null && subscribed)
        {
            StatManager.Instance.OnHPChanged -= HandleHPChanged;
            subscribed = false;
        }
    }

    private void HandleHPChanged(IStatOwner changedOwner, int oldHP, int newHP, int maxHP)
    {
        if (changedOwner != owner) return;
        SetMaxHealth(maxHP);
        SetHealth(newHP);
    }

    public void SetMaxHealth(int maxHealth) => slider.maxValue = maxHealth;
    public void SetHealth(int currentHealth) => slider.value = currentHealth;
}