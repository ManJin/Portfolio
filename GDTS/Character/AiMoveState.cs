using UnityEngine;

namespace clone
{
    // 캐릭터 자동으로 움직이는 상태. 지정된 방향으로 지정한 시간만큼 움직인다. 
    public class AiMoveState : IState, ILateUpdatable, IDisposable
    {
        private Character character;
        Vector2 direction;
        private float movableTime;
        private float timePassed;
        
        private static ObjectPool<AiMoveState> pool = new ObjectPool<AiMoveState>();
        
        public static AiMoveState Create(Character character, Vector2 direction, float time)
        {
            AiMoveState s = pool.GetOrCreate();
            s.character = character;
            s.direction = direction;
            s.movableTime = time;
            s.timePassed = 0f;
            return s;
        }
        
        public void Enter(IState prevState)
        {
            timePassed = 0f;
        }

        public void Exit(IState nextState)
        {
            Dispose();
        }

        public void Dispose()
        {
            timePassed = 0f;
            character = null;
        }

        public void LateUpdateFrame(float dt)
        {
            timePassed += dt;
            if (timePassed < movableTime)
            {
                MoveLogic.ExcuteMove(character, direction, character.MoveSpeed * dt);    
            }
        }
    }    
}

