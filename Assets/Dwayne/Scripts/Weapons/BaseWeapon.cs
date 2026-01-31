using UnityEngine;
using Dwayne.Abilities;
using Dwayne.Interfaces;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Base class for weapons implementing IWeaponInterface.
    /// Uses a cooldown-based fire timer (no magazine/reload); subclasses implement Fire for hitscan or projectile.
    /// </summary>
    public abstract class BaseWeapon : MonoBehaviour, IWeaponInterface
    {
        [Header("Cooldown")]
        [SerializeField] [Min(0f)] protected float fireCooldown = 0.5f; // seconds before next shot after firing (0 = no cooldown)
        [SerializeField] [Min(1)] protected int magazineSize = 30; // max shots before refill
        [SerializeField] [Min(0f)] protected float refillCooldown = 1.5f; // seconds to wait when empty before shots refill

        [Header("Damage & Range")]
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float range = 100f;

        [Header("Spread")]
        [SerializeField] [Min(1)] protected int pelletsPerShot = 1;
        [SerializeField] [Min(0f)] [Tooltip("Cone half-angle in degrees (0 = no spread).")]
        protected float spreadAngle = 0f;

        [Header("Charging")]
        [SerializeField] protected bool useCharging;
        [SerializeField] [Min(0.01f)] protected float maxChargeTime = 1f;
        [SerializeField] [Range(0f, 1f)] protected float minChargeToFire = 0f; // minimum charge (0..1) to release a shot
        [SerializeField] [Min(1f)] protected float fullChargeDamageMultiplier = 1.5f; // damage at full charge (1 = no scaling)

        [Header("Abilities")]
        [SerializeField] [Tooltip("Optional ability triggered by fire-ability input (e.g. alt-fire).")]
        protected BaseAbility fireAbility;
        [SerializeField] [Tooltip("Optional secondary ability (e.g. weapon skill).")]
        protected BaseAbility altFireAbility;
        [SerializeField] [Tooltip("Owner passed to abilities (user). If unset, uses transform.root or this GameObject.")]
        protected GameObject owner;

        protected float nextFireTime;
        protected float nextRefillTime;
        protected int currentShots;
        protected float chargeStartTime = -1f;
        protected Vector3 chargeOrigin;
        protected Vector3 chargeDirection;

        public virtual bool CanFire =>
            currentShots > 0 && (fireCooldown <= 0f || Time.time >= nextFireTime);

        /// <summary>True when the weapon is currently being charged.</summary>
        public virtual bool IsCharging => useCharging && chargeStartTime >= 0f;

        /// <summary>Current charge progress 0..1. Only valid while IsCharging.</summary>
        public virtual float ChargeProgress =>
            !IsCharging ? 0f : Mathf.Clamp01((Time.time - chargeStartTime) / maxChargeTime);

        /// <summary>Damage multiplier for the current charge level (1 at no charge, fullChargeDamageMultiplier at full).</summary>
        public virtual float CurrentChargeDamageMultiplier => Mathf.Lerp(1f, fullChargeDamageMultiplier, ChargeProgress);

        /// <summary>Seconds until the weapon can fire again. 0 when ready.</summary>
        public virtual float CooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);

        /// <summary>Seconds until shots refill when empty. 0 when not empty or refill complete.</summary>
        public virtual float RefillCooldownRemaining => currentShots > 0 ? 0f : Mathf.Max(0f, nextRefillTime - Time.time);

        public virtual int CurrentAmmo => currentShots;
        public virtual int MagazineSize => magazineSize;
        public virtual float Damage => damage;
        public virtual float Range => range;
        public virtual bool IsReloading => currentShots == 0 && Time.time < nextRefillTime;

        /// <summary>Owner passed to abilities (user). Set explicitly or inferred from hierarchy.</summary>
        public virtual GameObject Owner => owner != null ? owner : (transform.root != null ? transform.root.gameObject : gameObject);

        /// <summary>Set the wielder/owner for ability calls. Call from character when equipping.</summary>
        public virtual void SetOwner(GameObject wielder) => owner = wielder;

        /// <summary>Try to use the fire ability (e.g. alt-fire). Returns true if the ability was used.</summary>
        public virtual bool TryUseFireAbility(Vector3 targetPosition = default) => TryUseAbility(fireAbility, targetPosition);

        /// <summary>Try to use the alt-fire ability. Returns true if the ability was used.</summary>
        public virtual bool TryUseAltFireAbility(Vector3 targetPosition = default) => TryUseAbility(altFireAbility, targetPosition);

        protected virtual bool TryUseAbility(BaseAbility ability, Vector3 targetPosition)
        {
            if (ability == null || !ability.CanUse)
                return false;
            return ability.Use(Owner, targetPosition);
        }

        protected virtual void Awake()
        {
            nextFireTime = 0f;
            currentShots = magazineSize;
        }

        protected virtual void Update()
        {
            if (currentShots == 0 && (refillCooldown <= 0f || Time.time >= nextRefillTime))
                currentShots = magazineSize;
        }

        /// <summary>
        /// Perform the actual shot (hitscan raycast or spawn projectile). Called by Fire when CanFire.
        /// </summary>
        protected abstract bool DoFire(Vector3 origin, Vector3 direction);

        /// <summary>
        /// Override to use charge (e.g. scale damage). Default calls DoFire(origin, direction).
        /// </summary>
        /// <param name="charge">Charge amount 0..1 from ReleaseCharge.</param>
        protected virtual bool DoFire(Vector3 origin, Vector3 direction, float charge)
        {
            return DoFire(origin, direction);
        }

        /// <summary>
        /// Returns a direction with spread applied for the given pellet index. Override for custom spread patterns.
        /// </summary>
        protected virtual Vector3 GetSpreadDirection(Vector3 baseDirection, int pelletIndex)
        {
            if (spreadAngle <= 0f || pelletsPerShot <= 1)
                return baseDirection.normalized;

            float halfAngleRad = spreadAngle * 0.5f * Mathf.Deg2Rad;
            float theta = Random.Range(0f, 1f) * halfAngleRad;
            float phi = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 local = new Vector3(
                Mathf.Sin(theta) * Mathf.Cos(phi),
                Mathf.Sin(theta) * Mathf.Sin(phi),
                Mathf.Cos(theta));
            return (Quaternion.FromToRotation(Vector3.forward, baseDirection.normalized) * local).normalized;
        }

        public virtual bool Fire(Vector3 origin, Vector3 direction)
        {
            if (!CanFire)
                return false;

            float charge = useCharging ? 0f : 1f;
            bool fired = false;
            for (int i = 0; i < pelletsPerShot; i++)
            {
                Vector3 spreadDir = GetSpreadDirection(direction, i);
                if (DoFire(origin, spreadDir, charge))
                    fired = true;
            }
            if (fired)
            {
                currentShots--;
                if (fireCooldown > 0f)
                    nextFireTime = Time.time + fireCooldown;
                if (currentShots == 0 && refillCooldown > 0f)
                    nextRefillTime = Time.time + refillCooldown;
            }
            return fired;
        }

        /// <summary>
        /// Start charging a shot. Call ReleaseCharge to fire. No-op if useCharging is false or CanFire is false.
        /// </summary>
        public virtual void BeginCharge(Vector3 origin, Vector3 direction)
        {
            if (!useCharging || !CanFire)
                return;
            chargeStartTime = Time.time;
            chargeOrigin = origin;
            chargeDirection = direction.normalized;
        }

        /// <summary>
        /// Fire with current charge level. Returns true if a shot was released. Clears charge state.
        /// </summary>
        public virtual bool ReleaseCharge()
        {
            if (!IsCharging)
            {
                CancelCharge();
                return false;
            }

            float charge = ChargeProgress;
            if (charge < minChargeToFire)
            {
                CancelCharge();
                return false;
            }

            bool fired = false;
            for (int i = 0; i < pelletsPerShot; i++)
            {
                Vector3 spreadDir = GetSpreadDirection(chargeDirection, i);
                if (DoFire(chargeOrigin, spreadDir, charge))
                    fired = true;
            }
            chargeStartTime = -1f;
            if (fired)
            {
                currentShots--;
                if (fireCooldown > 0f)
                    nextFireTime = Time.time + fireCooldown;
                if (currentShots == 0 && refillCooldown > 0f)
                    nextRefillTime = Time.time + refillCooldown;
            }
            return fired;
        }

        /// <summary>
        /// Cancel the current charge without firing.
        /// </summary>
        public virtual void CancelCharge()
        {
            chargeStartTime = -1f;
        }

        /// <summary>No-op; refill is automatic after refillCooldown. Kept for IWeaponInterface.</summary>
        public virtual void Reload() { }

#if UNITY_EDITOR
        [ContextMenu("Fire")]
        private void DebugFire()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            Fire(origin, direction);
        }

        [ContextMenu("Fire Ability")]
        private void DebugFireAbility()
        {
            Vector3 target = transform.position + transform.forward * range;
            TryUseFireAbility(target);
        }
#endif
    }
}
