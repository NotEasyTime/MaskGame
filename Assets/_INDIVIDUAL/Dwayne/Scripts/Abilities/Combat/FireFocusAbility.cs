using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Fire focus: Concentrated beam attack with high single-target damage.
    /// Counterpart to FireSpreadAbility (spread).
    /// </summary>
    public class FireFocusAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Fire;

        [Header("Fire Focus")]
        [SerializeField] float range = 30f;
        [SerializeField] float damage = 25f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Beam Visual")]
        [SerializeField] float beamWidth = 0.1f;
        [SerializeField] GameObject beamVFX; // Optional beam visual effect

        [Header("Debug")]
        [SerializeField] bool showDebugLogs = true;
        [SerializeField] bool showDebugGizmos = true;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;

            if (showDebugLogs)
                Debug.Log($"[FireFocusAbility] Firing from {user.name} at {targetPosition} | Range: {range}m");

            // Spawn VFX at user when firing
            SpawnVFXAtUser(user);

            // Projectile VFX at origin for hit-scan visual (no physical projectile)
            if (projectileVFX != null)
            {
                GameObject vfx = SpawnVFX(projectileVFX, origin, Quaternion.LookRotation(direction));
                if (vfx != null)
                    Destroy(vfx, 0.2f);
            }

            // Single focused raycast
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
            {
                // Draw debug line
                if (showDebugGizmos)
                    Debug.DrawLine(origin, hit.point, Color.red, 1f);

                // Spawn impact VFX on hit
                SpawnImpactVFX(hit.point, hit.normal);

                // Projectile VFX at hit point for hit-scan visual (beam reached)
                if (projectileVFX != null)
                {
                    GameObject hitVfx = SpawnVFX(projectileVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    if (hitVfx != null)
                        Destroy(hitVfx, 0.15f);
                }

                // Spawn beam visual effect
                if (beamVFX != null)
                {
                    SpawnBeamVFX(origin, hit.point);
                }

                var damagable = hit.collider.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damage, hit.point, -direction, user);
                    if (showDebugLogs)
                        Debug.Log($"[FireFocusAbility] HIT {hit.collider.name} for {damage} damage at {hit.distance:F1}m");
                }
                else if (showDebugLogs)
                {
                    Debug.Log($"[FireFocusAbility] HIT {hit.collider.name} at {hit.distance:F1}m (no damage)");
                }
            }
            else
            {
                if (showDebugGizmos)
                    Debug.DrawRay(origin, direction * range, Color.yellow, 1f);

                if (showDebugLogs)
                    Debug.Log($"[FireFocusAbility] MISS - No target in range");

                // Spawn beam even if no hit (max range)
                if (beamVFX != null)
                {
                    SpawnBeamVFX(origin, origin + direction * range);
                }
            }

            return true;
        }

        /// <summary>
        /// Spawns a beam visual effect from origin to hit point.
        /// </summary>
        private void SpawnBeamVFX(Vector3 origin, Vector3 hitPoint)
        {
            if (beamVFX == null)
                return;

            Vector3 direction = (hitPoint - origin).normalized;
            float distance = Vector3.Distance(origin, hitPoint);
            Vector3 midPoint = origin + direction * (distance * 0.5f);

            GameObject beam = Instantiate(beamVFX, midPoint, Quaternion.LookRotation(direction));

            // Scale beam to match distance
            beam.transform.localScale = new Vector3(beamWidth, beamWidth, distance);

            // Auto-destroy after short duration
            Destroy(beam, 0.2f);
        }
    }
}
