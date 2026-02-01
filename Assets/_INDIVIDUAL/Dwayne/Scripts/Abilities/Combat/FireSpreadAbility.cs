using UnityEngine;
using Dwayne.Interfaces;
using Element;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Fire spread: Multi-pellet hitscan shotgun attack.
    /// Counterpart to FireFocusAbility (focused beam).
    /// </summary>
    public class FireSpreadAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Fire;

        [Header("Fire Combat")]
        [SerializeField] float range = 20f;
        [SerializeField] float damage = 8f;
        [SerializeField] int pelletsPerShot = 6;
        [SerializeField] float spreadAngleDeg = 12f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Debug")]
        [SerializeField] bool showDebugLogs = true;
        [SerializeField] bool showDebugGizmos = true;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Vector3 origin = user.transform.position + Vector3.up * 1f;
            Vector3 baseDirection = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f)
                baseDirection = user.transform.forward;

            if (showDebugLogs)
                Debug.Log($"[FireSpreadAbility] Firing from {user.name} at {targetPosition} | {pelletsPerShot} pellets | {spreadAngleDeg}° spread");

            // Spawn VFX at user when firing
            SpawnVFXAtUser(user);

            float halfAngleRad = spreadAngleDeg * 0.5f * Mathf.Deg2Rad;
            int hitCount = 0;

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
                    hitCount++;

                    // Draw debug line
                    if (showDebugGizmos)
                        Debug.DrawLine(origin, hit.point, Color.red, 0.5f);

                    // Spawn impact VFX on hit
                    SpawnImpactVFX(hit.point, hit.normal);

                    var damagable = hit.collider.GetComponent<IDamagable>();
                    if (damagable != null && damagable.IsAlive)
                    {
                        damagable.TakeDamage(damage, hit.point, -direction, user);
                        if (showDebugLogs)
                            Debug.Log($"[FireSpreadAbility] Pellet {i + 1} hit {hit.collider.name} for {damage} damage");
                    }
                    else if (showDebugLogs)
                    {
                        Debug.Log($"[FireSpreadAbility] Pellet {i + 1} hit {hit.collider.name} (no damage)");
                    }
                }
                else if (showDebugGizmos)
                {
                    Debug.DrawRay(origin, direction * range, Color.yellow, 0.5f);
                }
            }

            if (showDebugLogs)
                Debug.Log($"[FireSpreadAbility] Spread complete | {hitCount}/{pelletsPerShot} pellets hit");

            return true;
        }
    }
}
