using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerFSM : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 12f;
    public float jumpHeight = 2f;
    public float gravity = -25f;
    public float acceleration = 10f;

    [Header("References")]
    public CharacterController Controller { get; set; }

    private BaseState currentState;
    public PlayerInput Actions { get; private set; }
    private Vector3 velocity;
    private bool isGrounded;
    public Vector3 CurrentVelocity => velocity;

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
        // Temporarily disable controller to override position cleanly
        if (Controller != null)
        {
            Controller.enabled = false;
        }

        // Set your desired spawn position (e.g., ground level)
        transform.position = new Vector3(0f, 1f, 0f);
        transform.rotation = Quaternion.identity;

        // Re-enable controller after position is set
        if (Controller != null)
        {
            Controller.enabled = true;
        }

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
            velocity.y = -2f; // Slight downward force to keep grounded
        }

        // Run active state update logic (Handles WASD movement)
        currentState?.UpdateState();

        // Apply Gravity & Vertical Movement
        velocity.y += gravity * Time.deltaTime;
        Controller.Move(velocity * Time.deltaTime);

        // Player rotation is now handled by CameraFollow script
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