using UnityEngine;
using Dwayne.Abilities;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Universal weapon that works with any BaseAbility (hitscan or projectile).
    /// Works with hitscan abilities (FireSpreadAbility, FireFocusAbility, etc.)
    /// and projectile abilities (GenericProjectileAbility, etc.).
    /// If no fireAbility is assigned, falls back to BaseWeapon's built-in fire logic.
    /// Configure fallback behavior in BaseWeapon inspector fields.
    /// </summary>
    public class UniversalWeapon : BaseWeapon
    {
        /// <summary>
        /// Override to fall back to weapon's Fire() method if fireAbility is null.
        /// </summary>
        public override bool TryUseFireAbility(Vector3 targetPosition = default)
        {
            // If ability exists, use it
            if (fireAbility != null)
            {
                return base.TryUseFireAbility(targetPosition);
            }

            // Fall back to weapon's built-in Fire() method
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : transform.forward;

            return Fire(origin, direction);
        }

        /// <summary>
        /// Override to fall back to weapon's Fire() method if altFireAbility is null.
        /// </summary>
        public override bool TryUseAltFireAbility(Vector3 targetPosition = default)
        {
            // If ability exists, use it
            if (altFireAbility != null)
            {
                return base.TryUseAltFireAbility(targetPosition);
            }

            // Fall back to weapon's built-in Fire() method (same as fire ability)
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : transform.forward;

            return Fire(origin, direction);
        }

        protected override bool DoFire(Vector3 origin, Vector3 direction)
        {
            return DoFire(origin, direction, 1f);
        }

        protected override bool DoFire(Vector3 origin, Vector3 direction, float charge)
        {
            // If fireAbility is assigned, use it
            if (fireAbility != null)
            {
                // Check if ability can be used
                if (!fireAbility.CanUse)
                {
                    return false;
                }

                // Calculate target position from origin and direction
                Vector3 targetPosition = origin + direction * range;

                // Use the ability - it will handle the attack (hitscan or projectile)
                bool success = fireAbility.Use(Owner, targetPosition);

                return success;
            }

            // Fall back to BaseWeapon's built-in fire logic
            return DoFallbackFire(origin, direction, charge);
        }
    }
}
