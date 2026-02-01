using UnityEngine;

namespace Dwayne.Effects
{
    /// <summary>
    /// Component that applies a slow effect to a GameObject.
    /// Reduces movement speed for a duration, stacks with multiple applications.
    /// </summary>
    public class SlowEffect : MonoBehaviour
    {
        [Header("Current Slow State")]
        [SerializeField] private float slowMultiplier = 1f;
        [SerializeField] private float remainingDuration = 0f;
        [SerializeField] private bool isSlowed = false;

        private float slowEndTime = 0f;

        /// <summary>Current slow multiplier (1 = normal speed, 0.5 = 50% speed, 0 = frozen).</summary>
        public float SlowMultiplier => slowMultiplier;

        /// <summary>Is this GameObject currently slowed?</summary>
        public bool IsSlowed => isSlowed;

        /// <summary>Time remaining on the slow effect.</summary>
        public float RemainingDuration => remainingDuration;

        void Update()
        {
            if (!isSlowed)
                return;

            remainingDuration = slowEndTime - Time.time;

            if (Time.time >= slowEndTime)
            {
                RemoveSlow();
            }
        }

        /// <summary>
        /// Applies a slow effect with the given multiplier and duration.
        /// If already slowed, uses the stronger slow and refreshes duration.
        /// </summary>
        /// <param name="multiplier">Speed multiplier (0-1, where 0.5 = 50% speed)</param>
        /// <param name="duration">How long the slow lasts in seconds</param>
        public void ApplySlow(float multiplier, float duration)
        {
            // Use the stronger slow (lower multiplier)
            if (!isSlowed || multiplier < slowMultiplier)
            {
                slowMultiplier = Mathf.Clamp01(multiplier);
            }

            // Refresh duration (use the longer duration)
            float newEndTime = Time.time + duration;
            if (newEndTime > slowEndTime)
            {
                slowEndTime = newEndTime;
            }

            isSlowed = true;
            remainingDuration = slowEndTime - Time.time;
        }

        /// <summary>
        /// Removes the slow effect immediately.
        /// </summary>
        public void RemoveSlow()
        {
            isSlowed = false;
            slowMultiplier = 1f;
            remainingDuration = 0f;
            slowEndTime = 0f;
        }

        /// <summary>
        /// Gets the movement speed multiplier accounting for slow.
        /// Returns 1 if not slowed, otherwise returns the slow multiplier.
        /// </summary>
        public float GetMovementMultiplier()
        {
            return isSlowed ? slowMultiplier : 1f;
        }
    }
}
