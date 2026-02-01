using System.Collections;
using UnityEngine;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Fire movement: speed boost while running and leave behind DOT zones that damage enemies.
    /// </summary>
    public class DashTrailAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Fire;

        [Header("Run / Speed")]
        [Tooltip("How long you stay sped up (seconds).")]
        [SerializeField] float runDuration = 4f;
        [Tooltip("Speed while running (velocity magnitude).")]
        [SerializeField] float runSpeed = 22f;

        [Header("DOT Zones (left behind as you run)")]
        [Tooltip("Spawn a DOT zone every this many seconds along your path.")]
        [SerializeField] float zoneSpawnInterval = 0.35f;
        [Tooltip("Radius of each zone.")]
        [SerializeField] float zoneRadius = 2f;
        [Tooltip("How long each zone lasts (seconds).")]
        [SerializeField] float zoneLifetime = 4f;
        [Tooltip("Damage per tick to enemies standing in the zone.")]
        [SerializeField] float zoneDotDamagePerTick = 4f;
        [Tooltip("Time between damage ticks in the zone.")]
        [SerializeField] float zoneDotTickInterval = 0.5f;
        [Tooltip("DoT duration applied to enemies when they're in the zone.")]
        [SerializeField] float zoneDotDuration = 2f;
        [SerializeField] LayerMask zoneHitMask = ~0;

        private Coroutine _runRoutine;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponentInParent<Rigidbody>();
            CharacterController cc = user.GetComponentInParent<CharacterController>();
            if (rb == null && cc == null)
            {
                Debug.LogWarning("DashTrailAbility: user (and root) has no Rigidbody or CharacterController.");
                return false;
            }

            if (_runRoutine != null)
                StopCoroutine(_runRoutine);

            Transform mover = rb != null ? rb.transform : cc.transform;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - mover.position).normalized
                : mover.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = mover.forward;
            direction.Normalize();

            SpawnTrailVFX(mover.position, Quaternion.LookRotation(direction));
            _runRoutine = StartCoroutine(RunWithTrailZones(user, rb, cc, direction));
            return true;
        }

        private IEnumerator RunWithTrailZones(GameObject user, Rigidbody rb, CharacterController cc, Vector3 direction)
        {
            float elapsed = 0f;
            float nextZoneTime = 0f;
            Transform mover = rb != null ? rb.transform : cc.transform;

            while (elapsed < runDuration && (rb != null || cc != null))
            {
                if (rb != null)
                {
                    Vector3 vel = direction * runSpeed;
                    vel.y = rb.linearVelocity.y;
                    rb.linearVelocity = vel;
                }
                else if (cc != null)
                {
                    cc.Move(direction * runSpeed * Time.deltaTime);
                }

                if (elapsed >= nextZoneTime)
                {
                    SpawnDotZoneAt(mover.position, user);
                    nextZoneTime = elapsed + zoneSpawnInterval;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (rb != null)
                rb.linearVelocity = Vector3.zero;

            _runRoutine = null;
        }

        private void SpawnDotZoneAt(Vector3 position, GameObject owner)
        {
            GameObject zoneGo = new GameObject("DashTrailZone");
            zoneGo.transform.position = position;
            var zone = zoneGo.AddComponent<DashTrailZone>();
            zone.Init(owner, zoneRadius, zoneLifetime, zoneDotDamagePerTick, zoneDotTickInterval, zoneDotDuration, zoneHitMask);
        }
    }
}
