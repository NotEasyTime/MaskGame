using UnityEngine;

namespace Dwayne.Interfaces
{
    /// <summary>
    /// Common interface for weapons: projectile and hitscan.
    /// Supports charging, abilities, and cooldown-based shot count.
    /// </summary>
    public interface IWeaponInterface
    {
        // Fire state
        /// <summary>Whether the weapon is ready to fire (e.g. not on cooldown, has ammo).</summary>
        bool CanFire { get; }

        /// <summary>Attempt to fire the weapon. Returns true if a shot was fired.</summary>
        bool Fire(Vector3 origin, Vector3 direction);

        // Ammo / shot count
        /// <summary>Current shots remaining before refill.</summary>
        int CurrentAmmo { get; }

        /// <summary>Maximum shots before refill cooldown.</summary>
        int MagazineSize { get; }

        /// <summary>Whether the weapon is currently empty and waiting for refill.</summary>
        bool IsReloading { get; }

        /// <summary>Start or trigger reload. No-op when refill is cooldown-based.</summary>
        void Reload();

        // Cooldowns (for UI / input)
        /// <summary>Seconds until the weapon can fire again. 0 when ready.</summary>
        float CooldownRemaining { get; }

        /// <summary>Seconds until shots refill when empty. 0 when not empty or refill complete.</summary>
        float RefillCooldownRemaining { get; }

        // Stats
        /// <summary>Base damage per shot (interpretation depends on projectile vs hitscan).</summary>
        float Damage { get; }

        /// <summary>Maximum range (for hitscan) or travel distance (for projectiles).</summary>
        float Range { get; }

        // Charging (optional; no-op when not supported)
        /// <summary>True when the weapon is currently being charged.</summary>
        bool IsCharging { get; }

        /// <summary>Current charge progress 0..1. Only valid while IsCharging.</summary>
        float ChargeProgress { get; }

        /// <summary>Start charging a shot. Call ReleaseCharge to fire.</summary>
        void BeginCharge(Vector3 origin, Vector3 direction);

        /// <summary>Fire with current charge level. Returns true if a shot was released.</summary>
        bool ReleaseCharge();

        /// <summary>Cancel the current charge without firing.</summary>
        void CancelCharge();

        // Abilities (optional; no-op when not assigned)
        /// <summary>Try to use the fire ability (e.g. alt-fire). Returns true if the ability was used.</summary>
        bool TryUseFireAbility(Vector3 targetPosition = default);

        /// <summary>Try to use the alt-fire ability. Returns true if the ability was used.</summary>
        bool TryUseAltFireAbility(Vector3 targetPosition = default);

        // Owner (for abilities and damage source)
        /// <summary>Owner/wielder passed to abilities. Set via SetOwner when equipping.</summary>
        GameObject Owner { get; }

        /// <summary>Set the wielder/owner for ability calls.</summary>
        void SetOwner(GameObject wielder);
    }
}
