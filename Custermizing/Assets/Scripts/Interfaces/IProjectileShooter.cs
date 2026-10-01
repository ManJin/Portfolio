using UnityEngine;

namespace Portfolio
{
    public interface IProjectileShooter
    {
        public bool OnHit(Projectile.MoveInfo moveInfo, Projectile.HitInfo HitInfo);
    }    
}

