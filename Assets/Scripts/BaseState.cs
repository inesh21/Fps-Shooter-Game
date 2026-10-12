public abstract class BaseState
{
    protected PlayerFSM player;

    public BaseState(PlayerFSM player)
    {
        this.player = player;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}