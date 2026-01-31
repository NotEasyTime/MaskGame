using UnityEngine;
using Dwayne.Interfaces;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Weapon that spawns a projectile per shot. Use for rockets, grenades, arrows.
    /// Assign a prefab with a component implementing IProjectile (e.g. SimpleProjectile).
    /// </summary>
    public class ProjectileWeapon : BaseWeapon
    {
        [Header("Projectile")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 50f;
        [SerializeField] protected bool useChargeDamage = true;

        protected override bool DoFire(Vector3 origin, Vector3 direction)
        {
            return DoFire(origin, direction, 1f);
        }

        protected override bool DoFire(Vector3 origin, Vector3 direction, float charge)
        {
            if (projectilePrefab == null)
                return false;

            float damageAmount = damage;
            if (useChargeDamage)
                damageAmount *= Mathf.Lerp(1f, fullChargeDamageMultiplier, charge);

            GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            var projectile = go.GetComponent<IProjectile>();
            if (projectile != null)
                projectile.Launch(origin, direction, projectileSpeed, damageAmount, Owner);

            return true;
        }
    }
}
