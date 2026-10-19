using UnityEngine;

public sealed class IdleState : BaseState
{
    public IdleState(PlayerFSM player) : base(player) { }

    public override void UpdateState()
    {
        Vector2 input = player.ReadMoveInput();
        if (input.sqrMagnitude <= 0.01f)
        {
            player.MoveHorizontally(Vector2.zero, 0f);
            return;
        }
        player.ChangeState(player.SprintHeld ? new SprintState(player) : new WalkState(player));
    }
}
