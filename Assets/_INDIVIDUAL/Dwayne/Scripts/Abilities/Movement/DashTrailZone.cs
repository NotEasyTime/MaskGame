using UnityEngine;
using Dwayne.Effects;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// A fire trail zone that damages enemies over time while they stand in it.
    /// Spawned by DashTrailAbility along the dash path.
    /// </summary>
    public class DashTrailZone : MonoBehaviour
    {
        [Header("Zone (set via Init when spawned)")]
        [SerializeField] float radius = 2f;
        [SerializeField] float zoneLifetime = 4f;
        [SerializeField] float dotDamagePerTick = 4f;
        [SerializeField] float dotTickInterval = 0.5f;
        [SerializeField] float dotDuration = 2f;
        [SerializeField] LayerMask hitMask = ~0;

        private GameObject _owner;
        private float _zoneEndTime;
        private float _nextTickTime;

        /// <summary>Initialize the zone when spawned by DashTrailAbility.</summary>
        public void Init(GameObject owner, float zoneRadius, float lifetime, float damagePerTick, float tickInterval, float dotDur, LayerMask mask)
        {
            _owner = owner;
            radius = zoneRadius;
            zoneLifetime = lifetime;
            dotDamagePerTick = damagePerTick;
            dotTickInterval = Mathf.Max(0.1f, tickInterval);
            dotDuration = dotDur;
            hitMask = mask;
            _zoneEndTime = Time.time + zoneLifetime;
            _nextTickTime = Time.time + dotTickInterval;
            Destroy(gameObject, zoneLifetime);
        }

        private void Update()
        {
            if (Time.time >= _zoneEndTime)
                return;

            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + dotTickInterval;
                ApplyTickToEnemiesInZone();
            }
        }

        private void ApplyTickToEnemiesInZone()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitMask);
            foreach (Collider col in hits)
            {
                if (col == null || col.gameObject == _owner)
                    continue;

                var damagable = col.GetComponent<IDamagable>();
                if (damagable == null || !damagable.IsAlive)
                    continue;

                DoTEffect dot = col.GetComponent<DoTEffect>();
                if (dot == null)
                    dot = col.gameObject.AddComponent<DoTEffect>();
                dot.ApplyDoT(dotDamagePerTick, dotTickInterval, dotDuration, _owner);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.color = new Color(1f, 0.35f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
