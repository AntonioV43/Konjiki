using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomController : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private List<Transform> enemySpawnPoints;
    [SerializeField] private List<GameObject> enemyPrefabs;

    [Header("Random Enemy Spawn (di atas Ground)")]
    [Tooltip("Aktif: musuh muncul di posisi acak pada tile Ground. Nonaktif: pakai Enemy Spawn Points.")]
    [SerializeField] private bool useRandomSpawn = true;
    [SerializeField] private int minEnemies = 2;
    [SerializeField] private int maxEnemies = 4;
    [Tooltip("Jarak minimum musuh dari titik spawn player.")]
    [SerializeField] private float minDistanceFromPlayer = 4f;
    [Tooltip("Jarak minimum antar musuh saat spawn.")]
    [SerializeField] private float minDistanceBetweenEnemies = 1.5f;

    [Header("Pathfinding Tilemaps (kosong = dicari otomatis dari anak bernama Ground / Wall)")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Exits / Doors")]
    [SerializeField] private GameObject exitDoor;

    public Transform PlayerSpawnPoint => playerSpawnPoint;

    /// <summary>True hanya setelah musuh habis DAN buff sudah dipilih.</summary>
    public bool DoorUnlocked { get; private set; }

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isRoomCleared = false;
    private bool enemiesSpawned = false;

    private void Awake()
    {
        // Pintu keluar mati selama musuh masih ada
        if (exitDoor != null)
            exitDoor.SetActive(false);
        else
            Debug.LogWarning($"{name}: Exit Door belum di-assign di RoomController!");
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

        if (enemyPrefabs.Count == 0) return;

        if (useRandomSpawn && GridPathfinder.IsReady)
            SpawnEnemiesRandom();
        else
            SpawnEnemiesAtPoints();
    }

    private void SpawnEnemiesAtPoints()
    {
        foreach (Transform spawnPoint in enemySpawnPoints)
            SpawnEnemyAt(spawnPoint.position);
    }

    private void SpawnEnemiesRandom()
    {
        int count = Random.Range(minEnemies, Mathf.Max(minEnemies, maxEnemies) + 1);
        Vector2 playerPos = playerSpawnPoint != null ? (Vector2)playerSpawnPoint.position : Vector2.zero;
        List<Vector2> used = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            if (!TryFindSpawnPosition(playerPos, used, out Vector2 pos))
            {
                Debug.LogWarning($"{name}: tidak menemukan posisi spawn untuk musuh ke-{i + 1} (ground terlalu kecil?).");
                break;
            }

            used.Add(pos);
            SpawnEnemyAt(pos);
        }
    }

    private bool TryFindSpawnPosition(Vector2 playerPos, List<Vector2> used, out Vector2 result)
    {
        float playerSqr = minDistanceFromPlayer * minDistanceFromPlayer;
        float enemySqr = minDistanceBetweenEnemies * minDistanceBetweenEnemies;

        // Percobaan 1: patuhi jarak dari player DAN antar musuh. Percobaan 2: hanya jarak dari player.
        for (int pass = 0; pass < 2; pass++)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                if (!GridPathfinder.TryGetRandomWalkablePosition(out result)) return false;
                if ((result - playerPos).sqrMagnitude < playerSqr) continue;

                bool tooClose = false;
                if (pass == 0)
                {
                    foreach (Vector2 other in used)
                    {
                        if ((result - other).sqrMagnitude < enemySqr) { tooClose = true; break; }
                    }
                }

                if (!tooClose) return true;
            }
        }

        result = Vector2.zero;
        return false;
    }

    private void SpawnEnemyAt(Vector3 position)
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Vector3 pos = new Vector3(position.x, position.y, transform.position.z);
        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity, transform);
        activeEnemies.Add(enemyObj);
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

        DoorUnlocked = true;
        if (exitDoor != null) exitDoor.SetActive(true);
    }
}