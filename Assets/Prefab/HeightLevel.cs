// HeightLevel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class HeightLevelEntry
{
    public string levelName = "New Level";
    public int levelIndex = 0;
    public TilemapRenderer tilemapRenderer;
    public TilemapCollider2D tilemapCollider;
    public bool isStairs = false;
}

public class HeightLevel : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("Levels")]
    [SerializeField] private List<HeightLevelEntry> levels = new List<HeightLevelEntry>();

    public GameObject Player => player;
    public int CurrentPlayerLevel => currentPlayerLevel;
    public List<HeightLevelEntry> Levels => levels;

    private int currentPlayerLevel = 0;
    private bool isOnStairs = false;

    private void Start() => UpdateColliders();

    public void EnterStairs()
    {
        isOnStairs = true;
        UpdateColliders();
    }

    public void ExitStairs()
    {
        isOnStairs = false;
        UpdateColliders();
        Debug.Log($"[HeightLevel] Player settled on level {currentPlayerLevel}");
    }

    public void SetPlayerLevel(int newLevel)
    {
        currentPlayerLevel = newLevel;
        Debug.Log($"[HeightLevel] Player level updated to {currentPlayerLevel}");
    }

    private void UpdateColliders()
    {
        foreach (var entry in levels)
        {
            if (entry.tilemapCollider == null) continue;

            entry.tilemapCollider.enabled = true;

            if (entry.isStairs || isOnStairs)
                entry.tilemapCollider.isTrigger = true;   // everything open on stairs
            else
                entry.tilemapCollider.isTrigger = entry.levelIndex == currentPlayerLevel;
        }
    }
}