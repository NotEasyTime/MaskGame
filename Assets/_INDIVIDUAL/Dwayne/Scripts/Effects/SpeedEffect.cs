using UnityEngine;

namespace Dwayne.Effects
{
    /// <summary>
    /// Component that modifies movement speed of a GameObject.
    /// Can slow down (multiplier less than 1) or speed up (multiplier greater than 1).
    /// </summary>
    public class SpeedEffect : MonoBehaviour
    {
        [Header("Current Speed State")]
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float remainingDuration = 0f;
        [SerializeField] private bool isActive = false;

        private float effectEndTime = 0f;

        /// <summary>Current speed multiplier (1 = normal, 0.5 = 50% speed, 2 = 200% speed).</summary>
        public float SpeedMultiplier => speedMultiplier;

        /// <summary>Is this GameObject currently affected by a speed modifier?</summary>
        public bool IsActive => isActive;

        /// <summary>Is this a slow effect (multiplier less than 1)?</summary>
        public bool IsSlowed => isActive && speedMultiplier < 1f;

        /// <summary>Is this a speed boost (multiplier greater than 1)?</summary>
        public bool IsBoosted => isActive && speedMultiplier > 1f;

        /// <summary>Time remaining on the speed effect.</summary>
        public float RemainingDuration => remainingDuration;

        void Update()
        {
            if (!isActive)
                return;

            remainingDuration = effectEndTime - Time.time;

            if (Time.time >= effectEndTime)
            {
                RemoveEffect();
            }
        }

        /// <summary>
        /// Applies a speed modifier with the given multiplier and duration.
        /// For slows: uses the stronger slow (lower multiplier).
        /// For boosts: uses the stronger boost (higher multiplier).
        /// Refreshes duration if longer.
        /// </summary>
        /// <param name="multiplier">Speed multiplier (0.5 = 50% speed, 2 = 200% speed)</param>
        /// <param name="duration">How long the effect lasts in seconds</param>
        public void ApplySpeedModifier(float multiplier, float duration)
        {
            bool isNewSlow = multiplier < 1f;
            bool isCurrentSlow = speedMultiplier < 1f;

            if (!isActive)
            {
                speedMultiplier = Mathf.Max(0f, multiplier);
            }
            else if (isNewSlow && isCurrentSlow)
            {
                // Both are slows - use stronger slow (lower multiplier)
                speedMultiplier = Mathf.Min(speedMultiplier, Mathf.Max(0f, multiplier));
            }
            else if (!isNewSlow && !isCurrentSlow)
            {
                // Both are boosts - use stronger boost (higher multiplier)
                speedMultiplier = Mathf.Max(speedMultiplier, multiplier);
            }
            else
            {
                // Mixed slow/boost - slow takes priority
                if (isNewSlow)
                    speedMultiplier = Mathf.Max(0f, multiplier);
            }

            // Refresh duration (use the longer duration)
            float newEndTime = Time.time + duration;
            if (newEndTime > effectEndTime)
            {
                effectEndTime = newEndTime;
            }

            isActive = true;
            remainingDuration = effectEndTime - Time.time;
        }

        /// <summary>
        /// Removes the speed effect immediately.
        /// </summary>
        public void RemoveEffect()
        {
            isActive = false;
            speedMultiplier = 1f;
            remainingDuration = 0f;
            effectEndTime = 0f;
        }

        /// <summary>
        /// Gets the current movement speed multiplier.
        /// Returns 1 if no effect active, otherwise returns the modifier.
        /// </summary>
        public float GetMovementMultiplier()
        {
            return isActive ? speedMultiplier : 1f;
        }
    }
}
