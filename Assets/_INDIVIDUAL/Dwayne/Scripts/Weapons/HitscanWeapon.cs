using UnityEngine;
using Dwayne.Abilities;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Weapon that uses BaseAbility for hitscan/instant attacks.
    /// Works with any BaseAbility (FireSpreadAbility, FireFocusAbility, etc.).
    /// The fireAbility handles the actual attack logic (raycast, damage, VFX).
    /// </summary>
    public class HitscanWeapon : BaseWeapon
    {
        [Header("Hitscan Settings")]
        [SerializeField] protected bool useChargeDamage = false;

        protected override bool DoFire(Vector3 origin, Vector3 direction)
        {
            return DoFire(origin, direction, 1f);
        }

        protected override bool DoFire(Vector3 origin, Vector3 direction, float charge)
        {
            // Use the fireAbility (any BaseAbility)
            if (fireAbility == null)
            {
                Debug.LogWarning($"HitscanWeapon '{name}' has no fireAbility assigned! Please assign a BaseAbility.");
                return false;
            }

            // Check if ability can be used
            if (!fireAbility.CanUse)
            {
                return false;
            }

            // Calculate target position from origin and direction
            Vector3 targetPosition = origin + direction * range;

            // Use the ability - it will handle the hitscan attack
            bool success = fireAbility.Use(Owner, targetPosition);

            return success;
        }
    }
}
