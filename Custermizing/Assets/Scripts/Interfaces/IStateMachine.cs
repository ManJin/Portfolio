
namespace Portfolio
{
    public interface IStateMachine
    {
        IState CurrentState { get; }
        void ChangeState(IState newState);
    }    
}
