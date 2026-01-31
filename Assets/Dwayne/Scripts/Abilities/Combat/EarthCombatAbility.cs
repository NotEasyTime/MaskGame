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

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 0.5f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;

            Vector3 end = origin + direction * range;
            Collider[] hits = Physics.OverlapSphere(end, radius, hitMask);

            foreach (Collider col in hits)
            {
                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damage, col.ClosestPoint(origin), (col.transform.position - origin).normalized, user);
                }
            }

            return true;
        }
    }
}
