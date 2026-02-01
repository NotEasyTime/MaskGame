using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// EarthQuake: Charged AOE ability.
    /// Hold to charge, at 100% charge releases a devastating earthquake that
    /// applies damage over time and speed modifier to all enemies in the area.
    /// Enable canDoT and canModifySpeed in the inspector to apply effects.
    /// </summary>
    public class EarthQuakeAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Earth;

        [Header("Charge Settings")]
        [SerializeField] float chargeTime = 2f;
        [SerializeField] bool autoReleaseAtFullCharge = true;

        [Header("Earthquake AOE")]
        [SerializeField] float radius = 10f;
        [SerializeField] float initialDamage = 20f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;
        [SerializeField] float debugTraceDuration = 1f;

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

        void Update()
        {
            if (!isCharging)
                return;

            // Debug: show charging progress
            if (showDebugTrace && chargingUser != null)
            {
                float progress = ChargePercent;
                Color chargeColor = Color.Lerp(Color.yellow, Color.green, progress);

                // Draw expanding circle to show charge progress
                float currentRadius = radius * progress;
                Vector3 center = chargingUser.transform.position;
                for (int i = 0; i < 16; i++)
                {
                    float angle1 = i * 22.5f * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * 22.5f * Mathf.Deg2Rad;
                    Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * currentRadius, 0.1f, Mathf.Sin(angle1) * currentRadius);
                    Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * currentRadius, 0.1f, Mathf.Sin(angle2) * currentRadius);
                    Debug.DrawLine(p1, p2, chargeColor, 0f);
                }
            }

            // Auto-release at full charge
            if (autoReleaseAtFullCharge && IsFullyCharged)
            {
                ReleaseEarthquake();
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
                Debug.Log($"[EarthQuakeAbility] Charging started by {user.name}");

            return true;
        }

        public override void Cancel()
        {
            if (!isCharging)
                return;

            // Only release if fully charged
            if (IsFullyCharged)
            {
                ReleaseEarthquake();
            }
            else
            {
                // Cancel without releasing
                if (showDebugTrace)
                    Debug.Log($"[EarthQuakeAbility] Charge cancelled at {ChargePercent * 100:F0}%");

                ResetChargeState();
            }
        }

        /// <summary>
        /// Force release the earthquake at current charge level.
        /// Only triggers full effect if fully charged.
        /// </summary>
        public void ForceRelease()
        {
            if (!isCharging)
                return;

            if (IsFullyCharged)
            {
                ReleaseEarthquake();
            }
            else
            {
                if (showDebugTrace)
                    Debug.Log($"[EarthQuakeAbility] Force release failed - only {ChargePercent * 100:F0}% charged (need 100%)");
                ResetChargeState();
            }
        }

        private void ReleaseEarthquake()
        {
            if (chargingUser == null)
            {
                ResetChargeState();
                return;
            }

            Vector3 center = chargingUser.transform.position;

            if (showDebugTrace)
            {
                Debug.Log($"[EarthQuakeAbility] EARTHQUAKE RELEASED at {center}!");

                // Draw final AOE circle
                for (int i = 0; i < 32; i++)
                {
                    float angle1 = i * 11.25f * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * 11.25f * Mathf.Deg2Rad;
                    Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0.1f, Mathf.Sin(angle1) * radius);
                    Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0.1f, Mathf.Sin(angle2) * radius);
                    Debug.DrawLine(p1, p2, Color.red, debugTraceDuration);
                }
            }

            // Spawn impact VFX at center
            SpawnImpactVFX(center, Vector3.up);

            // Set lastUser for effect attribution (in case another ability was used while charging)
            lastUser = chargingUser;

            // Find all targets in radius
            Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);
            int hitCount = 0;

            foreach (Collider col in hits)
            {
                // Skip the user
                if (col.gameObject == chargingUser)
                    continue;

                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    hitCount++;
                    Vector3 hitPoint = col.ClosestPoint(center);
                    Vector3 hitDirection = (col.transform.position - center).normalized;

                    // Apply initial damage
                    damagable.TakeDamage(initialDamage, hitPoint, hitDirection, chargingUser);

                    // Apply DoT effect
                    ApplyDoT(col.gameObject);

                    // Apply speed modifier (slow)
                    ApplySpeedModifier(col.gameObject);

                    if (showDebugTrace)
                        Debug.DrawLine(center, col.transform.position, Color.magenta, debugTraceDuration);
                }
            }

            if (showDebugTrace)
                Debug.Log($"[EarthQuakeAbility] Hit {hitCount} targets with DoT and speed modifier");

            ResetChargeState();
        }

        private void ResetChargeState()
        {
            isCharging = false;
            chargeStartTime = 0f;
            chargingUser = null;
            chargeTargetPosition = Vector3.zero;
        }
    }
}
