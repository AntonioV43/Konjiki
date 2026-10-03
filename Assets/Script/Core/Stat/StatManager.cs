using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager that mediates ALL HP/stat changes for both player and enemies.
/// Nothing should modify CharacterStats.CurrentHP directly outside of this manager -
/// always go through Damage()/Heal()/SetHP() so events fire consistently.
/// </summary>
public class StatManager : Singleton<StatManager>
{
    public static StatManager Instance { get; private set; }

    // Fired whenever ANY tracked character's HP changes.
    // Params: owner, oldHP, newHP, maxHP
    public event Action<IStatOwner, int, int, int> OnHPChanged;

    // Fired once when a character's HP hits 0.
    public event Action<IStatOwner> OnDeath;

    // Optional: fired when HP increases (heal) vs decreases (damage), if you want separate hooks.
    public event Action<IStatOwner, int> OnDamaged; // amount actually removed
    public event Action<IStatOwner, int> OnHealed;  // amount actually restored

    private readonly HashSet<IStatOwner> registered = new HashSet<IStatOwner>();

    override protected void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------------- Registration ----------------

    /// <summary>Call from a character's OnEnable/Start so the manager knows about it.</summary>
    public void Register(IStatOwner owner)
    {
        if (owner == null) return;
        registered.Add(owner);
    }

    /// <summary>Call from a character's OnDisable/OnDestroy.</summary>
    public void Unregister(IStatOwner owner)
    {
        registered.Remove(owner);
    }

    // ---------------- HP Fluctuation ----------------

    /// <summary>Reduce HP by a positive amount (applies no defense math - do that before calling, or extend here).</summary>
    public void Damage(IStatOwner owner, int amount)
    {
        if (owner == null || owner.Stats == null || amount <= 0) return;
        if (owner.Stats.IsDead) return;

        int oldHP = owner.Stats.CurrentHP;
        owner.Stats.CurrentHP = oldHP - amount;
        int newHP = owner.Stats.CurrentHP;

        int actualLost = oldHP - newHP;
        if (actualLost > 0)
            OnDamaged?.Invoke(owner, actualLost);

        RaiseHPChanged(owner, oldHP, newHP);

        if (newHP <= 0 && oldHP > 0)
            OnDeath?.Invoke(owner);
    }

    /// <summary>Increase HP by a positive amount, clamped to MaxHP.</summary>
    public void Heal(IStatOwner owner, int amount)
    {
        if (owner == null || owner.Stats == null || amount <= 0) return;
        if (owner.Stats.IsDead) return; // remove this check if you allow revives via Heal

        int oldHP = owner.Stats.CurrentHP;
        owner.Stats.CurrentHP = oldHP + amount;
        int newHP = owner.Stats.CurrentHP;

        int actualHealed = newHP - oldHP;
        if (actualHealed > 0)
            OnHealed?.Invoke(owner, actualHealed);

        RaiseHPChanged(owner, oldHP, newHP);
    }

    /// <summary>Directly set HP to an exact value (e.g. scripted events, full heal, boss phase reset).</summary>
    public void SetHP(IStatOwner owner, int value)
    {
        if (owner == null || owner.Stats == null) return;

        int oldHP = owner.Stats.CurrentHP;
        owner.Stats.CurrentHP = value;
        int newHP = owner.Stats.CurrentHP;

        RaiseHPChanged(owner, oldHP, newHP);

        if (newHP <= 0 && oldHP > 0)
            OnDeath?.Invoke(owner);
    }

    /// <summary>Convenience: apply damage with basic defense mitigation.</summary>
    public void DealDamageWithDefense(IStatOwner attacker, IStatOwner target, int baseAttack)
    {
        if (target == null || target.Stats == null) return;

        int mitigated = Mathf.Max(1, baseAttack - target.Stats.Defense); // always at least 1 dmg
        Damage(target, mitigated);
    }

    private void RaiseHPChanged(IStatOwner owner, int oldHP, int newHP)
    {
        if (oldHP != newHP)
            OnHPChanged?.Invoke(owner, oldHP, newHP, owner.Stats.MaxHP);
    }
}