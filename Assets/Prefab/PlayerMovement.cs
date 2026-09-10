using UnityEngine;

[RequireComponent(typeof(InputControll))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public bool normalizeDiagonal = true;

    [Header("Rotation")]
    public bool rotateTowardsMovement = true;
    public float rotationSpeed = 720f; // degrees per second

    private InputControll input;
    private Rigidbody2D rb; // remove if using CharacterController or Transform-only movement

    private Vector2 moveInput;
    
    private void Awake()
    {
        input = GetComponent<InputControll>();
        rb = GetComponent<Rigidbody2D>(); // optional, see note below
    }

    private void Update()
    {
        // Read input each frame
        moveInput = input.move;

        if (rotateTowardsMovement && moveInput.sqrMagnitude > 0.01f)
        {
            RotateTowardsMovement();
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector2 direction = moveInput;

        if (normalizeDiagonal && direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector2 velocity = direction * moveSpeed;

        if (rb != null)
        {
            // Physics-based movement (recommended for top-down with collisions)
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }
        else
        {
            // Fallback: direct transform movement
            transform.position += new Vector3(velocity.x, velocity.y, 0f) * Time.fixedDeltaTime;
        }
    }

    private void RotateTowardsMovement()
    {
        float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle - 90f); // -90 if sprite faces up by default

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}