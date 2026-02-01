using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Earth combat: Spike DMG / Earthquake — AOE or line damage.
    /// </summary>
    public class EarthCombatAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Earth;

        [Header("Earth Combat")]
        [SerializeField] float range = 12f;
        [SerializeField] float radius = 3f;
        [SerializeField] float damage = 20f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Debug")]
        [SerializeField] bool showDebugTrace = true;
        [SerializeField] float debugTraceDuration = 0.5f;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 0.5f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;

            // Spawn VFX at user
            SpawnVFXAtUser(user);

            Vector3 end = origin + direction * range;

            // Debug: draw line to AOE center and circle
            if (showDebugTrace)
            {
                Debug.DrawLine(origin, end, Color.yellow, debugTraceDuration);
                for (int i = 0; i < 16; i++)
                {
                    float angle1 = i * 22.5f * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * 22.5f * Mathf.Deg2Rad;
                    Vector3 p1 = end + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                    Vector3 p2 = end + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                    Debug.DrawLine(p1, p2, Color.red, debugTraceDuration);
                }
            }

            // Spawn impact VFX at AOE center (scale by AOE radius)
            SpawnImpactVFX(end, Vector3.up, radius);

            Collider[] hits = Physics.OverlapSphere(end, radius, hitMask);

            foreach (Collider col in hits)
            {
                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damage, col.ClosestPoint(origin), (col.transform.position - origin).normalized, user);

                    if (showDebugTrace)
                        Debug.DrawLine(end, col.transform.position, Color.green, debugTraceDuration);
                }
            }

            return true;
        }
    }
}
