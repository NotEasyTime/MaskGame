using UnityEngine;
using Dwayne.Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Base class for abilities implementing IAbility.
    /// Handles cooldown and Use/Cancel; subclasses implement the actual effect in DoUse.
    /// </summary>
    public abstract class BaseAbility : MonoBehaviour, IAbility
    {
        [Header("Cooldown")]
        [SerializeField] protected float cooldownDuration = 1f;

        protected float lastUseTime = float.NegativeInfinity;

        public virtual bool CanUse => CooldownRemaining <= 0f;
        public virtual float CooldownRemaining => Mathf.Max(0f, lastUseTime + cooldownDuration - Time.time);
        public virtual float CooldownDuration => cooldownDuration;

        /// <summary>
        /// Perform the ability effect. Called by Use when CanUse. Return true if the ability was successfully used.
        /// </summary>
        protected abstract bool DoUse(GameObject user, Vector3 targetPosition);

        public virtual bool Use(GameObject user, Vector3 targetPosition = default)
        {
            if (!CanUse)
                return false;

            bool used = DoUse(user, targetPosition);
            if (used)
                lastUseTime = Time.time;
            return used;
        }

        public virtual void Cancel()
        {
            // Override in channeled abilities to interrupt.
        }
    }
}
