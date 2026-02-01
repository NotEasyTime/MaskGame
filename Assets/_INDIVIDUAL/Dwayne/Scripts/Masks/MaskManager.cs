using UnityEngine;
using Dwayne.Abilities;
using Dwayne.Weapons;
using Element;
using Interfaces;

namespace Dwayne.Masks
{
    /// <summary>
    /// Manages the player's masks with separate slots for weapon and movement ability.
    /// Allows independent cycling of weapon mask and movement mask.
    /// NextMask cycles the weapon, PreviousMask cycles the movement ability.
    /// </summary>
    public class MaskManager : MonoBehaviour
    {
        [Header("Masks")]
        [Tooltip("Array of available masks that the player can switch between")]
        [SerializeField] private Mask[] availableMasks;

        [Tooltip("Index of the mask used for the weapon")]
        [SerializeField] private int weaponMaskIndex = 0;

        [Tooltip("Index of the mask used for the movement ability")]
        [SerializeField] private int movementMaskIndex = 0;

        private Mask currentWeaponMask;
        private Mask currentMovementMask;

        [Header("Spawn Points")]
        [Tooltip("Where to spawn the weapon (e.g., hand transform)")]
        [SerializeField] private Transform weaponSpawnPoint;

        [Tooltip("Where to spawn the movement ability (usually the player itself)")]
        [SerializeField] private Transform movementAbilitySpawnPoint;

        [Header("Targeting")]
        [Tooltip("Camera used for aim raycast. If unset, uses Camera.main (ensure your look-around camera is tagged MainCamera).")]
        [SerializeField] private Camera targetingCamera;

        [Tooltip("Use screen center (crosshair) for aiming instead of mouse position")]
        [SerializeField] private bool useCrosshairAiming = true;

        [Tooltip("Layer mask for targeting raycast")]
        [SerializeField] private LayerMask targetingRaycastMask = ~0;

        [Tooltip("Maximum targeting distance for raycast")]
        [SerializeField] private float maxTargetDistance = 100f;

        [Tooltip("Default targeting distance when raycast doesn't hit anything")]
        [SerializeField] private float defaultTargetDistance = 50f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Active instances
        private GameObject weaponInstance;
        private BaseWeapon weaponComponent;

        private GameObject movementAbilityInstance;
        private BaseAbility movementAbilityComponent;

        // Cached references
        private Camera mainCamera;
        private IDamagable playerDamagable;

        void Start()
        {
            mainCamera = targetingCamera != null ? targetingCamera : Camera.main;
            playerDamagable = GetComponent<IDamagable>();

            // Set spawn points to self if not assigned
            if (weaponSpawnPoint == null)
                weaponSpawnPoint = transform;

            if (movementAbilitySpawnPoint == null)
                movementAbilitySpawnPoint = transform;

            // Validate masks array
            if (availableMasks == null || availableMasks.Length == 0)
            {
                Debug.LogError("MaskManager: No masks assigned! Please add masks to the availableMasks array.");
                return;
            }

            // Clamp mask indices to valid range
            weaponMaskIndex = Mathf.Clamp(weaponMaskIndex, 0, availableMasks.Length - 1);
            movementMaskIndex = Mathf.Clamp(movementMaskIndex, 0, availableMasks.Length - 1);

            // Equip the initial masks
            EquipWeaponFromMask(weaponMaskIndex);
            EquipMovementFromMask(movementMaskIndex);
        }

        /// <summary>
        /// Equips weapon from a specific mask index.
        /// </summary>
        public void EquipWeaponFromMask(int index)
        {
            if (availableMasks == null || availableMasks.Length == 0)
            {
                Debug.LogError("MaskManager: No masks available to equip!");
                return;
            }

            if (index < 0 || index >= availableMasks.Length)
            {
                Debug.LogError($"MaskManager: Mask index {index} out of range (0-{availableMasks.Length - 1})!");
                return;
            }

            Mask mask = availableMasks[index];
            if (mask == null || !mask.IsValid())
            {
                Debug.LogError("MaskManager: Cannot equip invalid mask for weapon!");
                return;
            }

            // Unequip current weapon
            UnequipCurrentWeapon();

            weaponMaskIndex = index;
            currentWeaponMask = mask;

            // Spawn weapon
            SpawnWeapon();

            if (showDebugLogs)
            {
                Debug.Log($"MaskManager: Equipped weapon from mask '{mask.maskName}' (Weapon: {weaponComponent?.GetType().Name})");
            }
        }

        /// <summary>
        /// Equips movement ability from a specific mask index.
        /// </summary>
        public void EquipMovementFromMask(int index)
        {
            if (availableMasks == null || availableMasks.Length == 0)
            {
                Debug.LogError("MaskManager: No masks available to equip!");
                return;
            }

            if (index < 0 || index >= availableMasks.Length)
            {
                Debug.LogError($"MaskManager: Mask index {index} out of range (0-{availableMasks.Length - 1})!");
                return;
            }

            Mask mask = availableMasks[index];
            if (mask == null || !mask.IsValid())
            {
                Debug.LogError("MaskManager: Cannot equip invalid mask for movement!");
                return;
            }

            // Unequip current movement ability
            UnequipCurrentMovementAbility();

            movementMaskIndex = index;
            currentMovementMask = mask;

            // Spawn movement ability
            SpawnMovementAbility();

            if (showDebugLogs)
            {
                Debug.Log($"MaskManager: Equipped movement from mask '{mask.maskName}' (Movement: {movementAbilityComponent?.GetType().Name})");
            }
        }

        /// <summary>
        /// Cycles to the next weapon mask (wraps around to first).
        /// </summary>
        public void NextMask()
        {
            NextWeaponMask();
        }

        /// <summary>
        /// Cycles to the previous movement ability mask (wraps around to last).
        /// </summary>
        public void PreviousMask()
        {
            PreviousMovementMask();
        }

        /// <summary>
        /// Cycles to the next weapon mask (wraps around to first).
        /// </summary>
        public void NextWeaponMask()
        {
            if (availableMasks == null || availableMasks.Length == 0)
                return;

            weaponMaskIndex = (weaponMaskIndex + 1) % availableMasks.Length;
            EquipWeaponFromMask(weaponMaskIndex);
        }

        /// <summary>
        /// Cycles to the previous weapon mask (wraps around to last).
        /// </summary>
        public void PreviousWeaponMask()
        {
            if (availableMasks == null || availableMasks.Length == 0)
                return;

            weaponMaskIndex--;
            if (weaponMaskIndex < 0)
                weaponMaskIndex = availableMasks.Length - 1;

            EquipWeaponFromMask(weaponMaskIndex);
        }

        /// <summary>
        /// Cycles to the next movement ability mask (wraps around to first).
        /// </summary>
        public void NextMovementMask()
        {
            if (availableMasks == null || availableMasks.Length == 0)
                return;

            movementMaskIndex = (movementMaskIndex + 1) % availableMasks.Length;
            EquipMovementFromMask(movementMaskIndex);
        }

        /// <summary>
        /// Cycles to the previous movement ability mask (wraps around to last).
        /// </summary>
        public void PreviousMovementMask()
        {
            if (availableMasks == null || availableMasks.Length == 0)
                return;

            movementMaskIndex--;
            if (movementMaskIndex < 0)
                movementMaskIndex = availableMasks.Length - 1;

            EquipMovementFromMask(movementMaskIndex);
        }

        /// <summary>
        /// Unequips the current weapon.
        /// </summary>
        public void UnequipCurrentWeapon()
        {
            if (weaponInstance != null)
            {
                Destroy(weaponInstance);
                weaponInstance = null;
                weaponComponent = null;
            }
        }

        /// <summary>
        /// Unequips the current movement ability.
        /// </summary>
        public void UnequipCurrentMovementAbility()
        {
            if (movementAbilityInstance != null)
            {
                Destroy(movementAbilityInstance);
                movementAbilityInstance = null;
                movementAbilityComponent = null;
            }
        }

        /// <summary>
        /// Unequips both weapon and movement ability.
        /// </summary>
        public void UnequipAll()
        {
            UnequipCurrentWeapon();
            UnequipCurrentMovementAbility();
        }

        private void SpawnWeapon()
        {
            if (currentWeaponMask == null || currentWeaponMask.weaponPrefab == null)
                return;

            weaponInstance = Instantiate(currentWeaponMask.weaponPrefab, weaponSpawnPoint.position, weaponSpawnPoint.rotation, weaponSpawnPoint);
            weaponComponent = weaponInstance.GetComponent<BaseWeapon>();

            if (weaponComponent == null)
            {
                Debug.LogError($"MaskManager: Weapon prefab '{currentWeaponMask.weaponPrefab.name}' does not have a BaseWeapon component!");
                Destroy(weaponInstance);
                return;
            }

            // Set the owner to this player
            weaponComponent.SetOwner(gameObject);
        }

        private void SpawnMovementAbility()
        {
            if (currentMovementMask == null || currentMovementMask.movementAbilityPrefab == null)
                return;

            movementAbilityInstance = Instantiate(currentMovementMask.movementAbilityPrefab, movementAbilitySpawnPoint.position, movementAbilitySpawnPoint.rotation, movementAbilitySpawnPoint);
            movementAbilityComponent = movementAbilityInstance.GetComponent<BaseAbility>();

            if (movementAbilityComponent == null)
            {
                Debug.LogError($"MaskManager: Movement ability prefab '{currentMovementMask.movementAbilityPrefab.name}' does not have a BaseAbility component!");
                Destroy(movementAbilityInstance);
                return;
            }
        }

        /// <summary>
        /// Fires the weapon's primary attack.
        /// </summary>
        public bool FireWeapon()
        {
            if (weaponComponent == null)
                return false;
            if (playerDamagable != null && !playerDamagable.IsAlive)
                return false;

            Vector3 origin = weaponSpawnPoint.position;
            Vector3 direction = GetTargetDirection();

            bool fired = weaponComponent.Fire(origin, direction);

            if (showDebugLogs && fired)
            {
                Debug.Log($"MaskManager: Fired weapon (Cooldown: {weaponComponent.CooldownRemaining}s)");
            }

            return fired;
        }

        /// <summary>
        /// Uses the weapon's fire ability (combat ability).
        /// </summary>
        public bool UseCombatAbility()
        {
            if (weaponComponent == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("MaskManager: Cannot use combat ability - no weapon equipped!");
                return false;
            }
            if (playerDamagable != null && !playerDamagable.IsAlive)
                return false;

            if (weaponComponent.FireAbility == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"MaskManager: Cannot use combat ability - weapon '{weaponComponent.name}' has no fireAbility assigned!");
                return false;
            }

            Vector3 targetPosition = GetTargetPosition();
            bool used = weaponComponent.TryUseFireAbility(targetPosition);

            if (showDebugLogs)
            {
                if (used)
                    Debug.Log($"MaskManager: Used combat ability '{weaponComponent.FireAbility.GetType().Name}' (Target: {targetPosition})");
                else
                    Debug.LogWarning($"MaskManager: Failed to use combat ability '{weaponComponent.FireAbility.GetType().Name}' - ability on cooldown or cannot be used");
            }

            return used;
        }

        /// <summary>
        /// Uses the weapon's alt-fire ability.
        /// </summary>
        public bool UseAltCombatAbility()
        {
            if (weaponComponent == null)
                return false;
            if (playerDamagable != null && !playerDamagable.IsAlive)
                return false;

            Vector3 targetPosition = GetTargetPosition();
            bool used = weaponComponent.TryUseAltFireAbility(targetPosition);

            if (showDebugLogs && used)
            {
                Debug.Log($"MaskManager: Used alt combat ability (Target: {targetPosition})");
            }

            return used;
        }

        /// <summary>
        /// Cancels the weapon's primary fire ability (for channeled abilities like ice breath on left click).
        /// </summary>
        public void CancelCombatAbility()
        {
            if (weaponComponent == null || weaponComponent.FireAbility == null)
                return;
            if (!weaponComponent.FireAbility.IsChanneled)
                return;
            weaponComponent.FireAbility.Cancel();
            if (showDebugLogs)
                Debug.Log("MaskManager: Cancelled combat ability");
        }

        /// <summary>
        /// Cancels the weapon's alt-fire ability (for channeled abilities).
        /// </summary>
        public void CancelAltCombatAbility()
        {
            if (weaponComponent == null || weaponComponent.AltFireAbility == null)
                return;

            weaponComponent.AltFireAbility.Cancel();

            if (showDebugLogs)
            {
                Debug.Log("MaskManager: Cancelled alt combat ability");
            }
        }

        /// <summary>
        /// Gets the current alt combat ability.
        /// </summary>
        public BaseAbility CurrentAltCombatAbility => weaponComponent?.AltFireAbility;

        /// <summary>
        /// Uses the movement ability.
        /// </summary>
        public bool UseMovementAbility()
        {
            if (movementAbilityComponent == null || !movementAbilityComponent.CanUse)
                return false;

            Vector3 targetPosition = GetTargetPosition();
            bool used = movementAbilityComponent.Use(gameObject, targetPosition);

            if (showDebugLogs && used)
            {
                Debug.Log($"MaskManager: Used movement ability (Cooldown: {movementAbilityComponent.CooldownRemaining}s, Target: {targetPosition})");
            }

            return used;
        }

        /// <summary>
        /// Begins charging the weapon if it supports charging.
        /// </summary>
        public void BeginChargeWeapon()
        {
            if (weaponComponent == null)
                return;

            Vector3 origin = weaponSpawnPoint.position;
            Vector3 direction = GetTargetDirection();

            weaponComponent.BeginCharge(origin, direction);
        }

        /// <summary>
        /// Releases the weapon charge and fires.
        /// </summary>
        public bool ReleaseChargeWeapon()
        {
            if (weaponComponent == null)
                return false;

            return weaponComponent.ReleaseCharge();
        }

        /// <summary>
        /// Cancels weapon charge without firing.
        /// </summary>
        public void CancelChargeWeapon()
        {
            if (weaponComponent == null)
                return;

            weaponComponent.CancelCharge();
        }

        /// <summary>
        /// Gets the target position based on camera aim (crosshair or mouse).
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            if (mainCamera == null)
                mainCamera = targetingCamera != null ? targetingCamera : Camera.main;

            if (mainCamera != null)
            {
                Ray ray;

                if (useCrosshairAiming)
                {
                    // Raycast from center of screen (where crosshair would be)
                    ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                }
                else
                {
                    // Raycast from mouse position
                    ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                }

                if (Physics.Raycast(ray, out RaycastHit hit, maxTargetDistance, targetingRaycastMask))
                {
                    return hit.point;
                }

                // No hit - return point at default distance along the ray
                return ray.origin + ray.direction * defaultTargetDistance;
            }

            // Fallback to forward direction if no camera
            return transform.position + transform.forward * defaultTargetDistance;
        }

        /// <summary>
        /// Gets the target direction based on mouse raycast or forward direction.
        /// </summary>
        private Vector3 GetTargetDirection()
        {
            Vector3 targetPosition = GetTargetPosition();
            return (targetPosition - weaponSpawnPoint.position).normalized;
        }

        // Public getters for UI/other systems
        public Mask CurrentWeaponMask => currentWeaponMask;
        public Mask CurrentMovementMask => currentMovementMask;
        public BaseWeapon CurrentWeapon => weaponComponent;
        public BaseAbility CurrentMovementAbility => movementAbilityComponent;
        public bool HasWeaponMask => currentWeaponMask != null;
        public bool HasMovementMask => currentMovementMask != null;
        public bool CanUseCombatAbility => weaponComponent != null;
        public bool CanUseMovementAbility => movementAbilityComponent != null && movementAbilityComponent.CanUse;

        /// <summary>Gets the array of available masks.</summary>
        public Mask[] AvailableMasks => availableMasks;

        /// <summary>Gets the current weapon mask index.</summary>
        public int WeaponMaskIndex => weaponMaskIndex;

        /// <summary>Gets the current movement mask index.</summary>
        public int MovementMaskIndex => movementMaskIndex;

        /// <summary>Gets the number of available masks.</summary>
        public int MaskCount => availableMasks != null ? availableMasks.Length : 0;

        /// <summary>
        /// Gets the element type of the currently equipped combat ability.
        /// Returns Air if no combat ability is equipped.
        /// </summary>
        public Element.Element GetCombatElementType()
        {
            if (weaponComponent != null && weaponComponent.FireAbility != null)
                return weaponComponent.FireAbility.ElementType;
            return Element.Element.Air;
        }

        /// <summary>
        /// Gets the element type of the currently equipped movement ability.
        /// Returns Air if no movement ability is equipped.
        /// </summary>
        public Element.Element GetMovementElementType()
        {
            if (movementAbilityComponent != null)
                return movementAbilityComponent.ElementType;
            return Element.Element.Air;
        }

        /// <summary>
        /// Gets a formatted string for UI display showing both element types.
        /// Example: "Fire | Ice" or "Air | Earth"
        /// </summary>
        public string GetElementDisplayString()
        {
            Element.Element combat = GetCombatElementType();
            Element.Element movement = GetMovementElementType();
            return $"{combat} | {movement}";
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showDebugLogs)
                return;

            // Draw targeting line (weapon aim: spawn point → crosshair target)
            Gizmos.color = Color.yellow;
            Vector3 targetPos = GetTargetPosition();
            Vector3 lineStart = weaponSpawnPoint != null ? weaponSpawnPoint.position : transform.position;
            Gizmos.DrawLine(lineStart, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.5f);

            // Draw weapon spawn point
            if (weaponSpawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(weaponSpawnPoint.position, 0.2f);
            }

            // Draw movement ability spawn point
            if (movementAbilitySpawnPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(movementAbilitySpawnPoint.position, 0.2f);
            }
        }
    }
}
