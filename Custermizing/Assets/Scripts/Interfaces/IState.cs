using UnityEngine;

namespace Portfolio
{
    public interface IState
    {
        void Enter(IState prevState);
        
        void Exit(IState nextState);
    }    
}

