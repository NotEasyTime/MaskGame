using UnityEngine;
using Dwayne.Interfaces;
using Interfaces;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Generic poolable projectile with configurable behavior.
    /// Handles basic damage on hit and automatic pooling.
    /// </summary>
    public class GenericProjectile : BaseProjectile
    {
        [Header("Behavior")]
        [SerializeField] protected bool destroyOnHit = true;
        [SerializeField] protected bool applyDamage = true;

        public override void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            if (!launched)
                return;

            // Apply damage to damageable targets
            if (applyDamage)
            {
                var damageable = other.GetComponent<IDamagable>();
                if (damageable != null && damageable.IsAlive && damage > 0f)
                {
                    damageable.TakeDamage(damage, point, -direction, owner);
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
