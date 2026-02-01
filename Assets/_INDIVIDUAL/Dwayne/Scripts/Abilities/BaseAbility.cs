using System;
using System.Reflection;
using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Effects;
using Element;
using Interfaces;
using Managers;

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

        [Tooltip("Scale impact VFX by (AOE radius * this). e.g. 0.5 = radius 10 → scale 5. Set 0 to use prefab scale only.")]
        [SerializeField] protected float impactVFXScaleFactor = 0.5f;

        [Header("Audio")]
        [Tooltip("Sound when ability is cast/fired")]
        [SerializeField] protected AudioClip castSound;
        [Tooltip("Sound on impact/hit")]
        [SerializeField] protected AudioClip impactSound;
        [Tooltip("Optional: sound when ability completes (e.g. channel end)")]
        [SerializeField] protected AudioClip completionSound;

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
            {
                lastUseTime = Time.time;
                PlayCastSound();
            }
            return used;
        }

        /// <summary>
        /// Runs the ability effect without cooldown check or setting lastUseTime.
        /// Used by weapons that treat magazine size as "charges" and apply this ability's CooldownDuration only when refilling (mag empty).
        /// </summary>
        public virtual bool UseFromWeapon(GameObject user, Vector3 targetPosition = default)
        {
            lastUser = user;
            return DoUse(user, targetPosition);
        }

        /// <summary>True when this ability is currently channeling (e.g. ice breath). Used to cancel on button release or when input is lost.</summary>
        public virtual bool IsChanneled => false;

        public virtual void Cancel()
        {
            // Override in channeled abilities to interrupt.
        }

        /// <summary>
        /// Spawns spawn VFX at the user's position.
        /// </summary>
        protected virtual void SpawnVFXAtUser(GameObject user)
        {
            if (spawnVFX == null || user == null)
                return;
            GameObject vfx = SpawnVFX(spawnVFX, user.transform.position, user.transform.rotation);
            if (vfx != null)
                TryPlayPixPlaysVfx(vfx, user.transform.position, user.transform.forward, 1f, vfxLifetime > 0f ? vfxLifetime : 2f);
        }

        protected virtual void PlayCastSound()
        {
            if (castSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(castSound);
        }

        protected virtual void PlayImpactSound()
        {
            if (impactSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(impactSound);
        }

        protected virtual void PlayCompletionSound()
        {
            if (completionSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(completionSound);
        }

        /// <summary>
        /// Spawns impact VFX at a hit point.
        /// </summary>
        /// <param name="position">World position (impact point).</param>
        /// <param name="normal">Surface normal for rotation (e.g. hit.normal).</param>
        /// <param name="aoRadius">AOE radius of the ability (e.g. explosion radius). When &gt; 0 and impactVFXScaleFactor &gt; 0, scales the VFX so it matches the AOE size. Also passed to PixPlays LocationVfx if present.</param>
        protected virtual void SpawnImpactVFX(Vector3 position, Vector3 normal, float aoRadius = 0f)
        {
            PlayImpactSound();
            if (impactVFX == null)
                return;

            Quaternion rotation = normal != Vector3.zero
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity;
            float scale = (aoRadius > 0f && impactVFXScaleFactor > 0f)
                ? aoRadius * impactVFXScaleFactor
                : 1f;
            GameObject vfx = SpawnVFX(impactVFX, position, rotation, scale);

            // If impact prefab uses PixPlays BaseVfx, call Play so the effect runs (use radius 1 when no AOE)
            if (vfx != null)
                TryPlayPixPlaysVfx(vfx, position, normal, aoRadius > 0f ? aoRadius : 1f, vfxLifetime > 0f ? vfxLifetime : 2f);
        }

        /// <summary>
        /// If the spawned VFX has PixPlays BaseVfx, call Play(VfxData) so the effect runs. Direction is used as target offset (position + direction * 2).
        /// </summary>
        protected static void TryPlayPixPlaysVfx(GameObject vfx, Vector3 position, Vector3 directionOrNormal, float radius, float duration)
        {
            try
            {
                Type baseVfxType = Type.GetType("PixPlays.ElementalVFX.BaseVfx, Assembly-CSharp");
                Type vfxDataType = Type.GetType("PixPlays.ElementalVFX.VfxData, Assembly-CSharp");
                if (baseVfxType == null || vfxDataType == null)
                    return;

                Component comp = vfx.GetComponent(baseVfxType);
                if (comp == null)
                    return;

                duration = Mathf.Max(0.5f, duration);
                ConstructorInfo ctor = vfxDataType.GetConstructor(new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(float) });
                if (ctor == null)
                    return;

                Vector3 target = position + (directionOrNormal.sqrMagnitude > 0.01f ? directionOrNormal : Vector3.forward) * 2f;
                object vfxData = ctor.Invoke(new object[] { position, target, duration, radius });
                MethodInfo play = baseVfxType.GetMethod("Play", new[] { vfxDataType });
                if (play != null)
                {
                    play.Invoke(comp, new[] { vfxData });
                    // PixPlays LocationVfx scales its own child by radius; keep root at 1 to avoid double scale
                    vfx.transform.localScale = Vector3.one;
                }
            }
            catch
            {
                // PixPlays not present or different assembly; root scaling already applied
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
        /// <param name="scale">Scale of the spawned VFX (1 = prefab default). Used for AOE impact VFX.</param>
        protected virtual GameObject SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float scale = 1f)
        {
            if (vfxPrefab == null)
                return null;

            GameObject vfx = Instantiate(vfxPrefab, position, rotation);
            vfx.SetActive(true); // Many VFX prefabs (e.g. PixPlays FireballCast) have root inactive; ensure they play
            if (scale != 1f)
                vfx.transform.localScale = Vector3.one * scale;

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
