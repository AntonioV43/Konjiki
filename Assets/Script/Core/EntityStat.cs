using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterStats
{
    public string Name = "Unnamed";

    [Header("Health")]
    public int MaxHP = 100;
    [SerializeField] private int currentHP = 100;

    [Header("Other Stats (extend as needed)")]
    public int Attack = 10;
    public int Defense = 5;

    public int CurrentHP
    {
        get => currentHP;
        set => currentHP = Mathf.Clamp(value, 0, MaxHP);
    }

    public bool IsDead => CurrentHP <= 0;

    public void ResetToFull() => CurrentHP = MaxHP;
}