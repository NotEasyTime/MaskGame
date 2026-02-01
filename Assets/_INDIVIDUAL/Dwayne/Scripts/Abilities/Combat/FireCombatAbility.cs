using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Fire combat: Spread (multi-pellet hitscan) / Focus (single-target charged).
    /// This ability does multi-pellet spread; Focus can be a charged variant or separate weapon.
    /// </summary>
    public class FireCombatAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Fire;

        [Header("Fire Combat")]
        [SerializeField] float range = 20f;
        [SerializeField] float damage = 8f;
        [SerializeField] int pelletsPerShot = 6;
        [SerializeField] float spreadAngleDeg = 12f;
        [SerializeField] LayerMask hitMask = ~0;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 baseDirection = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f)
                baseDirection = user.transform.forward;

            // Spawn VFX at user when firing
            SpawnVFXAtUser(user);

            float halfAngleRad = spreadAngleDeg * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i < pelletsPerShot; i++)
            {
                float theta = Random.Range(0f, 1f) * halfAngleRad;
                float phi = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 local = new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Sin(theta) * Mathf.Sin(phi),
                    Mathf.Cos(theta));
                Vector3 direction = (Quaternion.FromToRotation(Vector3.forward, baseDirection) * local).normalized;

                if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
                {
                    // Spawn impact VFX on hit
                    SpawnImpactVFX(hit.point, hit.normal);

                    var damagable = hit.collider.GetComponent<IDamagable>();
                    if (damagable != null && damagable.IsAlive)
                        damagable.TakeDamage(damage, hit.point, -direction, user);
                }
            }

            return true;
        }
    }
}
