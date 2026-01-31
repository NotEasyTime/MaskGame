using UnityEngine;

namespace Dwayne.Interfaces
{
    /// <summary>
    /// Interface for abilities: activation, cooldown, and state.
    /// </summary>
    public interface IAbility
    {
        /// <summary>Whether the ability can be used (e.g. off cooldown, has resource).</summary>
        bool CanUse { get; }

        /// <summary>Remaining cooldown time in seconds. 0 when ready.</summary>
        float CooldownRemaining { get; }

        /// <summary>Total cooldown duration in seconds.</summary>
        float CooldownDuration { get; }

        /// <summary>Attempt to activate the ability. Returns true if it was used.</summary>
        bool Use(GameObject user, Vector3 targetPosition = default);

        /// <summary>Optional: cancel or interrupt an active ability (e.g. channeled abilities).</summary>
        void Cancel();
    }
}
