using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(PlayerFSM player) : base(player) { }

    public override void EnterState() { }

    public override void UpdateState()
    {
        Vector2 input = player.Actions.Player.Move.ReadValue<Vector2>();
        
        // Transition to Walk if moving
        if (input.sqrMagnitude > 0.01f)
        {
            player.SwitchState(new WalkState(player));
        }
    }

    public override void ExitState() { }
}