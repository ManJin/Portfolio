using NUnit.Framework.Constraints;
using UnityEngine;

namespace clone
{
    public class Character : FieldObject, IProjectileShooter, IUpdatable, IDisposable, IEventListner
    {
        public bool IsDead = false;
        // 플레이어블 캐릭터인가. 
        public bool IsPlayer = false;
        // 캐릭터 오브젝트 
        private Transform transform;

        public Transform Transform
        {
            get
            {
                if (transform == null)
                {
                    transform = GetComponent<Transform>();
                }

                return transform;
            }
        }
        
        public Vector2 Position
        {
            get => Transform.localPosition;
            set
            {
                if (!Utils.Equals(Transform.localPosition, value))
                {
                    Transform.localPosition = value;
                }
            }
        }
        
        // 기본 공격력
        public float BasicAttack { get; } = 14.0f;
        // 최대 HP
        public float MaxHp { get; } = 100;
        // 현재 Hp
        public float Hp { get; private set; } = 100;
        // 최대 스킬게이지
        public float MaxMp { get; } = 100;
        // 현재 스킬게이지 
        public float Mp { get; private set; } = 100;

        // 이동 기본 속도 
        public float MoveSpeed = 1f;
        
        public Direction Direction =  Direction.None;
        private ObjectController objectController;

        public ObjectController ObjectController
        {
            get { return objectController; }
            set
            {
                // 같은거면 중복 설정 안함. 
                if (objectController == value)
                    return;
                objectController?.DetachFrom(this);
                (objectController as IDisposable)?.Dispose();
                objectController = value;
                objectController.AttachTo(this);    
            }
        }

        void Start()
        {
            ObjectController = new ObjectController();
            HittableBounds.center = Position;
            MessageSystem.Instance.Publish(HpChangedEvent.Create(IsPlayer, Hp));
        }
        public bool OnHit( Projectile.MoveInfo moveInfo, Projectile.HitInfo hitInfo )
        {
            if (hitInfo.target != null && hitInfo.target is Character c)
            {
                if (ReferenceEquals(c, this))
                {
                    Damage(hitInfo);
                    return true;
                }    
            }
            return false;
        }

        // hp가 바닥까지 내려가면 게임 종료
        void Damage(Projectile.HitInfo info)
        {
            if (IsDead)
            {
                return;
            }
                
            Hp -= info.damage;
            //Debug.LogFormat("Damage : {0}->{1}", Hp, IsDead);
            if (Hp <= 0f)
            {
                IsDead = true;
                Hp = 0;
            }
            // HP 변경 이벤트 발송 
            MessageSystem.Instance.Publish(HpChangedEvent.Create(IsPlayer, Hp));
        }
        
        void IUpdatable.UpdateFrame(float dt)
        {
            
        }

        void IDisposable.Dispose()
        {
            
        }

        bool IEventListner.OnEvent(Event e)
        {
            if (e is TouchEvent te)
            {
                if (IsPlayer == false)
                    return false;
            }
            return false;
        }
    }    
}

