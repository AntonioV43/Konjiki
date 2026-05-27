using UnityEngine;
using UnityEngine.InputSystem;

public class InputControll : MonoBehaviour
{
    [Header("Input Values")]
    public Vector2 move;
    public bool attackPressed;

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    private void OnAttack(InputValue value)
    {
        if (value.isPressed)
            attackPressed = true;
    }
}