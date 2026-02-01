using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice Breath: Cone-shaped freezing breath attack.
    /// Damages and slows all enemies in a cone in front of the user.
    /// Uses the SpeedEffect system from BaseAbility to apply slow to targets.
    /// </summary>
    public class IceBreathAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Ice Breath")]
        [SerializeField] float range = 8f;
        [SerializeField] float coneAngle = 45f;
        [SerializeField] float damage = 12f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;
        [SerializeField] float debugTraceDuration = 0.5f;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;
            direction.Normalize();

            // Spawn VFX at user
            SpawnVFXAtUser(user);

            // Debug: draw cone shape
            if (showDebugTrace)
            {
                float halfAngle = coneAngle * 0.5f * Mathf.Deg2Rad;
                Vector3 leftDir = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * direction;
                Vector3 rightDir = Quaternion.Euler(0, coneAngle * 0.5f, 0) * direction;

                Debug.DrawRay(origin, direction * range, Color.cyan, debugTraceDuration);
                Debug.DrawRay(origin, leftDir * range, Color.blue, debugTraceDuration);
                Debug.DrawRay(origin, rightDir * range, Color.blue, debugTraceDuration);

                // Draw arc at range
                int segments = 8;
                for (int i = 0; i < segments; i++)
                {
                    float t1 = (float)i / segments;
                    float t2 = (float)(i + 1) / segments;
                    float angle1 = Mathf.Lerp(-coneAngle * 0.5f, coneAngle * 0.5f, t1);
                    float angle2 = Mathf.Lerp(-coneAngle * 0.5f, coneAngle * 0.5f, t2);
                    Vector3 p1 = origin + Quaternion.Euler(0, angle1, 0) * direction * range;
                    Vector3 p2 = origin + Quaternion.Euler(0, angle2, 0) * direction * range;
                    Debug.DrawLine(p1, p2, Color.blue, debugTraceDuration);
                }
            }

            // Spawn impact VFX at mid-range
            SpawnImpactVFX(origin + direction * (range * 0.5f), Vector3.up);

            // Find all targets in range, then filter by cone angle
            Collider[] hits = Physics.OverlapSphere(origin, range, hitMask);
            float halfConeAngle = coneAngle * 0.5f;

            foreach (Collider col in hits)
            {
                Vector3 toTarget = col.transform.position - origin;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.01f)
                    continue;

                float angleToTarget = Vector3.Angle(direction, toTarget.normalized);

                if (angleToTarget > halfConeAngle)
                    continue;

                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damage, col.ClosestPoint(origin), (col.transform.position - origin).normalized, user);

                    // Apply speed modifier (slow) using BaseAbility's system
                    ApplySpeedModifier(col.gameObject);

                    if (showDebugTrace)
                        Debug.DrawLine(origin, col.transform.position, Color.white, debugTraceDuration);
                }
            }

            return true;
        }
    }
}
