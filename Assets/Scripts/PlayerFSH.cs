using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerFSM : MonoBehaviour
{
    public CharacterController Controller { get; private set; }
    public PlayerInput Actions { get; private set; }

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float gravity = -9.81f;

    private BaseState currentState;

    private void Awake()
    {
        Controller = GetComponent<CharacterController>();
        Actions = new PlayerInput();
    }

    private void OnEnable() => Actions.Enable();
    private void OnDisable() => Actions.Disable();

    private void Start()
    {
        // Start in Idle
        SwitchState(new IdleState(this));
    }

    private void Update()
    {
        currentState?.UpdateState();
    }

    public void SwitchState(BaseState newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }
    
}