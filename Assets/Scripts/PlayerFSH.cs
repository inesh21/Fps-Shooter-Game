using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerFSM : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f;

    [Header("References")]
    public CharacterController Controller { get; set; }

    private BaseState currentState;
    public PlayerInput Actions { get; private set; }
    private Vector3 velocity;
    private bool isGrounded;

    private void Awake()
    {
        Actions = new PlayerInput();

        if (Controller == null) 
            Controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        // 2. Enable inputs and subscribe to events
        Actions.Player.Enable();
        Actions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        // 3. Unsubscribe to avoid memory leaks
        Actions.Player.Jump.performed -= OnJump;
        Actions.Player.Disable();
    }

    private void Start()
    {
        // Start in Idle state
        SwitchState(new IdleState(this));
    }

    private void Update()
    {
        // Ground Check
        if (Controller == null)
            Controller = GetComponent<CharacterController>();
        isGrounded = Controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Downward force to stay grounded
        }

        // 4. Run active state update logic (Handles WASD movement)
        currentState?.UpdateState();

        // Apply Gravity & Vertical Movement
        velocity.y += gravity * Time.deltaTime;
        Controller.Move(velocity * Time.deltaTime);
    }

    public void SwitchState(BaseState newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}