using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float attackCooldown = 1f;
    private Rigidbody2D rb;
    private InputControll controll;

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
        if (controll.canAttack)
        {
            Debug.Log("Attacking!");
            controll.canAttack = false;      
            StartCoroutine(AttackCooldown());
        }
    }
    private System.Collections.IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        controll.canAttack = true;
    }

}