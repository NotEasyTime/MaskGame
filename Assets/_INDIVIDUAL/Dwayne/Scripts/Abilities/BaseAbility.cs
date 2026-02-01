using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Effects;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Base class for abilities implementing IAbility.
    /// Handles cooldown, VFX, and Use/Cancel; subclasses implement the actual effect in DoUse.
    /// </summary>
    public abstract class BaseAbility : MonoBehaviour, IAbility
    {
        [Header("Element")]
        [SerializeField] protected Element.Element elementType = Element.Element.Air;

        [Header("Cooldown")]
        [SerializeField] protected float cooldownDuration = 1f;

        [Header("VFX")]
        [Tooltip("VFX spawned when ability is activated (at user position)")]
        [SerializeField] protected GameObject spawnVFX;

        [Tooltip("VFX for projectiles (instantiated and moves with projectile)")]
        [SerializeField] protected GameObject projectileVFX;

        [Tooltip("VFX spawned on impact/hit")]
        [SerializeField] protected GameObject impactVFX;

        [Tooltip("VFX for trails/paths (instantiated along movement path)")]
        [SerializeField] protected GameObject trailVFX;

        [Tooltip("How long VFX objects live before auto-destroying (0 = use particle lifetime)")]
        [SerializeField] protected float vfxLifetime = 2f;

        [Header("Slow Effect")]
        [Tooltip("Can this ability apply slow to targets?")]
        [SerializeField] protected bool canSlow = false;

        [Tooltip("Movement speed multiplier when slowed (0.5 = 50% speed, 0 = frozen)")]
        [SerializeField] [Range(0f, 1f)] protected float slowMultiplier = 0.5f;

        [Tooltip("How long the slow effect lasts in seconds")]
        [SerializeField] protected float slowDuration = 2f;

        protected float lastUseTime = float.NegativeInfinity;

        public virtual Element.Element ElementType => elementType;
        public virtual bool CanUse => CooldownRemaining <= 0f;
        public virtual float CooldownRemaining => Mathf.Max(0f, lastUseTime + cooldownDuration - Time.time);
        public virtual float CooldownDuration => cooldownDuration;

        /// <summary>
        /// Perform the ability effect. Called by Use when CanUse. Return true if the ability was successfully used.
        /// </summary>
        protected abstract bool DoUse(GameObject user, Vector3 targetPosition);

        public virtual bool Use(GameObject user, Vector3 targetPosition = default)
        {
            if (!CanUse)
                return false;

            bool used = DoUse(user, targetPosition);
            if (used)
                lastUseTime = Time.time;
            return used;
        }

        public virtual void Cancel()
        {
            // Override in channeled abilities to interrupt.
        }

        /// <summary>
        /// Spawns spawn VFX at the user's position.
        /// </summary>
        protected virtual void SpawnVFXAtUser(GameObject user)
        {
            if (spawnVFX != null && user != null)
                SpawnVFX(spawnVFX, user.transform.position, user.transform.rotation);
        }

        /// <summary>
        /// Spawns impact VFX at a hit point.
        /// </summary>
        protected virtual void SpawnImpactVFX(Vector3 position, Vector3 normal)
        {
            if (impactVFX != null)
            {
                Quaternion rotation = normal != Vector3.zero
                    ? Quaternion.LookRotation(normal)
                    : Quaternion.identity;
                SpawnVFX(impactVFX, position, rotation);
            }
        }

        /// <summary>
        /// Spawns trail VFX at a position.
        /// </summary>
        protected virtual void SpawnTrailVFX(Vector3 position, Quaternion rotation)
        {
            if (trailVFX != null)
                SpawnVFX(trailVFX, position, rotation);
        }

        /// <summary>
        /// Generic VFX spawner. Returns the spawned GameObject.
        /// </summary>
        protected virtual GameObject SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
        {
            if (vfxPrefab == null)
                return null;

            GameObject vfx = Instantiate(vfxPrefab, position, rotation);

            if (vfxLifetime > 0f)
                Destroy(vfx, vfxLifetime);

            return vfx;
        }

        /// <summary>
        /// Applies slow effect to a single target if this ability can slow.
        /// Gets or adds SlowEffect component and applies the slow.
        /// </summary>
        protected virtual void ApplySlow(GameObject target)
        {
            if (!canSlow || target == null)
                return;

            SlowEffect slowEffect = target.GetComponent<SlowEffect>();
            if (slowEffect == null)
                slowEffect = target.AddComponent<SlowEffect>();

            slowEffect.ApplySlow(slowMultiplier, slowDuration);
        }

        /// <summary>
        /// Applies slow effect to multiple targets (useful for AOE abilities).
        /// Filters out null colliders and applies slow to each valid target.
        /// </summary>
        protected virtual void ApplySlowToColliders(Collider[] colliders)
        {
            if (!canSlow || colliders == null || colliders.Length == 0)
                return;

            foreach (Collider collider in colliders)
            {
                if (collider != null && collider.gameObject != null)
                {
                    ApplySlow(collider.gameObject);
                }
            }
        }

        /// <summary>
        /// Applies slow effect to GameObjects (useful when you already have GameObject references).
        /// </summary>
        protected virtual void ApplySlowToTargets(GameObject[] targets)
        {
            if (!canSlow || targets == null || targets.Length == 0)
                return;

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    ApplySlow(target);
                }
            }
        }
    }
}
