using UnityEngine;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice spear ability that spawns an IceProjectile.
    /// The IceProjectile handles slow effects and AOE freeze on impact.
    /// </summary>
    public class IceSpearAbility : ProjectileAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        // ProjectileAbility base class handles spawning the projectile
        // Make sure to assign an IceProjectile prefab to the projectilePrefab field in Inspector
    }
}
