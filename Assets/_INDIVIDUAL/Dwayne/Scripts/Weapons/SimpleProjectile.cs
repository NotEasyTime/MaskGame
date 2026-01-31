using Interfaces;
using UnityEngine;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Projectile that applies damage on hit and is destroyed on hit or expire.
    /// Use as the component on the prefab assigned to ProjectileWeapon.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleProjectile : BaseProjectile
    {
        public override void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            var damagable = other.GetComponent<IDamagable>();
            if (damagable != null && damagable.IsAlive)
                damagable.TakeDamage(damage, point, -direction, owner);

            Destroy(gameObject);
        }

        public override void OnExpire()
        {
            Destroy(gameObject);
        }
    }
}
