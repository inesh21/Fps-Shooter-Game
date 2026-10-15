using UnityEngine;

public class SprintState : BaseState
{
    public SprintState(PlayerFSM player) : base(player) { }

    public override void EnterState() { }

    public override void UpdateState()
    {
        Vector2 input = player.Actions.Player.Move.ReadValue<Vector2>();

        if (!player.Actions.Player.Sprint.IsPressed() || input.sqrMagnitude < 0.01f)
        {
            player.SwitchState(new WalkState(player));
            return;
        }

        // Use camera-relative movement like WalkState
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y) + (cameraRight * input.x);
        player.Controller.Move(moveDirection * player.sprintSpeed * Time.deltaTime);

        // Rotate player to face camera forward direction (FPS style)
        player.transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
    }

    public override void ExitState() { }
}