using UnityEngine;

/// <summary>
/// Pasang di objek pintu keluar (yang di-assign ke Exit Door di RoomController).
/// Saat player menyentuh pintu, pindah ke room berikutnya.
/// Butuh Collider2D dengan Is Trigger dicentang.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    private bool used;

    private void Reset()
    {
        // Otomatis jadikan trigger saat komponen pertama kali ditambahkan
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;

        used = true;

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.SpawnNextRoom();
    }
}