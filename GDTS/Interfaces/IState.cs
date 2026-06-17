using UnityEngine;

namespace clone
{
    public interface IState
    {
        void Enter(IState prevState);
        
        void Exit(IState nextState);
    }    
}

