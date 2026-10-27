using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerFSM : NetworkBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float walkSpeed = 6f;
    [Min(0f)] public float sprintSpeed = 12f;
    [Min(0f)] public float jumpHeight = 2f;
    [Min(0f)] public float gravity = 25f;
    [Min(0f)] public float acceleration = 10f;
    [SerializeField, Min(0f)] private float jumpBuffer = 0.15f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;

    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private WeaponManager weaponManager;

    private BaseState state;
    private Vector3 verticalVelocity;
    private Vector3 horizontalVelocity;
    private bool grounded;
    private bool jumpQueued;
    private float jumpQueuedAt;
    private float lastGroundedAt = float.NegativeInfinity;

    public PlayerInput Actions { get; private set; }
    public CharacterController Controller => controller;
    public Animator Animator => animator;
    public WeaponManager WeaponManager => weaponManager;
    public bool IsGrounded => grounded;
    public float VerticalVelocity => verticalVelocity.y;
    public Vector3 CurrentVelocity => horizontalVelocity + verticalVelocity;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // If spawned over network, only the owner should control camera, audio, and inputs
        ConfigureOwnerAuthority(IsOwner);
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerFSM requires a CharacterController on the same GameObject.", this);
            enabled = false;
            return;
        }

        // The player root is the feet pivot. This prevents half of a default
        // centered capsule from extending below a ground level root.
        if (Mathf.Approximately(controller.center.y, 0f))
            controller.center = new Vector3(controller.center.x, controller.height * 0.5f, controller.center.z);

        Actions = new PlayerInput();
    }

    private void OnEnable()
    {
        if (Actions == null) return;
        Actions.Player.Jump.performed += OnJumpAction;
        Actions.Player.Enable();
    }

    private void OnDisable()
    {
        if (Actions == null) return;
        Actions.Player.Jump.performed -= OnJumpAction;
        Actions.Player.Disable();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Actions?.Dispose();
    }

    private void Start()
    {
        if (controller == null) return;

        // Auto-configure singleplayer / offline fallback if not networked or spawned yet
        if (!IsSpawned)
        {
            ConfigureOwnerAuthority(true);
        }

        ChangeState(new IdleState(this));
    }

    private void ConfigureOwnerAuthority(bool isLocalOwner)
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (audioListener == null && playerCamera != null)
            audioListener = playerCamera.GetComponent<AudioListener>();

        if (playerCamera != null) playerCamera.enabled = isLocalOwner;
        if (audioListener != null) audioListener.enabled = isLocalOwner;

        var cameraFollow = GetComponentInChildren<CameraFollow>(true);
        if (cameraFollow != null) cameraFollow.enabled = isLocalOwner;

        var weaponSway = GetComponentInChildren<WeaponSwayAndBob>(true);
        if (weaponSway != null) weaponSway.enabled = isLocalOwner;

        var weaponAntiClip = GetComponentInChildren<WeaponAntiClip>(true);
        if (weaponAntiClip != null) weaponAntiClip.enabled = isLocalOwner;

        if (!isLocalOwner)
        {
            if (Actions != null) Actions.Player.Disable();
            if (controller != null) controller.enabled = false;
        }
    }

    private void Update()
    {
        // Allow movement in single-player (IsSpawned == false) or when we are the network owner
        if (IsSpawned && !IsOwner) return;
        if (controller == null || !controller.enabled) return;

        // Supports the generated action map and Unity PlayerInput's Send
        // Messages mode. Duplicate notifications simply refresh one buffer.
        if (Actions != null && Actions.Player.Jump.WasPressedThisFrame()) QueueJump();
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) QueueJump();

        grounded = controller.isGrounded || (verticalVelocity.y <= 0f && GroundProbe());
        if (grounded)
        {
            lastGroundedAt = Time.time;
            if (verticalVelocity.y < 0f) verticalVelocity.y = -2f;
        }

        bool buffered = jumpQueued && Time.time - jumpQueuedAt <= jumpBuffer;
        bool mayJump = grounded || Time.time - lastGroundedAt <= coyoteTime;
        if (buffered && mayJump)
        {
            ChangeState(new JumpState(this));
            jumpQueued = false;
            lastGroundedAt = float.NegativeInfinity;
        }
        else if (jumpQueued && !buffered)
        {
            jumpQueued = false;
        }

        state?.UpdateState();
        verticalVelocity.y -= Mathf.Abs(gravity) * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    public void ChangeState(BaseState next)
    {
        if (next == null) return;
        state?.ExitState();
        state = next;
        state.EnterState();
    }

    public void StartJump()
    {
        verticalVelocity.y = Mathf.Sqrt(2f * Mathf.Max(0f, jumpHeight) * Mathf.Abs(gravity));
    }

    public Vector2 ReadMoveInput() => Actions != null ? Actions.Player.Move.ReadValue<Vector2>() : Vector2.zero;
    public bool SprintHeld => Actions != null && Actions.Player.Sprint.IsPressed();

    public void MoveHorizontally(Vector2 input, float speed)
    {
        Transform view = (playerCamera != null && playerCamera.enabled) ? playerCamera.transform : (Camera.main != null ? Camera.main.transform : transform);
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
        Vector3 direction = forward * input.y + right * input.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        Vector3 desired = direction * Mathf.Max(0f, speed);
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired,
            Mathf.Max(0f, acceleration) * Time.deltaTime);
        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    // Unity PlayerInput with Behavior=Send Messages invokes OnJump(InputValue).
    public void OnJump(InputValue value)
    {
        if (value.isPressed) QueueJump();
    }

    private void OnJumpAction(InputAction.CallbackContext context) => QueueJump();

    private void QueueJump()
    {
        jumpQueued = true;
        jumpQueuedAt = Time.time;
    }

    private bool GroundProbe()
    {
        Vector3 feet = transform.TransformPoint(controller.center - Vector3.up * (controller.height * 0.5f));
        RaycastHit[] hits = Physics.RaycastAll(feet + Vector3.up * 0.12f, Vector3.down, 0.25f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == controller || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }
    
}
