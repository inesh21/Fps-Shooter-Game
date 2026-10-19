using UnityEngine;

public sealed class WalkState : BaseState
{
    public WalkState(PlayerFSM player) : base(player) { }

    public override void UpdateState()
    {
        Vector2 input = player.ReadMoveInput();
        if (input.sqrMagnitude <= 0.01f)
        {
            player.ChangeState(new IdleState(player));
            return;
        }
        if (player.SprintHeld)
        {
            player.ChangeState(new SprintState(player));
            return;
        }
        player.MoveHorizontally(input, player.walkSpeed);
    }
}
