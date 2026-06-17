using UnityEngine;

namespace clone
{
    public class ControllerEnemyState : IState, IEventListner, ILateUpdatable, IDisposable
    {
        private Character character;
        private Character target;
        
        private static ObjectPool<ControllerEnemyState> pool = new ObjectPool<ControllerEnemyState>();
        
        private StateMachine subStateMachine =  new StateMachine();

        private Direction direction;

        private float timePassed = 0f;
        
        private float followTime = 1.1f;
        private float retreatTime = 0.9f;
        private float attackTime = 0.5f;

        private int step = 0;
        private int maxStep = 4;
        public static ControllerEnemyState Create(Character character, Character target)
        {
            var s = pool.GetOrCreate();
            s.character = character;
            s.target = target;
            return s;
        }

        Vector2 DirectionToVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return Vector2.left;
                case Direction.Right:
                    return Vector2.right;
                default:
                    return Vector2.zero;
            }
        }
        void IState.Enter(IState prevState)
        {// 자동 움직임 및 스킬.. 
            // 타깃과의 거리유지를 기본으로 
            // 노말액션 -> 스킬1 -> 스킬 2 순으로 동작 
            // 스킬1->2로 넘어가는 경우에 추가적으로 거리를 벌림. 
            // 스킬2 시전 후 1초간 전진?
            direction = Direction.Left;
            subStateMachine.ChangeState(AiMoveState.Create(character, DirectionToVector(direction), followTime));
            step = 0;
        }

        void IState.Exit(IState nextState)
        {
            Dispose();
        }

        public bool OnEvent(Event e)
        {
            return false;
        }

        public void LateUpdateFrame(float dt)
        {
            timePassed += dt;

            subStateMachine.ProcessTransition();
            (subStateMachine.CurrentState as ILateUpdatable)?.LateUpdateFrame(dt);
            
            // 전진
            if (step % maxStep == 0)
            {
                if (timePassed >= followTime)
                {
                    step++;
                    timePassed = 0f;
                    direction = Direction.Left;
                    subStateMachine.ChangeState(AiMoveState.Create(character, DirectionToVector(direction), followTime));
                }
            }
            else if (step % maxStep == 1)
            {
                if (timePassed >= attackTime)
                {
                    step++;
                    timePassed = 0f;
                    subStateMachine.ChangeState(AiAttackState.Create(character, MainSystem.Instance.player, attackTime));
                }
            }
            else if (step % maxStep == 2)
            {
                if (timePassed >= retreatTime)
                {
                    step++;
                    timePassed = 0f;
                    direction = Direction.Right;
                    subStateMachine.ChangeState(AiMoveState.Create(character, DirectionToVector(direction), retreatTime));
                }
            }
            else if (step % maxStep == 3)
            {
                if (timePassed >= attackTime)
                {
                    step++;
                    timePassed = 0f;
                    subStateMachine.ChangeState(AiAttackState.Create(character, MainSystem.Instance.player, attackTime));
                }
            }
            
            if (step >= maxStep * 10)
            {
                step = 0;
            }
        }

        public void Dispose()
        {
            timePassed = 0;
            (subStateMachine as IDisposable)?.Dispose();
        }
    }    
}

