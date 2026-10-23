using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : Singleton<DungeonManager>
{
    [Header("Room Prefabs Pool")]
    [SerializeField] private List<GameObject> roomPrefabs;
    [SerializeField] private GameObject startRoomPrefab;
    private GameObject lastRoomPrefab;

    [Header("Player Reference")]
    [SerializeField] private GameObject playerPrefab;

    private GameObject currentRoomInstance;
    private GameObject playerInstance;

    private void Start()
    {
        SetupPlayer();

        if (startRoomPrefab != null)
            SpawnRoom(startRoomPrefab);
        else
            SpawnNextRoom();
    }

    private void SetupPlayer()
    {
        playerInstance = GameObject.FindGameObjectWithTag("Player");
        if (playerInstance == null && playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        }
    }

    public void SpawnNextRoom()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            Debug.LogError("List roomPrefabs di DungeonManager masih kosong!");
            return;
        }

        GameObject chosen = roomPrefabs[Random.Range(0, roomPrefabs.Count)];

        if (roomPrefabs.Count > 1)
        {
            int safety = 0;
            while (chosen == lastRoomPrefab && safety++ < 20)
                chosen = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
        }

        SpawnRoom(chosen);
    }

    public void SpawnRoom(GameObject roomPrefab)
    {
        if (roomPrefab == null) return;

        // 1. Hancurkan ruangan lama (dimatikan dulu supaya tidak ikut aktif di frame ini)
        if (currentRoomInstance != null)
        {
            currentRoomInstance.SetActive(false);
            Destroy(currentRoomInstance);
        }

        lastRoomPrefab = roomPrefab;
        Debug.Log($"Spawn room: {roomPrefab.name}");

        // 2. Instantiate ruangan baru
        currentRoomInstance = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity);

        RoomController roomController = currentRoomInstance.GetComponent<RoomController>();
        if (roomController == null)
        {
            Debug.LogError($"{roomPrefab.name} tidak punya RoomController!");
            return;
        }

        // 3. Pindahkan player ke spawn point
        if (playerInstance != null && roomController.PlayerSpawnPoint != null)
        {
            playerInstance.transform.position = roomController.PlayerSpawnPoint.position;
        }

        // 4. Bangun grid A* dari tilemap room (instan, tidak perlu bake / tunggu frame)
        roomController.BuildPathGrid();

        // 5. Spawn musuh setelah grid siap
        roomController.SpawnEnemies();
    }
}