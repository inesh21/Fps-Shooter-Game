using UnityEngine;

public sealed class WalkState : BaseState
{
    public WalkState(PlayerFSM player) : base(player) { }

    public override void EnterState()
    {
        if (player.Animator != null)
        {
            player.Animator.SetFloat("Speed", 0.5f);
            player.Animator.SetBool("IsGrounded", true);
        }
    }

    public override void UpdateState()
    {
        Vector2 input = player.ReadMoveInput();

        // Feed strafe input into GameFeel for camera tilt while walking
        if (GameFeel.Instance != null) GameFeel.Instance.StrafeInput = input.x;

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

    public override void ExitState() { }
}
