using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Effects;
using Interfaces;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Lightning bolt projectile that can chain to nearby enemies on hit.
    /// Used by LightningBoltAbility.
    /// </summary>
    public class LightningBoltProjectile : BaseProjectile
    {
        [Header("Lightning Effects")]
        [SerializeField] protected bool destroyOnHit = true;

        [Header("Chain Lightning")]
        [Tooltip("Can this lightning chain to nearby enemies?")]
        [SerializeField] protected bool canChain = true;
        [Tooltip("Number of times the lightning can chain")]
        [SerializeField] protected int maxChains = 2;
        [Tooltip("Range to search for chain targets")]
        [SerializeField] protected float chainRange = 5f;
        [Tooltip("Damage multiplier per chain (0.7 = 70% of previous damage)")]
        [SerializeField] protected float chainDamageMultiplier = 0.7f;

        [Header("Stun Effect")]
        [Tooltip("Apply speed reduction (stun) on hit?")]
        [SerializeField] protected bool applyStun = true;
        [Tooltip("Speed multiplier during stun (0 = full stun, 0.3 = heavy slow)")]
        [SerializeField] protected float stunSpeedMultiplier = 0.3f;
        [Tooltip("Duration of the stun effect")]
        [SerializeField] protected float stunDuration = 0.5f;

        [Header("Debug")]
        [SerializeField] protected bool showDebugChains = true;
        [SerializeField] protected float debugChainDuration = 0.5f;

        // Track chains to prevent hitting same target twice
        private System.Collections.Generic.HashSet<GameObject> hitTargets = new System.Collections.Generic.HashSet<GameObject>();
        private int chainsRemaining;

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            hitTargets.Clear();
            chainsRemaining = maxChains;
        }

        public override void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            if (!launched)
                return;

            GameObject hitObject = other.gameObject;

            // Skip if already hit this target
            if (hitTargets.Contains(hitObject))
                return;

            hitTargets.Add(hitObject);

            // Apply damage to direct hit target
            var damageable = other.GetComponent<IDamagable>();
            if (damageable != null && damageable.IsAlive && damage > 0f)
            {
                damageable.TakeDamage(damage, point, -direction, owner);

                // Apply stun effect
                if (applyStun)
                {
                    ApplyStunToTarget(hitObject);
                }

                // Try to chain to nearby enemies
                if (canChain && chainsRemaining > 0)
                {
                    ChainToNearbyTargets(point, hitObject);
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

            ReturnToPool();
        }

        /// <summary>
        /// Applies stun (speed reduction) to a target.
        /// </summary>
        protected virtual void ApplyStunToTarget(GameObject target)
        {
            if (!applyStun || target == null)
                return;

            SpeedEffect speedEffect = target.GetComponent<SpeedEffect>();
            if (speedEffect == null)
                speedEffect = target.AddComponent<SpeedEffect>();

            speedEffect.ApplySpeedModifier(stunSpeedMultiplier, stunDuration);
        }

        /// <summary>
        /// Chain lightning to nearby valid targets.
        /// </summary>
        protected virtual void ChainToNearbyTargets(Vector3 fromPoint, GameObject excludeTarget)
        {
            Collider[] hits = Physics.OverlapSphere(fromPoint, chainRange, hitMask);

            foreach (Collider col in hits)
            {
                if (col == null || col.gameObject == null)
                    continue;

                // Skip owner
                if (col.gameObject == owner)
                    continue;

                // Skip already hit targets
                if (hitTargets.Contains(col.gameObject))
                    continue;

                // Skip the exclude target (the one we just hit)
                if (col.gameObject == excludeTarget)
                    continue;

                // Check if it's damageable
                var damageable = col.GetComponent<IDamagable>();
                if (damageable == null || !damageable.IsAlive)
                    continue;

                // Found a valid chain target
                hitTargets.Add(col.gameObject);
                chainsRemaining--;

                // Calculate chain damage
                float chainDamage = damage * chainDamageMultiplier;
                Vector3 chainPoint = col.ClosestPoint(fromPoint);
                Vector3 chainDirection = (col.transform.position - fromPoint).normalized;

                // Apply chain damage
                damageable.TakeDamage(chainDamage, chainPoint, chainDirection, owner);

                // Apply stun to chain target
                if (applyStun)
                {
                    ApplyStunToTarget(col.gameObject);
                }

                // Debug: draw chain line
                if (showDebugChains)
                {
                    Debug.DrawLine(fromPoint, chainPoint, Color.yellow, debugChainDuration);
                }

                // Update damage for next chain
                damage = chainDamage;

                // Recursively chain if we have chains remaining
                if (chainsRemaining > 0)
                {
                    ChainToNearbyTargets(chainPoint, col.gameObject);
                }

                // Only chain to one target per iteration
                break;
            }
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
