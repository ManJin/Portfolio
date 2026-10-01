using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace Portfolio
{
    // 프로젝타일 스펙 
    public class ProjectileSpec
    {
        // 오브젝트 이름.
        public string Name;
        // 오브젝트 프리셋 이름. 
        public string PresetName; 
        // 히트 후 결과 
        public HitResultType HitResultType;
        // 관통 타입 
        public PenetratingType PenetratingType;

        //공격력계수 
        public float AttackModifier;
        // 탄환 발사 속도 
        public float Speed;
        // 탄환 가속도 
        public float Acceleration;
        // 탄환 생존시간 
        public float LifeTime;
        // 회전각속도 
        public float AngularVelocity;
        // 호밍시의 각속도 
        public float HomingAngularSpeed;

        // 폭발형인가. 
        public bool IsExplode;
        // 폭발반경
        public float ExplosionRadius;
        
        // 판정 상자크기 
        public float HitBoxWidth;
        public float HitBoxHeight;

        public ProjectileSpec()
        {
            
        }
        /*public ProjectileSpec(string name, string preset, HitResultType resultType, PenetratingType penetrateType,
        float atkModifier, float speed, float acc, float lifeTime, float angle, float homing, bool explode, float expRadius,
        float width, float height)
        {
            Name = name;
            PresetName = preset;
            HitResultType = resultType;
            PenetratingType = penetrateType;
            AttackModifier = atkModifier;
            Speed = speed;
            Acceleration = acc;
            LifeTime = lifeTime;
            AngularVelocity = angle;
            HomingAngularSpeed = homing;
            IsExplode = explode;
            ExplosionRadius = expRadius;
            HitBoxWidth = width;
            HitBoxHeight = height;
        }*/
    }
    
    // 업데이트에서 도는 프로젝타일 클래스 
    public class Projectile : IUpdatable
    {
        public GameObject ProjectilePrefab;
        // 이동타입
        public enum MoveType
        {
            Directional,
            Arc,
        }

        // hit 하는 방식 
        public enum HitType
        {
            // 일정시간 후 삭제 
            Timer,

            // 목표 타겟에 맞은경우
            Target,

            // 지형 등에 부딛힌 경우 
            Map,

            // 강제 제거
            Force,
        }

        public struct MoveInfo
        {
            // 현재위치
            public Vector2 position;

            // 갈 방향
            public Vector2 direction;

            // 좌우속도
            public float speed;

            // 상하속도
            public float verticalSpeed;

            // 중력가속도
            public float gravity;
        }

        public struct HitInfo
        {
            public HitType hitType;

            // 맞은 위치
            public Vector2 position;

            // 맞은 방향
            public Vector2 direction;

            // 맞은 대상
            public FieldObject target;

            // 상대의 공격력 * 탄 스펙의 계수로 계산한 데미지 수치 
            public float damage;
        }

        // 현재 위치정보 캐싱
        public MoveInfo CurrentMoveInfo;

        // 이전 위치 정보 캐싱
        protected MoveInfo beforeMoveInfo;

        // 다음 위치 정보 캐싱 
        protected MoveInfo afterMoveInfo;

        [SerializeField]
        public ProjectileSpec Spec;

        // 동작중인가. 
        public bool Activated;

        // 보이는중인가. ?
        public bool Visible;

        // 발사 이후 지난 시간. 
        public float TimePassed;

        // 지정위치 폭파시 필요한 위치 
        public Vector2 TargetPos = Vector2.positiveInfinity;

        // 프로젝타일 발사한 객체 
        public Character Shooter;

        // 타겟 지정하는 경우 
        public GameObject Target;

        //탄환 관통시 중복 피격 검사용 
        public List<FieldObject> HitObjects = new List<FieldObject>();

        public void SetSpec(ProjectileSpec spec)
        {
            Spec = spec;
        }

        protected virtual void OnUpdateFrame(float dt)
        {
            //Debug.LogFormat("Projectile.OnUpdateFrame : {0}", CurrentMoveInfo.position);
            ProjectilePrefab.transform.localPosition = CurrentMoveInfo.position;
        }
        
        public virtual void UpdateFrame(float dt)
        {
            //Debug.LogFormat("Projectile.UpdateFrame : {0}", TimePassed);
            var oldTimePassed = TimePassed;
            TimePassed += dt;

            // 발사한 사람이 죽었다면 프로젝타일도 없애 줌. 
            bool forceEliminated = Shooter == null || Shooter.IsDead;
            if (forceEliminated)
            {
                var hit = new HitInfo
                {
                    hitType = HitType.Force,
                    position = CurrentMoveInfo.position,
                    direction = CurrentMoveInfo.direction,
                    target = null,
                    damage = 0f,
                };
                (Shooter as IProjectileShooter)?.OnHit(CurrentMoveInfo, hit);
                Deactivate();
                return;
            }
            else
            {// 여기서는 정상 동작 중. 
                beforeMoveInfo = CurrentMoveInfo;
                bool overTime = TimePassed > Spec.LifeTime;
                float realDt = overTime ? Spec.LifeTime - oldTimePassed : dt;
                var moveVector = GetMoveVector(realDt);
                
                afterMoveInfo = CurrentMoveInfo;
                afterMoveInfo.position += moveVector;
                bool removeProjectile = CheckProjectileCollision(beforeMoveInfo, afterMoveInfo, realDt);
                if (removeProjectile)
                {
                    Deactivate();
                }
                else
                {
                    //Debug.LogFormat("Move {0}, {1}, {2}", CurrentMoveInfo.position, moveVector, overTime);
                    CurrentMoveInfo.position += moveVector;
                    if (overTime)
                    {// 시간 오버로 삭제함. 
                        var hit = new HitInfo
                        {
                            hitType = HitType.Timer,
                            position = CurrentMoveInfo.position,
                            direction = CurrentMoveInfo.direction,
                            target = null,
                            damage = 0f,
                        };
                        Shooter.OnHit(CurrentMoveInfo, hit);
                        Deactivate();
                    }
                }

                if (Activated)
                {
                    OnUpdateFrame(dt);
                }
            }
        }

        protected virtual Vector2 GetMoveVector(float realDt)
        {
            Vector2 moveDir = CurrentMoveInfo.direction;
            if (Target && Spec.HomingAngularSpeed > 0f)
            {// 호밍되는 경우 
                Vector2 movedir = moveDir;
                float magnitude = movedir.magnitude;
                movedir.Normalize();
                Vector2 targetPos = new Vector2(Target.transform.position.x, Target.transform.position.y);  
                Vector2 diff = (targetPos - CurrentMoveInfo.position).normalized;
                float angleMaxDiff = Spec.HomingAngularSpeed * realDt;

                Vector2 newDir = Utils.RotateToward(movedir, diff, angleMaxDiff * Mathf.Deg2Rad, 0);
                moveDir = newDir.normalized * magnitude;
                CurrentMoveInfo.direction = moveDir;
            }
            // 가속도 계산
            var deltaSpeed = Spec.Acceleration * realDt;
            float movement = (CurrentMoveInfo.speed + 0.5f * deltaSpeed) * realDt;
            //Debug.LogFormat("GetMoveVector : {0}, {1}", moveDir, movement);
            
            Vector2 moveVector = moveDir * movement;
            CurrentMoveInfo.speed += deltaSpeed;

            if (CurrentMoveInfo.gravity > 0)
            {
                var deltaVerticalSpeed = -CurrentMoveInfo.gravity * realDt;
                moveVector += (CurrentMoveInfo.verticalSpeed + deltaVerticalSpeed * 0.5f) * realDt * Vector2.up;
                CurrentMoveInfo.verticalSpeed += deltaVerticalSpeed;
            }

            return moveVector;
        }

        protected virtual bool CheckProjectileCollision(MoveInfo before, MoveInfo after, float dt)
        {
            bool removeProjectile = false;
            var moveVector = after.position - before.position;
            if (before.gravity > 0 && before.verticalSpeed > 0)
            {
                // 상승중에 충돌체크 하지 않음.     
            }
            else
            {
                removeProjectile = CheckBoundsCollision(moveVector);
                
            }

            return removeProjectile;
        }

        protected Bounds CreateBounds()
        {
            Projectile projectile = this;
            Vector2 xVector = Vector2.right;
            Vector2 yVector = Vector2.up;
            Vector2 moveDir = projectile.CurrentMoveInfo.direction;
            Vector2 size = GetSize();
            
            Bounds bounds = new  Bounds(projectile.CurrentMoveInfo.position, Vector2.zero);
            float rad = Mathf.Acos(moveDir.x);
            float angle = Mathf.Rad2Deg * rad;
            Quaternion quarternion = Quaternion.AngleAxis(90-angle, Vector3.forward);
            xVector = quarternion * xVector;
            yVector = quarternion * yVector;
            
            Vector2 pSize = new Vector2(
                Mathf.Abs(xVector.x * size.x) + Mathf.Abs(yVector.x * size.y),
                Mathf.Abs(xVector.y * size.x) + Mathf.Abs(yVector.y * size.y));

            bounds.size = pSize;
            
            return bounds;
        }

        public virtual Vector2 GetSize()
        {
            if (Spec.HitBoxHeight > 0 && Spec.HitBoxWidth > 0)
            {
                return new Vector2(Spec.HitBoxWidth, Spec.HitBoxHeight);
            }
            else
            {
                return new Vector2(1f, 1f);
            }
        }

        public bool CheckBoundsCollision(Vector2 moveVector)
        {
            Projectile projectile = this;
            Bounds bounds = projectile.CreateBounds();
            float halfSize = Mathf.Max(bounds.extents.x, bounds.extents.y) / 2.0f;
            bool? removeProjectile = false;
            // 충돌 가능한 것들에 대한 리스트를 구함.
            var before = projectile.CurrentMoveInfo;
            var list = MainSystem.Instance.GetFieldObjectsCollideBy(bounds, moveVector);
            
            for (int i = 0; i < list.Count; i++)
            {
                
                if (ReferenceEquals(list[i], (FieldObject)projectile.Shooter) == true)
                {// 발사한 당사자는 패스 
                    continue;
                }
                
                if (projectile.HitObjects.Contains(list[i]))
                {//이미 히트된 오브젝트라면 패스 
                    continue;
                }
                
                // 여기부터는 히트 성공 판정으로 
                projectile.HitObjects.Add(list[i]);

                MoveInfo move = before;
                var dist = Utils.GetDistanceBetween(bounds, list[i].HittableBounds);
                move.position = before.position + moveVector.normalized * dist;
                HitInfo hit = new HitInfo
                {
                    hitType = HitType.Target,
                    position = before.position + moveVector.normalized * (dist + halfSize),
                    direction = moveVector.normalized,
                    target = list[i],
                    damage = projectile.Shooter.BasicAttack * Spec.AttackModifier,
                };
                // 히트된 대상이 
                removeProjectile = (hit.target as IProjectileShooter)?.OnHit(move, hit);
            }
            
            list.Dispose();
            list.Clear();
            
            return removeProjectile ?? false;
        }

        

        public void Active()
        {
            OnActive();
        }
        
        protected virtual void OnActive()
        {
            ProjectilePrefab.SetActive(true);
            Activated = true;
            Visible = true;
            TimePassed = 0;
            HitObjects.Clear();
        }
        
        public void Deactivate()
        {
            OnDeactivate();
        }

        protected virtual void OnDeactivate()
        {
            ProjectilePrefab.SetActive(false);
            ProjectilePrefab = null;
            TimePassed = 0;
            Activated = false;
            Spec = null;
            Target = null;
            Shooter = null;
            HitObjects.Clear();
        }
    }    
}

