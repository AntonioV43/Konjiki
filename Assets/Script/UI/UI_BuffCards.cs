using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_BuffCards : MonoBehaviour, IUIController
{
    [System.Serializable]
    public struct CardSlot
    {
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public Image iconImage;
        public Button cardButton;
    }

    /// <summary>Dipanggil setelah player memilih buff dan UI sudah ditutup.</summary>
    public static event System.Action OnBuffSelected;

    [SerializeField] private CardSlot[] cardSlots;
    [SerializeField] private List<BuffData> allAvailableBuffs;

    public void Show()
    {
        gameObject.SetActive(true);
        GameManager.Instance.PauseGame(true);
        InputManager.Instance.EnableUIInput();

        GenerateRandomBuffs();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        GameManager.Instance.PauseGame(false);
        InputManager.Instance.EnableGameplayInput();
    }

    private void GenerateRandomBuffs()
    {
        List<BuffData> pool = new List<BuffData>(allAvailableBuffs);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = Random.Range(0, pool.Count);
            BuffData selectedBuff = pool[randomIndex];
            pool.RemoveAt(randomIndex);

            if (cardSlots[i].nameText) 
                cardSlots[i].nameText.text = selectedBuff.buffName;
                
            if (cardSlots[i].descriptionText) 
                cardSlots[i].descriptionText.text = selectedBuff.description;
                
            if (cardSlots[i].iconImage && selectedBuff.icon) 
                cardSlots[i].iconImage.sprite = selectedBuff.icon;

            cardSlots[i].cardButton.onClick.RemoveAllListeners();
            cardSlots[i].cardButton.onClick.AddListener(() => OnSelectBuff(selectedBuff));
        }
    }

    public void OnSelectBuff(BuffData selectedBuff)
    {
        ApplyBuffToPlayer(selectedBuff);
        UIManager.Instance.Close(UIType.BuffCards);

        OnBuffSelected?.Invoke();
    }

    private void ApplyBuffToPlayer(BuffData buff)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        switch (buff.buffType)
        {
            case BuffType.StatBuff:
                ApplyStatBuff(player, buff);
                break;

            case BuffType.SpecialBuff:
                // Logika untuk buff khusus (misal: projectile berlipat, dsb)
                break;

            case BuffType.DrawBackBuff:
                // Logika buff efek samping (misal: ATK naik tapi MaxHP berkurang)
                break;
        }
    }

    private void ApplyStatBuff(PlayerController player, BuffData buff)
    {
        switch (buff.statType)
        {
            case StatType.MaxHP:
                player.Stats.MaxHP += buff.statValue;
                // Opsional: isi HP saat ini sebanyak penambahan MaxHP
                StatManager.Instance.Heal(player, buff.statValue);
                break;

            case StatType.Heal:
                StatManager.Instance.Heal(player, buff.statValue);
                break;

            case StatType.AttackDamage:
                player.attackDamage += buff.statValue;
                break;

            case StatType.Defense:
                player.Stats.Defense += buff.statValue;
                break;

            case StatType.MoveSpeed:
                float speedToAdd = buff.floatValue > 0 ? buff.floatValue : buff.statValue;
                player.moveSpeed += speedToAdd;
                break;
        }
    }
}