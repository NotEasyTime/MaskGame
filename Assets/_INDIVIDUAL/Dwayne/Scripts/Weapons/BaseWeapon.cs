using UnityEngine;
using Dwayne.Abilities;
using Dwayne.Interfaces;
using Pool;
using Interfaces;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Fire mode for built-in weapon logic when no ability is assigned.
    /// </summary>
    public enum FireMode
    {
        Hitscan,    // Instant raycast
        Projectile  // Spawns projectile from pool
    }

    /// <summary>
    /// Base class for weapons implementing IWeaponInterface.
    /// Uses a cooldown-based fire timer (no magazine/reload); subclasses implement Fire for hitscan or projectile.
    /// Provides built-in fallback fire logic (hitscan or projectile) when abilities are not assigned.
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

        [Header("Fallback Fire Mode")]
        [Tooltip("Fire type when no ability is assigned (Hitscan = instant raycast, Projectile = spawns projectile)")]
        [SerializeField] protected FireMode fallbackFireMode = FireMode.Hitscan;

        [Header("Fallback Hitscan")]
        [Tooltip("Layer mask for built-in hitscan (when no ability is assigned and mode is Hitscan)")]
        [SerializeField] protected LayerMask fallbackHitMask = ~0;

        [Header("Fallback Projectile")]
        [Tooltip("Projectile prefab to spawn (when no ability is assigned and mode is Projectile)")]
        [SerializeField] protected BaseProjectile fallbackProjectilePrefab;
        [Tooltip("Projectile speed (when no ability is assigned and mode is Projectile)")]
        [SerializeField] protected float fallbackProjectileSpeed = 20f;
        [Tooltip("Pool name for projectile (optional, defaults to prefab name)")]
        [SerializeField] protected string fallbackPoolName;

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

        /// <summary>The fire ability assigned to this weapon.</summary>
        public virtual BaseAbility FireAbility => fireAbility;

        /// <summary>The alt-fire ability assigned to this weapon.</summary>
        public virtual BaseAbility AltFireAbility => altFireAbility;

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

            // Instantiate ability prefabs so we don't modify the original assets
            // This fixes the "starts on cooldown" bug when abilities are prefab references
            if (fireAbility != null && !IsInstancedAbility(fireAbility))
            {
                fireAbility = InstantiateAbility(fireAbility);
            }
            if (altFireAbility != null && !IsInstancedAbility(altFireAbility))
            {
                altFireAbility = InstantiateAbility(altFireAbility);
            }
        }

        /// <summary>
        /// Checks if an ability is already an instance (not a prefab reference).
        /// </summary>
        private bool IsInstancedAbility(BaseAbility ability)
        {
            // If the ability's gameObject is in a scene (has a scene), it's an instance
            return ability.gameObject.scene.IsValid();
        }

        /// <summary>
        /// Instantiates an ability prefab as a child of this weapon.
        /// </summary>
        private BaseAbility InstantiateAbility(BaseAbility abilityPrefab)
        {
            GameObject instance = Instantiate(abilityPrefab.gameObject, transform);
            instance.name = abilityPrefab.name + " (Instance)";
            return instance.GetComponent<BaseAbility>();
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

        /// <summary>
        /// Built-in fire logic used when no fireAbility is assigned.
        /// Switches between hitscan raycast and projectile spawning based on fallbackFireMode.
        /// Subclasses can call this from DoFire() when fireAbility is null.
        /// </summary>
        protected virtual bool DoFallbackFire(Vector3 origin, Vector3 direction, float charge = 1f)
        {
            // Switch based on fire mode
            switch (fallbackFireMode)
            {
                case FireMode.Hitscan:
                    return DoFallbackHitscan(origin, direction, charge);

                case FireMode.Projectile:
                    return DoFallbackProjectile(origin, direction, charge);

                default:
                    Debug.LogWarning($"BaseWeapon '{name}': Unknown fallback fire mode '{fallbackFireMode}'!");
                    return false;
            }
        }

        /// <summary>
        /// Built-in hitscan raycast logic.
        /// Uses weapon's damage, range, and fallbackHitMask settings.
        /// </summary>
        protected virtual bool DoFallbackHitscan(Vector3 origin, Vector3 direction, float charge)
        {
            // Calculate damage with charge multiplier
            float finalDamage = damage * Mathf.Lerp(1f, fullChargeDamageMultiplier, charge);

            // Single raycast
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, fallbackHitMask))
            {
                // Apply damage to damageable objects
                var damageable = hit.collider.GetComponent<IDamagable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(finalDamage, hit.point, -direction, Owner);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Built-in projectile spawning logic.
        /// Spawns projectile from ObjectPoolManager using weapon's damage and fallbackProjectileSpeed.
        /// </summary>
        protected virtual bool DoFallbackProjectile(Vector3 origin, Vector3 direction, float charge)
        {
            // Validate projectile prefab
            if (fallbackProjectilePrefab == null)
            {
                Debug.LogError($"BaseWeapon '{name}': Fallback fire mode is Projectile but no fallbackProjectilePrefab assigned!");
                return false;
            }

            // Ensure pool manager exists
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError($"BaseWeapon '{name}': ObjectPoolManager not found! Cannot spawn projectile.");
                return false;
            }

            // Get pool name (use custom name or prefab name)
            string poolNameToUse = !string.IsNullOrEmpty(fallbackPoolName) ? fallbackPoolName : fallbackProjectilePrefab.name;

            // Create pool if it doesn't exist
            if (!ObjectPoolManager.Instance.HasPool(poolNameToUse))
            {
                ObjectPoolManager.Instance.CreatePoolRuntime(
                    fallbackProjectilePrefab.gameObject,
                    preloadCount: 10,
                    maxSize: 50,
                    allowExpansion: true
                );
            }

            // Get projectile from pool
            GameObject projectileObj = ObjectPoolManager.Instance.Get(poolNameToUse, origin, Quaternion.LookRotation(direction));
            if (projectileObj == null)
            {
                Debug.LogError($"BaseWeapon '{name}': Failed to get projectile from pool '{poolNameToUse}'!");
                return false;
            }

            BaseProjectile projectile = projectileObj.GetComponent<BaseProjectile>();
            if (projectile == null)
            {
                Debug.LogError($"BaseWeapon '{name}': Projectile '{projectileObj.name}' missing BaseProjectile component!");
                ObjectPoolManager.ReturnToPool(projectileObj);
                return false;
            }

            // Calculate damage with charge multiplier
            float finalDamage = damage * Mathf.Lerp(1f, fullChargeDamageMultiplier, charge);

            // Launch the projectile
            projectile.Launch(origin, direction, fallbackProjectileSpeed, finalDamage, Owner);

            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Fire")]
        private void DebugFire()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            Debug.Log($"[{name}] DebugFire: origin={origin}, direction={direction}");
            Fire(origin, direction);
        }

        [ContextMenu("Fire Ability")]
        private void DebugFireAbility()
        {
            Vector3 target = transform.position + transform.forward * range;
            Debug.Log($"[{name}] DebugFireAbility: target={target}, range={range}");
            TryUseFireAbility(target);
        }
#endif
    }
}
