using UnityEngine;

public sealed class SprintState : BaseState
{
    public SprintState(PlayerFSM player) : base(player) { }

    public override void EnterState()
    {
        if (player.Animator != null)
        {
            player.Animator.SetFloat("Speed", 1f);
            player.Animator.SetBool("IsGrounded", true);
        }
        if (GameFeel.Instance != null) GameFeel.Instance.IsSprinting = true;
    }

    public override void UpdateState()
    {
        Vector2 input = player.ReadMoveInput();

        // Feed strafe input into GameFeel for camera tilt
        if (GameFeel.Instance != null) GameFeel.Instance.StrafeInput = input.x;

        if (input.sqrMagnitude <= 0.01f)
        {
            player.ChangeState(new IdleState(player));
            return;
        }
        if (!player.SprintHeld)
        {
            player.ChangeState(new WalkState(player));
            return;
        }
        player.MoveHorizontally(input, player.sprintSpeed);
    }

    public override void ExitState()
    {
        if (GameFeel.Instance != null) GameFeel.Instance.IsSprinting = false;
    }
}
