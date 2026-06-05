using UnityEngine;
using UnityEngine.InputSystem;

public class InputControll : MonoBehaviour
{
    [Header("Input Values")]
    public Vector2 move;
    public bool attackPressed;
    public bool jumpPressed;
    public bool jumpHeld;

    private PlayerInput playerInput;
    private InputAction jumpAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        // Grab the Jump action directly so we can poll it every frame
        jumpAction = playerInput.actions["Jump"];
    }

    private void Update()
    {
        // Poll held state every frame — 100% reliable, no missed releases
        jumpHeld = jumpAction.IsPressed();

        // One-frame trigger
        jumpPressed = jumpAction.WasPressedThisFrame();
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