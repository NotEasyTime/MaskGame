using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Air combat: Push Back / Lightning — hitscan with damage and optional push force.
    /// </summary>
    public class AirCombatAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Air;

        [Header("Air Combat")]
        [SerializeField] float range = 25f;
        [SerializeField] float damage = 15f;
        [SerializeField] float pushForce = 10f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;
        [SerializeField] float debugTraceDuration = 0.5f;
        [SerializeField] Color debugTraceColor = Color.cyan;
        [SerializeField] Color debugHitColor = Color.red;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;

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
                // Draw a small cross at hit point
                Debug.DrawLine(hit.point - Vector3.right * 0.3f, hit.point + Vector3.right * 0.3f, debugHitColor, debugTraceDuration);
                Debug.DrawLine(hit.point - Vector3.up * 0.3f, hit.point + Vector3.up * 0.3f, debugHitColor, debugTraceDuration);
                Debug.DrawLine(hit.point - Vector3.forward * 0.3f, hit.point + Vector3.forward * 0.3f, debugHitColor, debugTraceDuration);
            }

            // Spawn impact VFX at hit point
            SpawnImpactVFX(hit.point, hit.normal);

            var damagable = hit.collider.GetComponent<IDamagable>();
            if (damagable != null && damagable.IsAlive)
            {
                damagable.TakeDamage(damage, hit.point, -direction, user);
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null && pushForce > 0f)
                    rb.AddForce(direction * pushForce, ForceMode.Impulse);
            }

            return true;
        }
    }
}
