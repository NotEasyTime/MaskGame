using UnityEngine;
using Dwayne.Interfaces;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice combat projectile: Ice Spear that flies through space and damages on impact.
    /// Can be launched by ProjectileWeapon or spawned directly.
    /// </summary>
    public class IceSpearAbility : ProjectileAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Ice Spear")]
        [SerializeField] float spearDamage = 25f;
        [SerializeField] float freezeDuration = 1.5f;
        [SerializeField] float slowMultiplier = 0.3f;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            // This is called on impact by the projectile system
            // targetPosition is the hit point
            // Apply ice-specific effects in an AOE around the impact

            float freezeRadius = 2f;
            Collider[] hits = Physics.OverlapSphere(targetPosition, freezeRadius, projectileHitMask);

            foreach (Collider col in hits)
            {
                // Skip the owner
                if (col.gameObject == projectileOwner)
                    continue;

                var damageable = col.GetComponent<IDamagable>();
                if (damageable != null && damageable.IsAlive)
                {
                    // Apply freeze/slow effect
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb != null && freezeDuration > 0f)
                    {
                        rb.linearVelocity *= slowMultiplier;
                        // Could add a StatusEffect component here for timed freeze
                    }
                }
            }

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
