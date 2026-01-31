using System;
using UnityEngine;

namespace Interfaces
{
    /// <summary>
    /// Interface for objects that can receive damage (body, limb, destructible).
    /// </summary>
    public interface IDamagable
    {
        /// <summary>Current health value.</summary>
        float CurrentHealth { get; }

        /// <summary>Maximum health value.</summary>
        float MaxHealth { get; }

        /// <summary>True if this object is still alive (e.g. health > 0).</summary>
        bool IsAlive { get; }

        /// <summary>Apply damage. Returns actual damage applied.</summary>
        /// <param name="amount">Damage amount.</param>
        /// <param name="hitPoint">World position of hit.</param>
        /// <param name="hitDirection">Direction of hit (e.g. from attacker).</param>
        /// <param name="source">Optional source of damage (e.g. GameObject, Projectile).</param>
        float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null);

        /// <summary>Raised when damage is applied. Args: amount, hitPoint, source.</summary>
        event Action<float, Vector3, object> OnDamaged;

        /// <summary>Raised when this object dies (e.g. health reaches zero).</summary>
        event Action OnDeath;
    }
}
