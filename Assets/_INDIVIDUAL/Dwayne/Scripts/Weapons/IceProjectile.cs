using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Effects;
using Interfaces;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Ice projectile that applies speed modifier (slow) on hit.
    /// Used by IceSpearAbility.
    /// </summary>
    public class IceProjectile : BaseProjectile
    {
        [Header("Ice Effects")]
        [SerializeField] protected bool destroyOnHit = true;
        [SerializeField] protected float freezeRadius = 2f;
        [SerializeField] protected bool applySpeedModifier = true;
        [SerializeField] protected float speedMultiplier = 0.5f;
        [SerializeField] protected float speedDuration = 2f;

        public override void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            if (!launched)
                return;

            // Apply damage to direct hit target
            var damageable = other.GetComponent<IDamagable>();
            if (damageable != null && damageable.IsAlive && damage > 0f)
            {
                damageable.TakeDamage(damage, point, -direction, owner);
            }

            // Apply freeze effect in AOE
            if (applySpeedModifier && freezeRadius > 0f)
            {
                Collider[] hits = Physics.OverlapSphere(point, freezeRadius, hitMask);
                foreach (Collider col in hits)
                {
                    if (col != null && col.gameObject != null)
                    {
                        ApplySpeedModifierToTarget(col.gameObject);
                    }
                }
            }

            // Return to pool on hit
            if (destroyOnHit)
            {
                ReturnToPool();
            }
        }

        public override void OnExpire()
        {
            if (!launched)
                return;

            // Return to pool on expire
            ReturnToPool();
        }

        /// <summary>
        /// Applies speed modifier to a target.
        /// </summary>
        protected virtual void ApplySpeedModifierToTarget(GameObject target)
        {
            if (!applySpeedModifier || target == null)
                return;

            SpeedEffect speedEffect = target.GetComponent<SpeedEffect>();
            if (speedEffect == null)
                speedEffect = target.AddComponent<SpeedEffect>();

            speedEffect.ApplySpeedModifier(speedMultiplier, speedDuration);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            TryHit(other, transform.position, -direction);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];
                TryHit(collision.collider, contact.point, contact.normal);
            }
        }
    }
}
