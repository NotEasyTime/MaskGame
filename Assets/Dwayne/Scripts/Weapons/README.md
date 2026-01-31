# Creating a Weapon

`BaseWeapon` is abstract, so you must use a **concrete weapon class** (or create your own by extending `BaseWeapon` and implementing `DoFire`).

## Option 1: Use an existing weapon type

### Hitscan (instant raycast)
1. Create an empty GameObject (e.g. "Rifle").
2. Add component **HitscanWeapon**.
3. Set **Damage**, **Range**, **Cooldown**, **Spread** (optional) in the Inspector.
4. From your input/character script, call:
   - `weapon.Fire(firePoint.position, firePoint.forward)` for tap fire, or
   - `weapon.BeginCharge(firePoint.position, firePoint.forward)` then `weapon.ReleaseCharge()` for charge fire.
5. Optionally assign a child Transform as the fire point and use its position/forward.

### Projectile (spawns a projectile)
1. **Projectile prefab:** Create a prefab with:
   - A **Rigidbody** (required by `BaseProjectile`).
   - A collider (Trigger or solid).
   - A component that implements `IProjectile`, e.g. **SimpleProjectile** (applies damage on hit, destroys on hit/expire).
2. Create an empty GameObject and add **ProjectileWeapon**.
3. Assign the projectile prefab to **Projectile Prefab**, set **Projectile Speed**, **Damage**, **Range**, etc.
4. Call `weapon.Fire(origin, direction)` (or charge flow) from input.

## Option 2: Create a custom weapon

1. Create a new C# script that inherits from `BaseWeapon`.
2. Implement the abstract method:
   ```csharp
   protected override bool DoFire(Vector3 origin, Vector3 direction)
   ```
   Return `true` if a shot was fired. Optionally override `DoFire(origin, direction, charge)` to use charge (e.g. scale damage).
3. Add the component to a GameObject and configure it in the Inspector.
4. Drive it via `Fire(origin, direction)` or `BeginCharge` / `ReleaseCharge()` from your input or character logic.

## Input flow

- **Tap fire:** `if (weapon.CanFire) weapon.Fire(origin, direction);`
- **Charge fire:** Fire down → `weapon.BeginCharge(origin, direction)`; Fire up → `weapon.ReleaseCharge()`
- **Fire ability (alt-fire):** `weapon.TryUseFireAbility(aimPoint);`
- **Debug (Editor):** Right-click the weapon component → **Fire** or **Fire Ability**

## Damage

Hitscan and `SimpleProjectile` apply damage to any hit object that has a component implementing **IDamagable** (e.g. from `KuroParadigm.Calamity.Tools.Core`). Ensure your damageable objects use that interface if you want weapons to affect them.
