using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputControll : MonoBehaviour
{
    [Header("Input Values")]
    public Vector2 move;
    public bool attackHeld;
    public bool attackBuffered;
    public bool dashPressed;

    private PlayerInput playerInput;
    private InputAction attackAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        if (playerInput != null && playerInput.actions != null)
            attackAction = playerInput.actions.FindAction("Attack");
    }

    private void Update()
    {
        if (attackAction == null && playerInput != null && playerInput.actions != null)
            attackAction = playerInput.actions.FindAction("Attack");

        // Selalu sinkron dengan status tombol asli setiap frame (otomatis false saat dilepas)
        attackHeld = attackAction != null && attackAction.IsPressed();
    }

    private void OnDisable()
    {
        attackHeld = false;
        attackBuffered = false;
        dashPressed = false;
    }

    private void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    private void OnAttack(InputValue value)
    {
        if (value.isPressed)
            attackBuffered = true;
    }

    private void OnDash(InputValue value)
    {
        if (value.isPressed)
            dashPressed = true;
    }
}