using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Weapons;
using Pool;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Base class for abilities that spawn projectiles from the object pool.
    /// Spawns a projectile when the ability is used (DoUse).
    /// The projectile handles movement, collision, and damage.
    /// </summary>
    public abstract class ProjectileAbility : BaseAbility
    {
        [Header("Projectile Spawning")]
        [Tooltip("Projectile prefab to spawn from pool (must have BaseProjectile component)")]
        [SerializeField] protected BaseProjectile projectilePrefab;

        [Tooltip("Pool name for this projectile (defaults to prefab name)")]
        [SerializeField] protected string poolName;

        [Header("Projectile Properties")]
        [SerializeField] protected float projectileSpeed = 20f;
        [SerializeField] protected float projectileDamage = 10f;

        /// <summary>
        /// Get the pool name for this projectile.
        /// </summary>
        protected virtual string GetPoolName()
        {
            if (!string.IsNullOrEmpty(poolName))
                return poolName;

            if (projectilePrefab != null)
                return projectilePrefab.name;

            return "UnknownProjectile";
        }

        /// <summary>
        /// Spawns a projectile from the pool and launches it.
        /// Override to customize projectile configuration.
        /// </summary>
        protected virtual BaseProjectile SpawnProjectile(GameObject user, Vector3 origin, Vector3 direction)
        {
            // Validate projectile prefab
            if (projectilePrefab == null)
            {
                Debug.LogError($"ProjectileAbility '{name}': No projectile prefab assigned!");
                return null;
            }

            // Ensure pool manager exists
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError($"ProjectileAbility '{name}': ObjectPoolManager not found! Cannot spawn projectile.");
                return null;
            }

            string poolName = GetPoolName();

            // Create pool if it doesn't exist
            if (!ObjectPoolManager.Instance.HasPool(poolName))
            {
                ObjectPoolManager.Instance.CreatePoolRuntime(
                    projectilePrefab.gameObject,
                    preloadCount: 10,
                    maxSize: 50,
                    allowExpansion: true
                );
            }

            // Get projectile from pool
            GameObject projectileObj = ObjectPoolManager.Instance.Get(poolName, origin, Quaternion.LookRotation(direction));
            if (projectileObj == null)
            {
                Debug.LogError($"ProjectileAbility '{name}': Failed to get projectile from pool '{poolName}'!");
                return null;
            }

            BaseProjectile projectile = projectileObj.GetComponent<BaseProjectile>();
            if (projectile == null)
            {
                Debug.LogError($"ProjectileAbility '{name}': Projectile '{projectileObj.name}' missing BaseProjectile component!");
                ObjectPoolManager.ReturnToPool(projectileObj);
                return null;
            }

            // Launch the projectile
            projectile.Launch(origin, direction, projectileSpeed, projectileDamage, user);

            return projectile;
        }

        /// <summary>
        /// Override in subclasses to implement ability-specific logic when used.
        /// This is called when the ability is triggered, and should spawn the projectile.
        /// </summary>
        /// <param name="user">The GameObject using this ability</param>
        /// <param name="targetPosition">Target position (can be used for aim direction)</param>
        /// <returns>True if the ability was used successfully</returns>
        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            // Calculate firing origin and direction
            Vector3 origin = user.transform.position + Vector3.up * 1.5f; // Shoot from character height
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;

            // Spawn VFX at user when firing
            if (projectileVFX != null)
            {
                SpawnVFX(projectileVFX, origin, Quaternion.LookRotation(direction));
            }

            // Spawn and launch projectile
            BaseProjectile projectile = SpawnProjectile(user, origin, direction);

            return projectile != null;
        }
    }
}
