using UnityEngine;
using Dwayne.Interfaces;
using Dwayne.Weapons;
using Pool;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Lightning Bolt: Charged projectile ability.
    /// Hold to charge, release at 100% to fire a powerful lightning bolt projectile.
    /// The longer you charge, the more damage it deals (when partialRelease is enabled).
    /// </summary>
    public class LightningBoltAbility : ProjectileAbility
    {
        public override Element.Element ElementType => Element.Element.Air;

        [Header("Charge Settings")]
        [SerializeField] float chargeTime = 1.5f;
        [SerializeField] bool autoReleaseAtFullCharge = true;
        [SerializeField] bool partialRelease = false;
        [Tooltip("Minimum charge percent required to fire (0-1)")]
        [SerializeField] float minChargeToFire = 1f;

        [Header("Charged Damage Scaling")]
        [Tooltip("Damage multiplier at full charge (base damage * this value)")]
        [SerializeField] float fullChargeDamageMultiplier = 2f;
        [Tooltip("Speed multiplier at full charge")]
        [SerializeField] float fullChargeSpeedMultiplier = 1.5f;

        [Header("Spawn Position")]
        [Tooltip("How high above the target the bolt spawns")]
        [SerializeField] float spawnHeightAboveTarget = 20f;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;

        // Charging state
        private bool isCharging = false;
        private float chargeStartTime = 0f;
        private GameObject chargingUser = null;
        private Vector3 chargeTargetPosition;

        /// <summary>Current charge percentage (0 to 1).</summary>
        public float ChargePercent => isCharging ? Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime) : 0f;

        /// <summary>Is the ability currently charging?</summary>
        public bool IsCharging => isCharging;

        /// <summary>Is the ability fully charged?</summary>
        public bool IsFullyCharged => ChargePercent >= 1f;

        /// <summary>Can the ability be released at current charge level?</summary>
        public bool CanRelease => ChargePercent >= minChargeToFire;

        void Update()
        {
            if (!isCharging)
                return;

            // Debug: show charging progress
            if (showDebugTrace && chargingUser != null)
            {
                float progress = ChargePercent;
                Color chargeColor = Color.Lerp(Color.cyan, Color.yellow, progress);

                // Draw charging indicator - expanding lines from user
                Vector3 origin = chargingUser.transform.position + Vector3.up * 1.5f;
                float lineLength = 0.5f + progress * 1.5f;

                // Draw lightning-style jagged lines
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 endPoint = origin + direction * lineLength;

                    // Add some jitter for lightning effect
                    Vector3 midPoint = origin + direction * (lineLength * 0.5f);
                    midPoint += new Vector3(
                        Random.Range(-0.1f, 0.1f),
                        Random.Range(-0.1f, 0.1f),
                        Random.Range(-0.1f, 0.1f)
                    );

                    Debug.DrawLine(origin, midPoint, chargeColor, 0f);
                    Debug.DrawLine(midPoint, endPoint, chargeColor, 0f);
                }
            }

            // Auto-release at full charge
            if (autoReleaseAtFullCharge && IsFullyCharged)
            {
                ReleaseLightningBolt();
            }
        }

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            // If already charging, ignore
            if (isCharging)
                return false;

            // Start charging
            isCharging = true;
            chargeStartTime = Time.time;
            chargingUser = user;
            chargeTargetPosition = targetPosition;

            // Spawn charging VFX at user
            SpawnVFXAtUser(user);

            if (showDebugTrace)
                Debug.Log($"[LightningBoltAbility] Charging started by {user.name}");

            return true;
        }

        public override void Cancel()
        {
            if (!isCharging)
                return;

            // Check if we can release
            if (CanRelease)
            {
                ReleaseLightningBolt();
            }
            else
            {
                // Cancel without releasing
                if (showDebugTrace)
                    Debug.Log($"[LightningBoltAbility] Charge cancelled at {ChargePercent * 100:F0}% (need {minChargeToFire * 100:F0}% to fire)");

                ResetChargeState();
            }
        }

        /// <summary>
        /// Force release the lightning bolt at current charge level.
        /// Only fires if charge meets minimum threshold.
        /// </summary>
        public void ForceRelease()
        {
            if (!isCharging)
                return;

            if (CanRelease)
            {
                ReleaseLightningBolt();
            }
            else
            {
                if (showDebugTrace)
                    Debug.Log($"[LightningBoltAbility] Force release failed - only {ChargePercent * 100:F0}% charged (need {minChargeToFire * 100:F0}%)");
                ResetChargeState();
            }
        }

        private void ReleaseLightningBolt()
        {
            if (chargingUser == null)
            {
                ResetChargeState();
                return;
            }

            float currentCharge = ChargePercent;

            // Calculate damage and speed based on charge
            float damageMultiplier = partialRelease
                ? Mathf.Lerp(1f, fullChargeDamageMultiplier, currentCharge)
                : fullChargeDamageMultiplier;
            float speedMultiplier = partialRelease
                ? Mathf.Lerp(1f, fullChargeSpeedMultiplier, currentCharge)
                : fullChargeSpeedMultiplier;

            // Get target position (where crosshair is aiming), then spawn bolt from sky above it
            Vector3 targetPos = GetTargetPosition();
            Vector3 origin = targetPos + Vector3.up * spawnHeightAboveTarget;
            Vector3 direction = Vector3.down;

            if (showDebugTrace)
                Debug.Log($"[LightningBoltAbility] LIGHTNING BOLT RELEASED at {currentCharge * 100:F0}% charge! Damage: {projectileDamage * damageMultiplier:F1}, Speed: {projectileSpeed * speedMultiplier:F1}");

            // Spawn projectile VFX at origin
            if (projectileVFX != null)
            {
                SpawnVFX(projectileVFX, origin, Quaternion.LookRotation(direction));
            }

            // Spawn the lightning bolt projectile with charged stats
            SpawnChargedProjectile(chargingUser, origin, direction, damageMultiplier, speedMultiplier);

            // Apply effects to self if configured
            lastUser = chargingUser;
            ApplyEffectsToSelf();

            ResetChargeState();
        }

        /// <summary>
        /// Spawns a projectile with charge-modified damage and speed.
        /// </summary>
        private BaseProjectile SpawnChargedProjectile(GameObject user, Vector3 origin, Vector3 direction, float damageMultiplier, float speedMultiplier)
        {
            // Validate projectile prefab
            if (projectilePrefab == null)
            {
                Debug.LogError($"LightningBoltAbility '{name}': No projectile prefab assigned!");
                return null;
            }

            // Ensure pool manager exists
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError($"LightningBoltAbility '{name}': ObjectPoolManager not found! Cannot spawn projectile.");
                return null;
            }

            string poolName = GetPoolName();

            // Create pool if it doesn't exist
            if (!ObjectPoolManager.Instance.HasPool(poolName))
            {
                ObjectPoolManager.Instance.CreatePoolRuntime(
                    projectilePrefab.gameObject,
                    preloadCount: 10,
                    maxSize: 50,
                    allowExpansion: true
                );
            }

            // Get projectile from pool
            GameObject projectileObj = ObjectPoolManager.Instance.Get(poolName, origin, Quaternion.LookRotation(direction));
            if (projectileObj == null)
            {
                Debug.LogError($"LightningBoltAbility '{name}': Failed to get projectile from pool '{poolName}'!");
                return null;
            }

            BaseProjectile projectile = projectileObj.GetComponent<BaseProjectile>();
            if (projectile == null)
            {
                Debug.LogError($"LightningBoltAbility '{name}': Projectile '{projectileObj.name}' missing BaseProjectile component!");
                ObjectPoolManager.ReturnToPool(projectileObj);
                return null;
            }

            // Launch with charged values
            float chargedSpeed = projectileSpeed * speedMultiplier;
            float chargedDamage = projectileDamage * damageMultiplier;
            projectile.Launch(origin, direction, chargedSpeed, chargedDamage, user);

            return projectile;
        }

        private void ResetChargeState()
        {
            isCharging = false;
            chargeStartTime = 0f;
            chargingUser = null;
            chargeTargetPosition = Vector3.zero;
        }

        /// <summary>
        /// Gets the target position based on camera view (screen center).
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Raycast from screen center (crosshair)
                Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    return hit.point;
                }
                // No hit - return point at default distance
                return ray.origin + ray.direction * 50f;
            }

            // Fallback to position in front of user
            if (chargingUser != null)
                return chargingUser.transform.position + chargingUser.transform.forward * 10f;

            return Vector3.zero;
        }
    }
}
