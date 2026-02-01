using UnityEngine;
using Dwayne.Interfaces;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice combat projectile: Ice Spear that flies through space and damages on impact.
    /// Can be launched by ProjectileWeapon or spawned directly.
    /// Uses the SlowEffect system from BaseAbility to apply slow to targets on impact.
    /// </summary>
    public class IceSpearAbility : ProjectileAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Ice Spear")]
        [SerializeField] float spearDamage = 25f;
        [SerializeField] float freezeRadius = 2f;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            // This is called on impact by the projectile system
            // targetPosition is the hit point
            // Apply ice-specific effects in an AOE around the impact

            Collider[] hits = Physics.OverlapSphere(targetPosition, freezeRadius, projectileHitMask);

            // Apply slow effect to all targets in AOE using BaseAbility's slow system
            ApplySlowToColliders(hits);

            return true;
        }

        public override void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            // Override damage to use spear-specific damage
            damage = spearDamage;

            // Call base to handle VFX and ability trigger
            base.OnHit(other, point, normal);
        }
    }
}
