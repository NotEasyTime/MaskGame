using UnityEngine;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Generic projectile ability with no special element type.
    /// Spawns a projectile from the pool when used.
    /// Assign a projectile prefab (GenericProjectile, IceProjectile, etc.) in the Inspector.
    /// </summary>
    public class GenericProjectileAbility : ProjectileAbility
    {
        public override Element.Element ElementType => Element.Element.Air;

        // All spawning logic is handled by ProjectileAbility base class
        // Just assign projectilePrefab, projectileSpeed, and projectileDamage in Inspector
    }
}
