# Creating a Weapon

`BaseWeapon` is abstract, so you must use a **concrete weapon class** (or create your own by extending `BaseWeapon` and implementing `DoFire`).

## Option 1: Use an existing weapon type

### Universal Weapon (works with any ability - hitscan or projectile)
1. Create an empty GameObject (e.g. "Rifle").
2. Add component **UniversalWeapon**.
3. **(Optional)** Assign a **BaseAbility** to **Fire Ability** or **Alt Fire Ability**:
   - **Hitscan abilities**: FireSpreadAbility, FireFocusAbility, etc. (instant raycast attacks)
   - **Projectile abilities**: GenericProjectileAbility, IceProjectileAbility, etc. (spawns projectiles)
   - **If no ability is assigned**, the weapon falls back to built-in fire logic
4. Configure **Fallback Fire Mode** (when no ability is assigned):
   - **Hitscan**: Instant raycast using weapon's Damage/Range/Fallback Hit Mask settings
   - **Projectile**: Spawns projectile from pool using Fallback Projectile Prefab/Speed settings
5. Set **Cooldown**, **Damage**, **Range**, and other weapon settings as needed.
6. From your input/character script, call:
   - `weapon.TryUseFireAbility(targetPosition)` - uses fireAbility if assigned, otherwise uses fallback fire
   - `weapon.TryUseAltFireAbility(targetPosition)` - uses altFireAbility if assigned, otherwise uses fallback fire
   - `weapon.Fire(firePoint.position, firePoint.forward)` - directly calls weapon fire logic

### Projectile Weapon (legacy - only works with ProjectileAbility)
1. **Projectile prefab:** Create a prefab with:
   - A **Rigidbody** (required by `BaseProjectile`).
   - A collider (Trigger or solid).
   - A component that implements `IProjectile`, e.g. **SimpleProjectile** (applies damage on hit, destroys on hit/expire).
2. Create an empty GameObject and add **ProjectileWeapon**.
3. Assign the projectile prefab to **Projectile Prefab**, set **Projectile Speed**, **Damage**, **Range**, etc.
4. Call `weapon.Fire(origin, direction)` (or charge flow) from input.

**Note:** UniversalWeapon is recommended as it works with both hitscan and projectile abilities.

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
