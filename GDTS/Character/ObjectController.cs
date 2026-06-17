using System.Runtime.CompilerServices;
using UnityEngine;

namespace clone
{
    public class ObjectController : IComponent, IEventListner, ILateUpdatable
    {
        // 이 컨트롤러를 가지틑 캐릭터 
        private Character character;
        // 현재 컨트롤러 상태 
        private IState CurrnetState => stateMachine.CurrentState;
        // 스테이트머신
        private StateMachine stateMachine = new  StateMachine();

        public void AttachTo(FieldObject fo)
        {// 오브젝트에 컨트롤러 붙인 경우 
            character = fo as Character;
            if (!character)
            {
                Debug.LogErrorFormat("ObjectController try attach to not character");
            }
            
            MainSystem.Instance.AddLateUpdateFrameCallback(0, this);
            // 터치이벤트에 반응 시키려면 구독 해서.. 
            MessageSystem.Instance.Subscribe<TouchEvent>(((IEventListner)this). OnEvent);

            if (character.IsPlayer)
            {
                // 플레이어면 플레이어스테이트로
                stateMachine.ChangeState(ControllerPlayerState.Create(character));
            }
            else
            {
                // 적이면 적 스테이트로 
                stateMachine.ChangeState(ControllerEnemyState.Create(character, MainSystem.Instance.player));
            }
            
        }

        public void DetachFrom(FieldObject fo)
        {// 오브젝트에 컨트롤러 뗀 경우 
            MainSystem.Instance.RemoveLateUpdateFrameCallback(0, this);
            MessageSystem.Instance.Unsubscribe<TouchEvent>(((IEventListner)this). OnEvent);
            stateMachine.ChangeState(null);
            stateMachine.ProcessTransition();
            character = null;
        }

        void ILateUpdatable.LateUpdateFrame(float dt)
        {
            if (!character)
            {
                return;
            }

            stateMachine.ProcessTransition();
            (stateMachine.CurrentState as ILateUpdatable)?.LateUpdateFrame(dt);
        }

        bool IEventListner.OnEvent(Event e)
        {
            if (e is TouchEvent te)
            {
                return true;
            }
            return false;
        }
        
        
    }    
}

