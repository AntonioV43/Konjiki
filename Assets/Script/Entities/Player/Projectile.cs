using UnityEngine;

public class Projectile : MonoBehaviour
{
    public LayerMask hitMask; // Set ke layer "Enemy" di Inspector

    private IStatOwner attacker;
    private Vector2 direction;
    private float speed;
    private int damage;
    private bool hasHit;
    private Rigidbody2D rb;

    private void Awake()
    {
        if (TryGetComponent(out rb))
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
        }
    }

    public void Init(IStatOwner owner, Vector2 dir, float moveSpeed, float lifeTime, int dmg)
    {
        attacker = owner;
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        speed = Mathf.Max(0.1f, moveSpeed);
        damage = dmg;
        hasHit = false;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        if (rb != null)
            rb.linearVelocity = direction * speed;

        Destroy(gameObject, Mathf.Max(0.05f, lifeTime));
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        if (other.TryGetComponent<EnemyController>(out var enemy))
        {
            hasHit = true;

            if (StatManager.Instance != null)
                StatManager.Instance.DealDamageWithDefense(attacker, enemy, damage);

            Destroy(gameObject);
        }
    }
}