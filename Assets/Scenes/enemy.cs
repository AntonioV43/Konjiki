using UnityEngine;

public class enemy : MonoBehaviour
{
    public Rigidbody2D rb;
    public CapsuleCollider2D capsule;

    [Header("Debug Damage Settings")]
    public int damageAmount = 10;
    public float damageInterval = 1f; // seconds between hits while touching

    private float lastDamageTime = -999f;

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void TryDamage(GameObject other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (Time.time - lastDamageTime < damageInterval)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            lastDamageTime = Time.time;
        }
    }
}