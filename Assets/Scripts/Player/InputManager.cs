using UnityEngine.InputSystem;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // For ease of checking pressed, held, or released
    [System.Serializable]
    public class Input
    {
        public bool pressed, held, released;
        private bool pressedLast;

        public Input(InputAction input)
        {
            input.performed += c => pressed = true;
            input.canceled += c => pressed = false;
        }

        public void Update()
        {
            held = pressed && !pressedLast;
            released = !pressed && pressedLast;
            pressedLast = pressed;
        }
    }

    public Vector2 playerMovement { get; private set; } = Vector2.zero;
    private Vector2 playerInputs;

    internal bool inputPressed, inputReleased;
    private bool inputLastPressed;

    private InputHandler inputs;

    private Input[] buttons;
    internal Input jump, climb;

    private void OnEnable() => inputs.Movement.Enable();
    private void onDisable() => inputs.Movement.Disable();

    private void Awake()
    {
        inputs = new();

        var i = inputs.Movement;

        i.WASD.performed += ctx => playerInputs = ctx.ReadValue<Vector2>();
        i.WASD.canceled += ctx => playerInputs = Vector2.zero;

        buttons = new[] { jump = new(i.Jump), climb = new(i.Climb)};
    }

    private void Update()
    {
        playerMovement = playerInputs;

        bool inputP = playerInputs != Vector2.zero;

        inputPressed = inputP && !inputLastPressed;
        inputReleased = !inputP && inputLastPressed;
        inputLastPressed = inputP;

        foreach (var i in buttons) i.Update();
    }
}
