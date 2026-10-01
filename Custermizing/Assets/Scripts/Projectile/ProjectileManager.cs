using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class ProjectileManager : MonoBehaviour, IUpdatable
    {
        public ProjectileSpec NormalSpec = null;
        public ProjectileSpec Skill1Spec = null;
        public ProjectileSpec Skill2Spec = null;
        
        private readonly List<Projectile> projectiles = new List<Projectile>();
        public void Awake()
        {
            
        }

        void Start()
        {
            MainSystem.Instance.AddUpdateFrameCallback(2, this);
            
            NormalSpec = new ProjectileSpec
            {
                Name = "normal_projectile",
                PresetName = "NormalProjectile",
                HitResultType = HitResultType.Damage,
                PenetratingType = PenetratingType.None,
                AttackModifier = 1f,
                Speed = 10f,
                Acceleration = 0f,
                LifeTime = 5f,
                AngularVelocity = 0f,
                HomingAngularSpeed = 0f,
                IsExplode = false,
                ExplosionRadius = 0f,
                HitBoxWidth = 1f,
                HitBoxHeight = 1f,
            };

            Skill1Spec = new ProjectileSpec
            {
                Name = "skill1_projectile",
                PresetName = "Skill1Projectile",
                HitResultType = HitResultType.Damage,
                PenetratingType = PenetratingType.None,
                AttackModifier = 1f,
                Speed = 5f,
                Acceleration = 0f,
                LifeTime = 10f,
                AngularVelocity = 0f,
                HomingAngularSpeed = 0f,
                IsExplode = false,
                ExplosionRadius = 0f,
                HitBoxWidth = 1f,
                HitBoxHeight = 1f,
            };

            Skill2Spec = new ProjectileSpec
            {
                Name = "skill2_projectile",
                PresetName = "Skill2Projectile",
                HitResultType = HitResultType.Damage,
                PenetratingType = PenetratingType.Enemy,
                AttackModifier = 1f,
                Speed = 5f,
                Acceleration = 3f,
                LifeTime = 10f,
                AngularVelocity = 30f,
                HomingAngularSpeed = 30f,
                IsExplode = true,
                ExplosionRadius = 5f,
                HitBoxWidth = 1f,
                HitBoxHeight = 1f,
            };
        }
        void IUpdatable.UpdateFrame(float dt)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                (projectiles[i] as IUpdatable).UpdateFrame(dt);
                if (projectiles[i].Activated == false)
                {
                    projectiles.RemoveAt(i);
                }
            }
        }

        public void Shoot1(IProjectileShooter shooter, Vector3 startPos, Vector3 direction, Character target = null)
        {
            Shoot(shooter, startPos, direction, NormalSpec, target);    
        }
        
        public void Shoot2(IProjectileShooter shooter, Vector3 startPos, Vector3 direction, Character target = null)
        {
            Shoot(shooter, startPos, direction, Skill1Spec, target);    
        }
        
        public void Shoot3(IProjectileShooter shooter, Vector3 startPos, Vector3 direction, Character target = null)
        {
            Shoot(shooter, startPos, direction, Skill2Spec, target);    
        }
        
        void Shoot(IProjectileShooter shooter, Vector3 startPos, Vector3 direction, ProjectileSpec spec,
            Character target = null)
        {
            Projectile p;
            var pool = UnityObjectPool.GetOrCreate(spec.PresetName);
            var pooledObject = pool.Instantiate(startPos);
            var projectile = new Projectile();
            projectile.SetSpec(spec);
            // 오브젝트 세팅 
            projectile.ProjectilePrefab = pooledObject;
            projectiles.Add(projectile);

            projectile.CurrentMoveInfo.position = startPos;
            projectile.CurrentMoveInfo.direction = direction;
            projectile.CurrentMoveInfo.speed = spec.Speed;
            projectile.Target = target?.gameObject;
            projectile.Shooter = shooter as Character;
            projectile.Active();
        }
    }    
}

