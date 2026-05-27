using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Height Level")]
    [SerializeField] private HeightLevel heightLevel;

    private Rigidbody2D rb;
    private InputControll controll;
    private bool canAttack = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controll = GetComponent<InputControll>();
    }

    private void Update()
    {
        Movement();
        Attack();
    }

    private void Movement()
    {
        Vector2 movement = controll.move * speed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void Attack()
    {
        if (controll.attackPressed)
        {
            controll.attackPressed = false;

            if (canAttack)
            {
                Debug.Log("Attacking!");
                canAttack = false;
                StartCoroutine(AttackCooldown());
            }
        }
    }

    private System.Collections.IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (heightLevel == null) return;

        var entry = heightLevel.Levels.Find(l => l.tilemapCollider == other);
        if (entry == null) return;

        if (entry.isStairs)
            heightLevel.EnterStairs();
        else
            heightLevel.SetPlayerLevel(entry.levelIndex); // walked onto a level tile
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (heightLevel == null) return;

        var entry = heightLevel.Levels.Find(l => l.isStairs && l.tilemapCollider == other);
        if (entry != null) heightLevel.ExitStairs();
    }
}