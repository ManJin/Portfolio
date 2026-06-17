using UnityEngine;

namespace clone
{
    public class StateMachine : IStateMachine, IDisposable
    {

        // 다음 스테이트
        private IState nextState = null;
        // 다음스테이트가 있는가?
        private bool hasNextState = false;
        //현재 스테이트 
        private IState currentState = null;
        
        // 다음상태로 변환 예약되어있는 경우 다음 스테이트를 리턴
        public IState CurrentState => hasNextState ? nextState : currentState;

        public void ChangeState(IState newState)
        {
            if (hasNextState)
            {
                (nextState as IDisposable)?.Dispose();
            }

            nextState = newState;
            hasNextState = true;
        }

        void IDisposable.Dispose()
        {
            currentState?.Exit(null);
            (currentState as IDisposable)?.Dispose();
            currentState = null;
            
            (nextState as IDisposable)?.Dispose();
            nextState = null;
            hasNextState = false;
            
        }

        public bool ProcessTransition()
        {
            if (hasNextState == false)
            {
                return false;
            }

            var prevState = currentState;
            prevState?.Exit(nextState);
            nextState?.Enter(prevState);
            // 다음스테이트로 넘겨주고나서 Dispose 함. 
            (prevState as IDisposable)?.Dispose();
            currentState = nextState;
            nextState = null;
            hasNextState = false;
            return true;
        }
        
        
    }    
}

