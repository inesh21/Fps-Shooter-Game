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

        Vector3 move = player.transform.right * input.x + player.transform.forward * input.y;
        player.Controller.Move(move * player.sprintSpeed * Time.deltaTime);
    }

    public override void ExitState() { }
}