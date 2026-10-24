using UnityEngine;

public enum BuffType 
{ 
    StatBuff, 
    SpecialBuff, 
    DrawBackBuff 
}

public enum StatType 
{ 
    MaxHP, 
    Heal, 
    AttackDamage, 
    Defense, 
    MoveSpeed 
}

[CreateAssetMenu(fileName = "NewBuffData", menuName = "Buff System/Buff Data")]
public class BuffData : ScriptableObject
{
    public string buffName;
    [TextArea] public string description;
    public Sprite icon;
    public BuffType buffType;

    [Header("Stat Settings")]
    public StatType statType;
    public int statValue;       // Nilai statistik bulat (misal: +20 HP, +5 ATK)
    public float floatValue;    // Nilai desimal (misal: +1.5 MoveSpeed)
}