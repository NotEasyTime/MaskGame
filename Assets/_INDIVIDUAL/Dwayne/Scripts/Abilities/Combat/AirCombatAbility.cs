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

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
                return true;

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
