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
        public override bool Fire(Vector3 origin, Vector3 direction)
        {
            bool fired = base.Fire(origin, direction);
            if (fired && fireAbility != null && currentShots == 0)
                nextRefillTime = Time.time + fireAbility.CooldownDuration;
            return fired;
        }

        public override bool ReleaseCharge()
        {
            bool fired = base.ReleaseCharge();
            if (fired && fireAbility != null && currentShots == 0)
                nextRefillTime = Time.time + fireAbility.CooldownDuration;
            return fired;
        }

        /// <summary>
        /// When fireAbility is set: uses magazine size as ability charges; when mag is empty, refill cooldown uses the ability's CooldownDuration.
        /// Alt-fire is unchanged (no magazine). Falls back to Fire() when fireAbility is null.
        /// </summary>
        public override bool TryUseFireAbility(Vector3 targetPosition = default)
        {
            if (fireAbility != null)
            {
                // Use magazine as ability charges: require ammo and weapon fire cooldown (not ability's per-use cooldown)
                if (currentShots <= 0 || (fireCooldown > 0f && Time.time < nextFireTime))
                    return false;

                bool success = fireAbility.UseFromWeapon(Owner, targetPosition);
                if (success)
                {
                    currentShots--;
                    if (fireCooldown > 0f)
                        nextFireTime = Time.time + fireCooldown;
                    if (currentShots == 0)
                        nextRefillTime = Time.time + fireAbility.CooldownDuration;
                }
                return success;
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
            // If fireAbility is assigned, use it (magazine is consumed by Fire()/ReleaseCharge(); refill uses ability cooldown)
            if (fireAbility != null)
            {
                Vector3 targetPosition = origin + direction * range;
                return fireAbility.UseFromWeapon(Owner, targetPosition);
            }

            // Fall back to BaseWeapon's built-in fire logic
            return DoFallbackFire(origin, direction, charge);
        }
    }
}
