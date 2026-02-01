using System;
using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Effects;
using Element;
using Interfaces;

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

        [Header("Speed Modifier - Targets")]
        [Tooltip("Can this ability apply speed modifier to targets?")]
        [SerializeField] protected bool canModifySpeed = false;

        [Tooltip("Speed multiplier (0.5 = 50% speed/slow, 1.5 = 150% speed/boost)")]
        [SerializeField] protected float speedMultiplier = 0.5f;

        [Tooltip("How long the speed effect lasts in seconds")]
        [SerializeField] protected float speedDuration = 2f;

        [Header("Speed Modifier - Self")]
        [Tooltip("Apply speed modifier to self (user) when ability is used")]
        [SerializeField] protected bool applySpeedToSelf = false;

        [Tooltip("Speed multiplier for self (0.5 = slow, 1.5 = speed boost)")]
        [SerializeField] protected float selfSpeedMultiplier = 1.5f;

        [Tooltip("How long the self speed effect lasts")]
        [SerializeField] protected float selfSpeedDuration = 2f;

        [Header("Damage Over Time - Targets")]
        [Tooltip("Can this ability apply DoT to targets?")]
        [SerializeField] protected bool canDoT = false;

        [Tooltip("Damage dealt per tick")]
        [SerializeField] protected float dotDamagePerTick = 5f;

        [Tooltip("Time between damage ticks in seconds")]
        [SerializeField] protected float dotTickInterval = 0.5f;

        [Tooltip("Total duration of the DoT effect in seconds")]
        [SerializeField] protected float dotDuration = 4f;

        [Header("Damage Over Time - Self")]
        [Tooltip("Apply DoT to self (user) when ability is used")]
        [SerializeField] protected bool applyDoTToSelf = false;

        [Tooltip("Damage dealt per tick to self")]
        [SerializeField] protected float selfDotDamagePerTick = 2f;

        [Tooltip("Time between self damage ticks in seconds")]
        [SerializeField] protected float selfDotTickInterval = 0.5f;

        [Tooltip("Total duration of the self DoT effect in seconds")]
        [SerializeField] protected float selfDotDuration = 2f;

        protected float lastUseTime = float.NegativeInfinity;
        protected GameObject lastUser;

        protected virtual void Awake()
        {
            // Reset cooldown on awake to ensure ability starts ready
            // This fixes issues where prefab references retain lastUseTime from previous sessions
            lastUseTime = float.NegativeInfinity;
        }

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

            lastUser = user;
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
        /// Applies speed modifier to a single target if this ability can modify speed.
        /// Gets or adds SpeedEffect component and applies the modifier.
        /// </summary>
        protected virtual void ApplySpeedModifier(GameObject target)
        {
            if (!canModifySpeed || target == null)
                return;

            SpeedEffect speedEffect = target.GetComponent<SpeedEffect>();
            if (speedEffect == null)
                speedEffect = target.AddComponent<SpeedEffect>();

            speedEffect.ApplySpeedModifier(speedMultiplier, speedDuration);
        }

        /// <summary>
        /// Applies speed modifier to multiple targets (useful for AOE abilities).
        /// Filters out null colliders and applies modifier to each valid target.
        /// </summary>
        protected virtual void ApplySpeedModifierToColliders(Collider[] colliders)
        {
            if (!canModifySpeed || colliders == null || colliders.Length == 0)
                return;

            foreach (Collider collider in colliders)
            {
                if (collider != null && collider.gameObject != null)
                {
                    ApplySpeedModifier(collider.gameObject);
                }
            }
        }

        /// <summary>
        /// Applies speed modifier to GameObjects (useful when you already have GameObject references).
        /// </summary>
        protected virtual void ApplySpeedModifierToTargets(GameObject[] targets)
        {
            if (!canModifySpeed || targets == null || targets.Length == 0)
                return;

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    ApplySpeedModifier(target);
                }
            }
        }

        /// <summary>
        /// Applies speed modifier to the user (self) if applySpeedToSelf is enabled.
        /// Uses selfSpeedMultiplier and selfSpeedDuration for configuration.
        /// </summary>
        protected virtual void ApplySpeedModifierToSelf()
        {
            if (!applySpeedToSelf || lastUser == null)
                return;

            SpeedEffect speedEffect = lastUser.GetComponent<SpeedEffect>();
            if (speedEffect == null)
                speedEffect = lastUser.AddComponent<SpeedEffect>();

            speedEffect.ApplySpeedModifier(selfSpeedMultiplier, selfSpeedDuration);
        }

        /// <summary>
        /// Applies damage over time effect to a single target if this ability can DoT.
        /// Gets or adds DoTEffect component and applies the DoT.
        /// </summary>
        protected virtual void ApplyDoT(GameObject target)
        {
            if (!canDoT || target == null)
                return;

            DoTEffect dotEffect = target.GetComponent<DoTEffect>();
            if (dotEffect == null)
                dotEffect = target.AddComponent<DoTEffect>();

            dotEffect.ApplyDoT(dotDamagePerTick, dotTickInterval, dotDuration, lastUser);
        }

        /// <summary>
        /// Applies DoT effect to multiple targets (useful for AOE abilities).
        /// Filters out null colliders and applies DoT to each valid target.
        /// </summary>
        protected virtual void ApplyDoTToColliders(Collider[] colliders)
        {
            if (!canDoT || colliders == null || colliders.Length == 0)
                return;

            foreach (Collider collider in colliders)
            {
                if (collider != null && collider.gameObject != null)
                {
                    ApplyDoT(collider.gameObject);
                }
            }
        }

        /// <summary>
        /// Applies DoT effect to GameObjects (useful when you already have GameObject references).
        /// </summary>
        protected virtual void ApplyDoTToTargets(GameObject[] targets)
        {
            if (!canDoT || targets == null || targets.Length == 0)
                return;

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    ApplyDoT(target);
                }
            }
        }

        /// <summary>
        /// Applies DoT to the user (self) if applyDoTToSelf is enabled.
        /// Uses selfDotDamagePerTick, selfDotTickInterval, and selfDotDuration for configuration.
        /// Useful for abilities with a health cost or self-damage mechanic.
        /// </summary>
        protected virtual void ApplyDoTToSelf()
        {
            if (!applyDoTToSelf || lastUser == null)
                return;

            DoTEffect dotEffect = lastUser.GetComponent<DoTEffect>();
            if (dotEffect == null)
                dotEffect = lastUser.AddComponent<DoTEffect>();

            dotEffect.ApplyDoT(selfDotDamagePerTick, selfDotTickInterval, selfDotDuration, lastUser);
        }

        /// <summary>
        /// Convenience method to apply all self-effects that are enabled.
        /// Call this in DoUse to apply speed/DoT to self based on inspector settings.
        /// </summary>
        protected virtual void ApplyEffectsToSelf()
        {
            ApplySpeedModifierToSelf();
            ApplyDoTToSelf();
        }
    }
}
