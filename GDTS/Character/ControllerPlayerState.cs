using UnityEngine;

namespace clone
{
    /// <summary>
    /// 플레이어 동작 컨트롤 스테이트 
    /// </summary>
    public class ControllerPlayerState : IState, IEventListner, ILateUpdatable, IDisposable
    {
        private Character character;

        // 해당 스테이트에서 지난 시간
        private float timePassed = 0;

        // 액션의 중복실행 방지
        private bool actionProcess = false;

        private static ObjectPool<ControllerPlayerState> pool = new ObjectPool<ControllerPlayerState>();
        public static ControllerPlayerState Create(Character character)
        {
            var s = pool.GetOrCreate();
            s.character = character;
            
            return s;
        }
        
        void IDisposable.Dispose()
        {
            pool.Return(this);
        }

        private bool moveleft = false;
        private bool moveright = false;

        void ILateUpdatable.LateUpdateFrame(float dt)
        {
            if (moveleft)
            {
                MoveLogic.ExcuteMove(character, Vector2.left, character.MoveSpeed * dt);
                character.HittableBounds.center = character.Position;
            }

            if (moveright)
            {
                MoveLogic.ExcuteMove(character, Vector2.right, character.MoveSpeed * dt);
                character.HittableBounds.center = character.Position;
            }
        
        
        }

        void IState.Enter(IState prevState)
        {
            MessageSystem.Instance.Subscribe<TouchEvent>(((IEventListner)this).OnEvent);
        }

        void IState.Exit(IState nextState)
        {
            MessageSystem.Instance.Unsubscribe<TouchEvent>(((IEventListner)this).OnEvent);
        }

        bool IEventListner.OnEvent(Event e)
        {// 스킬 들어오면 액션 실행 
            if (e is TouchEvent te)
            {
                if (te.TouchEventType == TouchEventType.Action1TouchDown)
                {
                    /*if (actionProcess)
                        return false;*/
                    actionProcess = true;
                    //액션 실행
                    MainSystem.Instance.ProjectileManager.Shoot1(character, character.Position, Vector2.right);
                }
                else if (te.TouchEventType == TouchEventType.Action1TouchUp)
                {
                    actionProcess = false;
                }
                else if (te.TouchEventType == TouchEventType.Action2TouchDown)
                {
                    /*if (actionProcess)
                        return false;*/
                    actionProcess = true;
                    MainSystem.Instance.ProjectileManager.Shoot2(character, character.Position, Vector2.right);
                }
                else if (te.TouchEventType == TouchEventType.Action2TouchUp)
                {
                    actionProcess = false;   
                }
                else if (te.TouchEventType == TouchEventType.Action3TouchDown)
                {
                    /*if (actionProcess)
                        return false;*/
                    actionProcess = true;
                    MainSystem.Instance.ProjectileManager.Shoot3(character, character.Position, Vector2.right);
                }
                else if (te.TouchEventType == TouchEventType.Action3TouchUp)
                {
                    actionProcess = false;
                }

                // 이동 동작의 경우에는 나중 눌린걸로 바꿔줄수 있도록.. 
                if (te.TouchEventType == TouchEventType.MoveLeftTouchDown)
                {
                    //MoveLogic.ExcuteMove(character, Vector2.left, 1f);
                    if (moveright) moveright = false;
                    moveleft = true;
                }
                else if  (te.TouchEventType == TouchEventType.MoveLeftTouchUp)
                {
                    moveleft = false;
                }
                else if (te.TouchEventType == TouchEventType.MoveRightTouchDown)
                {
                    //MoveLogic.ExcuteMove(character, Vector2.right, 1f);
                    if (moveleft) moveleft = false;
                    moveright = true;
                }
                else if (te.TouchEventType == TouchEventType.MoveRightTouchUp)
                {
                    moveright = false;
                }
            }
            return false;
        }
    }    
}

