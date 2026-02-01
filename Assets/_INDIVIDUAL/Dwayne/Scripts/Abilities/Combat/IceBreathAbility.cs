using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice Breath: Channeled cone-shaped freezing breath attack.
    /// Hold to continuously damage and slow enemies in a cone.
    /// Drains resource while channeling, stops when resource depleted or released.
    /// </summary>
    public class IceBreathAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Ice Breath")]
        [SerializeField] float range = 8f;
        [SerializeField] float coneAngle = 45f;
        [SerializeField] float damagePerSecond = 20f;
        [SerializeField] float tickRate = 0.1f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Resource")]
        [SerializeField] float maxResource = 100f;
        [SerializeField] float drainPerSecond = 25f;
        [SerializeField] float regenPerSecond = 15f;
        [SerializeField] float regenDelay = 1f;
        [SerializeField] bool regenWhileChanneling = false;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;

        // Channeling state
        private bool isChanneling = false;
        private GameObject channelingUser = null;
        private Vector3 channelTargetPosition;
        private float lastTickTime = 0f;
        private float currentResource;
        private float lastChannelEndTime = 0f;

        // VFX instance for continuous effect
        private GameObject activeVFX;

        /// <summary>Current resource amount.</summary>
        public float CurrentResource => currentResource;

        /// <summary>Maximum resource amount.</summary>
        public float MaxResource => maxResource;

        /// <summary>Current resource as percentage (0 to 1).</summary>
        public float ResourcePercent => currentResource / maxResource;

        /// <summary>Is the ability currently channeling?</summary>
        public bool IsChanneling => isChanneling;

        /// <summary>Can the ability be used (has resource and off cooldown)?</summary>
        public override bool CanUse => base.CanUse && currentResource > 0f;

        protected override void Awake()
        {
            base.Awake();
            currentResource = maxResource;
        }

        void Update()
        {
            // Handle channeling
            if (isChanneling)
            {
                UpdateChanneling();
            }
            else
            {
                // Regenerate resource when not channeling (after delay)
                if (currentResource < maxResource && Time.time >= lastChannelEndTime + regenDelay)
                {
                    currentResource = Mathf.Min(maxResource, currentResource + regenPerSecond * Time.deltaTime);
                }
            }
        }

        private void UpdateChanneling()
        {
            if (channelingUser == null)
            {
                StopChanneling();
                return;
            }

            // Drain resource
            currentResource -= drainPerSecond * Time.deltaTime;

            // Regen while channeling if enabled
            if (regenWhileChanneling)
            {
                currentResource += regenPerSecond * Time.deltaTime;
            }

            // Stop if resource depleted
            if (currentResource <= 0f)
            {
                currentResource = 0f;
                StopChanneling();
                return;
            }

            // Apply damage at tick rate
            if (Time.time >= lastTickTime + tickRate)
            {
                lastTickTime = Time.time;
                ApplyBreathDamage();
            }

            // Update VFX position/rotation
            if (activeVFX != null)
            {
                Vector3 origin = channelingUser.transform.position + Vector3.up * 1f;
                Vector3 direction = GetChannelDirection();
                activeVFX.transform.position = origin;
                activeVFX.transform.rotation = Quaternion.LookRotation(direction);
            }

            // Debug visualization
            if (showDebugTrace)
            {
                DrawConeDebug();
            }
        }

        private Vector3 GetChannelDirection()
        {
            Vector3 origin = channelingUser.transform.position + Vector3.up * 1f;

            // Use camera direction for continuous aiming
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    return (hit.point - origin).normalized;
                }
                return ray.direction;
            }

            return channelingUser.transform.forward;
        }

        private void ApplyBreathDamage()
        {
            Vector3 origin = channelingUser.transform.position + Vector3.up * 1f;
            Vector3 direction = GetChannelDirection();
            float damageThisTick = damagePerSecond * tickRate;

            // Find all targets in range, then filter by cone angle
            Collider[] hits = Physics.OverlapSphere(origin, range, hitMask);
            float halfConeAngle = coneAngle * 0.5f;

            foreach (Collider col in hits)
            {
                // Skip self
                if (col.gameObject == channelingUser)
                    continue;

                Vector3 toTarget = col.transform.position - origin;

                if (toTarget.sqrMagnitude < 0.01f)
                    continue;

                float angleToTarget = Vector3.Angle(direction, toTarget.normalized);

                if (angleToTarget > halfConeAngle)
                    continue;

                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damageThisTick, col.ClosestPoint(origin), toTarget.normalized, channelingUser);

                    // Apply speed modifier (slow)
                    ApplySpeedModifier(col.gameObject);

                    if (showDebugTrace)
                        Debug.DrawLine(origin, col.transform.position, Color.white, tickRate);
                }
            }
        }

        private void DrawConeDebug()
        {
            Vector3 origin = channelingUser.transform.position + Vector3.up * 1f;
            Vector3 direction = GetChannelDirection();

            // Get perpendicular axes for 3D cone
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, direction).normalized;
            if (right.sqrMagnitude < 0.01f)
            {
                right = Vector3.Cross(Vector3.forward, direction).normalized;
            }
            up = Vector3.Cross(direction, right).normalized;

            Color coneColor = Color.Lerp(Color.red, Color.cyan, ResourcePercent);

            // Draw cone edges
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
                float halfCone = coneAngle * 0.5f * Mathf.Deg2Rad;

                Vector3 edgeDir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, direction) *
                                  (direction * Mathf.Cos(halfCone) + up * Mathf.Sin(halfCone));
                edgeDir = edgeDir.normalized;

                Debug.DrawRay(origin, edgeDir * range, coneColor, 0f);
            }

            // Draw center ray
            Debug.DrawRay(origin, direction * range, Color.cyan, 0f);
        }

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            // Start channeling
            if (isChanneling)
                return false;

            if (currentResource <= 0f)
                return false;

            isChanneling = true;
            channelingUser = user;
            channelTargetPosition = targetPosition;
            lastTickTime = Time.time;

            // Spawn continuous VFX
            if (spawnVFX != null)
            {
                Vector3 origin = user.transform.position + Vector3.up * 1f;
                Vector3 direction = targetPosition != Vector3.zero
                    ? (targetPosition - origin).normalized
                    : user.transform.forward;
                activeVFX = Instantiate(spawnVFX, origin, Quaternion.LookRotation(direction));
            }

            if (showDebugTrace)
                Debug.Log($"[IceBreathAbility] Channeling started by {user.name}");

            return true;
        }

        public override void Cancel()
        {
            if (isChanneling)
            {
                StopChanneling();
            }
        }

        private void StopChanneling()
        {
            if (showDebugTrace && channelingUser != null)
                Debug.Log($"[IceBreathAbility] Channeling stopped. Resource: {currentResource:F1}/{maxResource}");

            isChanneling = false;
            channelingUser = null;
            lastChannelEndTime = Time.time;

            // Destroy active VFX
            if (activeVFX != null)
            {
                Destroy(activeVFX);
                activeVFX = null;
            }
        }

        /// <summary>
        /// Force stop channeling (for external systems).
        /// </summary>
        public void ForceStop()
        {
            StopChanneling();
        }

        /// <summary>
        /// Refill resource to max (for pickups, etc.).
        /// </summary>
        public void RefillResource()
        {
            currentResource = maxResource;
        }

        /// <summary>
        /// Add resource amount.
        /// </summary>
        public void AddResource(float amount)
        {
            currentResource = Mathf.Min(maxResource, currentResource + amount);
        }
    }
}
