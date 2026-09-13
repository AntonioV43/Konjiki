using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 2f;
    public int damage = 10;
    public LayerMask hitMask; // set to "Enemy" layer in Inspector

    private Vector2 direction;

    public void Init(Vector2 dir, int dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore anything not on the enemy layer
        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            StatManager.Instance.Damage(enemy, damage);
            Destroy(gameObject);
        }
    }
}