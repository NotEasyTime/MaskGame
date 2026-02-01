using UnityEngine;
using Dwayne.Abilities;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Weapon that uses ProjectileAbility to spawn pooled projectiles.
    /// The fireAbility should be a ProjectileAbility (assigned directly, not instantiated).
    /// The ability handles spawning projectiles from the ObjectPoolManager.
    /// </summary>
    public class ProjectileWeapon : BaseWeapon
    {
        [Header("Projectile Settings")]
        [SerializeField] protected bool useChargeDamage = true;

        protected override bool DoFire(Vector3 origin, Vector3 direction)
        {
            return DoFire(origin, direction, 1f);
        }

        protected override bool DoFire(Vector3 origin, Vector3 direction, float charge)
        {
            // Use the fireAbility (should be a ProjectileAbility)
            if (fireAbility == null)
            {
                Debug.LogWarning($"ProjectileWeapon '{name}' has no fireAbility assigned! Please assign a ProjectileAbility.");
                return false;
            }

            // Verify it's a ProjectileAbility
            ProjectileAbility projectileAbility = fireAbility as ProjectileAbility;
            if (projectileAbility == null)
            {
                Debug.LogError($"ProjectileWeapon '{name}' fireAbility '{fireAbility.name}' is not a ProjectileAbility!");
                return false;
            }

            // Check if ability can be used
            if (!projectileAbility.CanUse)
            {
                return false;
            }

            // Calculate target position from origin and direction
            Vector3 targetPosition = origin + direction * range;

            // Use the ability - it will spawn the projectile from the pool
            bool success = projectileAbility.Use(Owner, targetPosition);

            return success;
        }
    }
}
