using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    [SerializeField] private PlayerInput playerInput;

    // Nama Action Map sesuaikan dengan yang ada di Input Action Asset Anda
    private const string GameplayMap = "Player";
    private const string UIMap = "UI";

    protected override void Awake()
    {
        base.Awake();
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
        }
    }

    public void EnableGameplayInput()
    {
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(GameplayMap);
    }

    public void EnableUIInput()
    {
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(UIMap);
    }
}