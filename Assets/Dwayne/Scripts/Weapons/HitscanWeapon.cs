using UnityEngine;
using KuroParadigm.Calamity.Tools.Core;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Weapon that fires an instant raycast (hitscan). Use for rifles, pistols, lasers.
    /// Assign to a GameObject (optionally with a child as fire point); call Fire(origin, direction) from input.
    /// </summary>
    public class HitscanWeapon : BaseWeapon
    {
        [Header("Hitscan")]
        [SerializeField] protected LayerMask hitMask = ~0;
        [SerializeField] protected bool useChargeDamage = true;

        protected override bool DoFire(Vector3 origin, Vector3 direction)
        {
            return DoFire(origin, direction, 1f);
        }

        protected override bool DoFire(Vector3 origin, Vector3 direction, float charge)
        {
            if (!Physics.Raycast(origin, direction, out var hit, range, hitMask))
                return true; // count as fired (no hit)

            float damageAmount = damage;
            if (useChargeDamage)
                damageAmount *= Mathf.Lerp(1f, fullChargeDamageMultiplier, charge);

            var damagable = hit.collider.GetComponent<IDamagable>();
            if (damagable != null && damagable.IsAlive)
                damagable.TakeDamage(damageAmount, hit.point, -direction, gameObject);

            return true;
        }
    }
}
