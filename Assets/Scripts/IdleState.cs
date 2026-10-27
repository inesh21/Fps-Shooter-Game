using UnityEngine;

public sealed class IdleState : BaseState
{
    public IdleState(PlayerFSM player) : base(player) { }

    public override void EnterState()
    {
        if (player.Animator != null)
        {
            player.Animator.SetFloat("Speed", 0f);
            player.Animator.SetBool("IsGrounded", true);
        }
        // Clear tilt when coming to rest
        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.StrafeInput  = 0f;
            GameFeel.Instance.IsSprinting  = false;
        }
    }

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

    public override void ExitState() { }
}
