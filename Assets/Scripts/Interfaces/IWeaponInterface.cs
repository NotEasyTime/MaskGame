using UnityEngine;

namespace Interfaces
{
    /// <summary>
    /// Common interface for weapons: projectile and hitscan.
    /// </summary>
    public interface IWeaponInterface
    {
        /// <summary>Whether the weapon is ready to fire (e.g. not on cooldown, has ammo).</summary>
        bool CanFire { get; }

        /// <summary>Current ammo in the magazine/clip.</summary>
        int CurrentAmmo { get; }

        /// <summary>Maximum ammo per magazine.</summary>
        int MagazineSize { get; }

        /// <summary>Base damage per shot (interpretation depends on projectile vs hitscan).</summary>
        float Damage { get; }

        /// <summary>Maximum range (for hitscan) or travel distance (for projectiles).</summary>
        float Range { get; }

        /// <summary>Attempt to fire the weapon. Returns true if a shot was fired.</summary>
        bool Fire(Vector3 origin, Vector3 direction);

        /// <summary>Start or trigger reload. Behavior depends on implementation (instant vs over time).</summary>
        void Reload();

        /// <summary>Whether the weapon is currently reloading.</summary>
        bool IsReloading { get; }
    }
}
