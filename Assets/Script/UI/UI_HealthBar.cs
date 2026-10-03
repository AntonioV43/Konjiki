using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectMask2D mask;
    [SerializeField] private TMP_Text barIndicator;

    private IStatOwner owner;
    private bool subscribed;

    private float maxMask;
    private float initialMask;
    private bool maskCached;

    private void CacheMask()
    {
        if (maskCached) return;

        // Force layout so rect.width is valid if Initialize runs before the first layout pass
        Canvas.ForceUpdateCanvases();

        maxMask = barRect.rect.width - mask.padding.x - mask.padding.z;
        initialMask = mask.padding.z;
        maskCached = true;
    }

    public void Initialize(IStatOwner statOwner)
    {
        // Clean up any previous owner subscription
        Unsubscribe();

        owner = statOwner;
        CacheMask();
        Refresh(owner.Stats.CurrentHP, owner.Stats.MaxHP);

        if (StatManager.Instance != null)
        {
            StatManager.Instance.OnHPChanged += HandleHPChanged;
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (subscribed && StatManager.Instance != null)
            StatManager.Instance.OnHPChanged -= HandleHPChanged;

        subscribed = false;
    }

    private void HandleHPChanged(IStatOwner changedOwner, int oldHP, int newHP, int maxHP)
    {
        if (changedOwner != owner) return;
        Refresh(newHP, maxHP);
    }

    private void Refresh(int currentHP, int maxHP)
    {
        SetValue(currentHP, maxHP);

        if (barIndicator != null)
            barIndicator.SetText($"{currentHP}/{maxHP}");
    }

    private void SetValue(int currentHP, int maxHP)
    {
        if (maxHP <= 0) return;

        float targetWidth = currentHP * maxMask / maxHP;
        float newMask = maxMask + initialMask - targetWidth;

        var padding = mask.padding;
        padding.z = newMask;
        mask.padding = padding;
    }
}