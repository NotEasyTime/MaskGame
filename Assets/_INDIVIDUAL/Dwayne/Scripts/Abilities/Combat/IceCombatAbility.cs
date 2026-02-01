using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice combat: Slow AOE + Spear (projectile). No shotgun.
    /// This ability does AOE slow/damage; Spear can be a separate ProjectileWeapon.
    /// Uses the SlowEffect system from BaseAbility to apply slow to targets.
    /// </summary>
    public class IceCombatAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Ice Combat")]
        [SerializeField] float range = 8f;
        [SerializeField] float radius = 4f;
        [SerializeField] float damage = 12f;
        [SerializeField] LayerMask hitMask = ~0;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;

            Vector3 center = origin + direction * Mathf.Min(range * 0.5f, range - radius);
            Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);

            foreach (Collider col in hits)
            {
                var damagable = col.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    damagable.TakeDamage(damage, col.ClosestPoint(origin), (col.transform.position - origin).normalized, user);

                    // Apply slow effect using BaseAbility's slow system
                    ApplySlow(col.gameObject);
                }
            }

            return true;
        }
    }
}
