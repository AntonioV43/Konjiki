using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomController : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private List<Transform> enemySpawnPoints;
    [SerializeField] private List<GameObject> enemyPrefabs;

    [Header("Pathfinding Tilemaps (kosong = dicari otomatis dari anak bernama Ground / Wall)")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Exits / Doors")]
    [SerializeField] private GameObject exitDoor;

    public Transform PlayerSpawnPoint => playerSpawnPoint;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isRoomCleared = false;
    private bool enemiesSpawned = false;

    private void Awake()
    {
        // Pintu keluar mati selama musuh masih ada
        if (exitDoor != null) exitDoor.SetActive(false);
    }

    private void OnDestroy()
    {
        UI_BuffCards.OnBuffSelected -= HandleBuffSelected;
    }

    /// <summary>Bangun grid A* dari tilemap room ini. Panggil sebelum SpawnEnemies().</summary>
    public void BuildPathGrid()
    {
        if (groundTilemap == null || wallTilemap == null)
        {
            foreach (Tilemap tm in GetComponentsInChildren<Tilemap>())
            {
                if (groundTilemap == null && tm.name == "Ground") groundTilemap = tm;
                else if (wallTilemap == null && tm.name == "Wall") wallTilemap = tm;
            }
        }

        if (groundTilemap == null)
        {
            Debug.LogError($"{name}: Tilemap 'Ground' tidak ditemukan, pathfinding tidak bisa dibuat.");
            return;
        }

        GridPathfinder.Build(groundTilemap, wallTilemap);
    }

    public void SpawnEnemies()
    {
        enemiesSpawned = true;

        if (enemySpawnPoints.Count == 0 || enemyPrefabs.Count == 0) return;

        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            GameObject enemyObj = Instantiate(randomEnemy, spawnPoint.position, Quaternion.identity, transform);
            activeEnemies.Add(enemyObj);
        }
    }

    private void Update()
    {
        if (isRoomCleared || !enemiesSpawned) return;

        activeEnemies.RemoveAll(enemy => enemy == null);

        if (activeEnemies.Count == 0)
        {
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        isRoomCleared = true;

        // Pintu baru muncul setelah player memilih buff
        UI_BuffCards.OnBuffSelected += HandleBuffSelected;

        if (UIManager.Instance != null)
            UIManager.Instance.Open(UIType.BuffCards);
        else
            HandleBuffSelected(); // fallback kalau UIManager tidak ada
    }

    private void HandleBuffSelected()
    {
        UI_BuffCards.OnBuffSelected -= HandleBuffSelected;

        if (exitDoor != null) exitDoor.SetActive(true);
    }
}