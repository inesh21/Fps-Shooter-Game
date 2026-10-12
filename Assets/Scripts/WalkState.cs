using UnityEngine;

public class WalkState : BaseState
{
    public WalkState(PlayerFSM player) : base(player) { }

    public override void EnterState() { }

    public override void UpdateState()
    {
        Vector2 input = player.Actions.Player.Move.ReadValue<Vector2>();

        if (input.sqrMagnitude < 0.01f)
        {
            player.SwitchState(new IdleState(player));
            return;
        }

        if (player.Actions.Player.Sprint.IsPressed())
        {
            player.SwitchState(new SprintState(player));
            return;
        }

        Vector3 move = player.transform.right * input.x + player.transform.forward * input.y;
        player.Controller.Move(move * player.walkSpeed * Time.deltaTime);
    }

    public override void ExitState() { }
}