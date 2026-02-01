using UnityEngine;
using Interfaces;

namespace Dwayne.Effects
{
    /// <summary>
    /// Component that applies damage over time to a GameObject.
    /// Deals periodic damage for a duration.
    /// </summary>
    public class DoTEffect : MonoBehaviour
    {
        [Header("Current DoT State")]
        [SerializeField] private float damagePerTick = 5f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float remainingDuration = 0f;
        [SerializeField] private bool isActive = false;

        private float dotEndTime = 0f;
        private float nextTickTime = 0f;
        private GameObject damageSource;
        private IDamagable damagable;

        /// <summary>Damage dealt per tick.</summary>
        public float DamagePerTick => damagePerTick;

        /// <summary>Is this GameObject currently taking DoT?</summary>
        public bool IsActive => isActive;

        /// <summary>Time remaining on the DoT effect.</summary>
        public float RemainingDuration => remainingDuration;

        void Start()
        {
            damagable = GetComponent<IDamagable>();
        }

        void Update()
        {
            if (!isActive)
                return;

            remainingDuration = dotEndTime - Time.time;

            if (Time.time >= dotEndTime)
            {
                RemoveDoT();
                return;
            }

            if (Time.time >= nextTickTime)
            {
                ApplyTickDamage();
                nextTickTime = Time.time + tickInterval;
            }
        }

        private void ApplyTickDamage()
        {
            if (damagable == null)
                damagable = GetComponent<IDamagable>();

            if (damagable != null && damagable.IsAlive)
            {
                damagable.TakeDamage(damagePerTick, transform.position, Vector3.down, damageSource);
            }
        }

        /// <summary>
        /// Applies a DoT effect with the given parameters.
        /// If already active, uses the stronger damage and refreshes duration.
        /// </summary>
        /// <param name="damage">Damage per tick</param>
        /// <param name="interval">Time between damage ticks in seconds</param>
        /// <param name="duration">Total duration of the DoT effect</param>
        /// <param name="source">Source of the damage (for attribution)</param>
        public void ApplyDoT(float damage, float interval, float duration, GameObject source = null)
        {
            // Use the stronger damage
            if (!isActive || damage > damagePerTick)
            {
                damagePerTick = damage;
            }

            // Use shorter tick interval (more frequent damage)
            if (!isActive || interval < tickInterval)
            {
                tickInterval = Mathf.Max(0.1f, interval);
            }

            // Refresh duration (use the longer duration)
            float newEndTime = Time.time + duration;
            if (newEndTime > dotEndTime)
            {
                dotEndTime = newEndTime;
            }

            damageSource = source;

            if (!isActive)
            {
                nextTickTime = Time.time + tickInterval;
            }

            isActive = true;
            remainingDuration = dotEndTime - Time.time;
        }

        /// <summary>
        /// Removes the DoT effect immediately.
        /// </summary>
        public void RemoveDoT()
        {
            isActive = false;
            damagePerTick = 0f;
            tickInterval = 0.5f;
            remainingDuration = 0f;
            dotEndTime = 0f;
            damageSource = null;
        }
    }
}
