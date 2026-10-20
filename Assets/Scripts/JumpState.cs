using UnityEngine;

public sealed class JumpState : BaseState
{
    public JumpState(PlayerFSM player) : base(player) { }

    public override void EnterState()
    {
        player.StartJump();
        if (player.Animator != null)
        {
            player.Animator.SetBool("IsGrounded", false);
            player.Animator.SetTrigger("Jump");
        }
    }

    public override void UpdateState()
    {
        Vector2 input = player.ReadMoveInput();
        bool moving = input.sqrMagnitude > 0.01f;
        bool sprinting = moving && player.SprintHeld;
        player.MoveHorizontally(input, sprinting ? player.sprintSpeed : player.walkSpeed);

        // Wait until the controller has landed and the upward arc has ended.
        if (!player.IsGrounded || player.VerticalVelocity > 0f) return;

        // Update animator when landing
        if (player.Animator != null)
        {
            player.Animator.SetBool("IsGrounded", true);
        }

        if (!moving) player.ChangeState(new IdleState(player));
        else if (sprinting) player.ChangeState(new SprintState(player));
        else player.ChangeState(new WalkState(player));
    }

    public override void ExitState() { }
}
