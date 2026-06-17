using UnityEngine;

namespace clone
{
    // 캐릭터 자동으로 움직이는 상태. 지정된 방향으로 지정한 시간만큼 움직인다. 
    public class AiAttackState : IState, ILateUpdatable, IDisposable
    {
        private Character character;
        private Character target;
        
        private float timePassed;

        private float randWaiting = 0f;
        private float maxAttackableTima = 0f;
        private static ObjectPool<AiAttackState> pool = new ObjectPool<AiAttackState>();

        private ProjectileManager manager = null;

        public bool IsDone { get; private set; }= false;
        
        private int attackTypeCount = 3;
        // 공격 방식을 셋중 하나로 골라서 공격 하도록...?
        public static AiAttackState Create(Character character, Character target, float attackableTime)
        {
            AiAttackState s = pool.GetOrCreate();
            s.character = character;
            s.target = target;
            // 최대 공격에 걸리는 시간 
            s.maxAttackableTima = attackableTime;
            s.timePassed = 0f;
            return s;
        }
        
        public void Enter(IState prevState)
        {
            timePassed = 0f;
            randWaiting = Random.Range(0.05f, maxAttackableTima);
            manager = MainSystem.Instance.ProjectileManager;
            IsDone = false;
        }

        public void Exit(IState nextState)
        {
            Dispose();
        }

        public void Dispose()
        {
            timePassed = 0f;
            IsDone = false;
            manager = null;
            character = null;
        }

        public void LateUpdateFrame(float dt)
        {
            if (IsDone)
            {
                return;
            }

            timePassed += dt;
             
            if (timePassed >= randWaiting)
            {
                // 발사 이후에 다른 행동을 하지 않음. 
                IsDone = true;
                var kind = Random.Range(0, attackTypeCount);

                if (kind == 0)
                {
                    // 랜덤한 시간 만큼 대기 후 공격 실행 
                    manager.Shoot1(character, character.Position, Vector2.left);    
                }
                else if (kind == 1)
                {
                    manager.Shoot2(character, character.Position, Vector2.left);
                }
                else if (kind == 2)
                {
                    manager.Shoot3(character, character.Position, Vector2.left);
                }
            }
        }
    }    
}