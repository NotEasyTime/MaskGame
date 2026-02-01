using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Air combat: Push — fires a trace that explodes on hit, applying knockback to all nearby enemies.
    /// </summary>
    public class PushAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Air;

        [Header("Trace Settings")]
        [SerializeField] float range = 25f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Explosion Settings")]
        [SerializeField] float explosionRadius = 5f;
        [SerializeField] float damage = 15f;
        [SerializeField] float knockbackForce = 15f;
        [SerializeField] float upwardForce = 5f;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;
        [SerializeField] float debugTraceDuration = 0.5f;
        [SerializeField] Color debugTraceColor = Color.cyan;
        [SerializeField] Color debugHitColor = Color.red;
        [SerializeField] Color debugExplosionColor = Color.yellow;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;

            // Spawn VFX at user when ability activates
            SpawnVFXAtUser(user);

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
            {
                // Debug: draw trace line to max range (miss)
                if (showDebugTrace)
                    Debug.DrawLine(origin, origin + direction * range, debugTraceColor, debugTraceDuration);

                // Spawn trail VFX even on miss (shows the attack direction)
                SpawnTrailVFX(origin, Quaternion.LookRotation(direction));
                return true;
            }

            // Debug: draw trace line to hit point
            if (showDebugTrace)
            {
                Debug.DrawLine(origin, hit.point, debugTraceColor, debugTraceDuration);
            }

            // Spawn impact VFX at hit point
            SpawnImpactVFX(hit.point, hit.normal);

            // Explode at hit point
            Explode(hit.point, user);

            return true;
        }

        private void Explode(Vector3 center, GameObject user)
        {
            // Debug: draw explosion radius
            if (showDebugTrace)
            {
                // Draw explosion sphere outline
                for (int i = 0; i < 32; i++)
                {
                    float angle1 = i * 11.25f * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * 11.25f * Mathf.Deg2Rad;

                    // Horizontal circle
                    Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * explosionRadius, 0f, Mathf.Sin(angle1) * explosionRadius);
                    Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * explosionRadius, 0f, Mathf.Sin(angle2) * explosionRadius);
                    Debug.DrawLine(p1, p2, debugExplosionColor, debugTraceDuration);

                    // Vertical circle (XY plane)
                    p1 = center + new Vector3(Mathf.Cos(angle1) * explosionRadius, Mathf.Sin(angle1) * explosionRadius, 0f);
                    p2 = center + new Vector3(Mathf.Cos(angle2) * explosionRadius, Mathf.Sin(angle2) * explosionRadius, 0f);
                    Debug.DrawLine(p1, p2, debugExplosionColor, debugTraceDuration);
                }
            }

            // Find all targets in explosion radius
            Collider[] hits = Physics.OverlapSphere(center, explosionRadius, hitMask);
            int hitCount = 0;

            foreach (Collider col in hits)
            {
                // Skip the user
                if (col.gameObject == user)
                    continue;

                Vector3 targetPos = col.transform.position;
                Vector3 closestPoint = col.ClosestPoint(center);
                Vector3 knockbackDirection = (targetPos - center).normalized;

                // If target is directly at center, push them away in a random direction
                if (knockbackDirection.sqrMagnitude < 0.01f)
                    knockbackDirection = Random.onUnitSphere;

                // Apply damage
                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damage, closestPoint, knockbackDirection, user);
                    hitCount++;
                }

                // Apply knockback force
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null && knockbackForce > 0f)
                {
                    Vector3 force = knockbackDirection * knockbackForce + Vector3.up * upwardForce;
                    rb.AddForce(force, ForceMode.Impulse);

                    // Debug: draw knockback direction
                    if (showDebugTrace)
                        Debug.DrawLine(targetPos, targetPos + force.normalized * 2f, debugHitColor, debugTraceDuration);
                }
            }

            if (showDebugTrace)
                Debug.Log($"[PushAbility] Explosion hit {hitCount} targets at {center}");
        }
    }
}
