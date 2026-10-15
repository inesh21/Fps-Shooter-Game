using UnityEngine;

public class WalkState : BaseState
{
    public WalkState(PlayerFSM player) : base(player) { }

    public override void EnterState() { }

    public override void UpdateState()
    {
        // 1. Read input vector (WASD)
        Vector2 input = player.Actions.Player.Move.ReadValue<Vector2>();

        // Check for sprint transition first
        if (player.Actions.Player.Sprint.IsPressed() && input.sqrMagnitude > 0.01f)
        {
            player.SwitchState(new SprintState(player));
            return;
        }

        // State transitions (e.g., transition to Idle when there is no input)
        if (input.sqrMagnitude < 0.01f)
        {
            player.SwitchState(new IdleState(player));
            return;
        }

        // 2. Get forward and right directions from the main camera
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // 3. Zero out Y to keep movement strictly on the horizontal plane
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 4. Calculate movement relative to camera direction
        Vector3 moveDirection = (cameraForward * input.y) + (cameraRight * input.x);

        // 5. Move the CharacterController
        player.Controller.Move(moveDirection * player.walkSpeed * Time.deltaTime);

        // 6. Smoothly rotate character to face the direction of movement
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    public override void ExitState() { }
}