using UnityEngine;

namespace Dwayne.Interfaces
{
    /// <summary>
    /// Interface for projectile behavior: movement, impact, and damage.
    /// </summary>
    public interface IProjectile
    {
        /// <summary>Damage dealt on impact.</summary>
        float Damage { get; }

        /// <summary>Maximum distance or time before the projectile is destroyed.</summary>
        float MaxLifetime { get; }

        /// <summary>Layer or tag mask for valid hit targets (optional, implementation-dependent).</summary>
        LayerMask HitMask { get; }

        /// <summary>Initialize and launch the projectile from origin along direction.</summary>
        void Launch(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner = null);

        /// <summary>Called when the projectile hits something. Implement impact logic and cleanup.</summary>
        /// <param name="other">The collider that was hit.</param>
        /// <param name="point">World position of the hit.</param>
        /// <param name="normal">Surface normal at the hit point.</param>
        void OnHit(Collider other, Vector3 point, Vector3 normal);

        /// <summary>Called when the projectile expires (e.g. max lifetime) without hitting.</summary>
        void OnExpire();
    }
}
