using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Abilities;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Weapon that spawns a projectile per shot using ProjectileAbility from fireAbility.
    /// The fireAbility should be assigned a ProjectileAbility prefab.
    /// </summary>
    public class ProjectileWeapon : BaseWeapon
    {
        [Header("Projectile Settings")]
        [SerializeField] protected float projectileSpeed = 50f;
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
                Debug.LogWarning($"ProjectileWeapon '{name}' has no fireAbility assigned! Please assign a ProjectileAbility prefab.");
                return false;
            }

            // Verify it's a ProjectileAbility
            ProjectileAbility projectileAbility = fireAbility as ProjectileAbility;
            if (projectileAbility == null)
            {
                Debug.LogError($"ProjectileWeapon '{name}' fireAbility '{fireAbility.name}' is not a ProjectileAbility! Please assign a ProjectileAbility component.");
                return false;
            }

            // Calculate damage with charge multiplier
            float damageAmount = damage;
            if (useChargeDamage)
                damageAmount *= Mathf.Lerp(1f, fullChargeDamageMultiplier, charge);

            // Instantiate the projectile ability GameObject
            GameObject projectileInstance = Instantiate(fireAbility.gameObject, origin, Quaternion.LookRotation(direction));
            ProjectileAbility projectileComponent = projectileInstance.GetComponent<ProjectileAbility>();

            if (projectileComponent != null)
            {
                // Launch the projectile
                projectileComponent.Launch(origin, direction, projectileSpeed, damageAmount, Owner);
                return true;
            }

            Debug.LogError($"ProjectileWeapon '{name}': Failed to get ProjectileAbility component from instantiated object!");
            return false;
        }
    }
}
