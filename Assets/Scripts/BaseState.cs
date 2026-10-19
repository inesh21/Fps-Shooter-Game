public abstract class BaseState
{
    protected readonly PlayerFSM player;

    protected BaseState(PlayerFSM owner) { player = owner; }

    public virtual void EnterState() { }
    public abstract void UpdateState();
    public virtual void ExitState() { }
}
