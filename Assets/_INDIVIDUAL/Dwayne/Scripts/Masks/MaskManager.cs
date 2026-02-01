using UnityEngine;
using Dwayne.Abilities;
using Dwayne.Weapons;
using Element;

namespace Dwayne.Masks
{
    /// <summary>
    /// Manages the player's current mask, including weapon (combat ability) and movement ability.
    /// Allows separate usage of combat and movement abilities.
    /// </summary>
    public class MaskManager : MonoBehaviour
    {
        [Header("Current Mask")]
        [SerializeField] private Mask currentMask;

        [Header("Spawn Points")]
        [Tooltip("Where to spawn the weapon (e.g., hand transform)")]
        [SerializeField] private Transform weaponSpawnPoint;

        [Tooltip("Where to spawn the movement ability (usually the player itself)")]
        [SerializeField] private Transform movementAbilitySpawnPoint;

        [Header("Targeting")]
        [Tooltip("Use mouse raycast for ability targeting")]
        [SerializeField] private bool useMouseTargeting = true;

        [Tooltip("Layer mask for mouse raycast")]
        [SerializeField] private LayerMask targetingRaycastMask = ~0;

        [Tooltip("Default targeting distance when mouse raycast fails")]
        [SerializeField] private float defaultTargetDistance = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Active instances
        private GameObject weaponInstance;
        private BaseWeapon weaponComponent;

        private GameObject movementAbilityInstance;
        private BaseAbility movementAbilityComponent;

        // Cached references
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;

            // Set spawn points to self if not assigned
            if (weaponSpawnPoint == null)
                weaponSpawnPoint = transform;

            if (movementAbilitySpawnPoint == null)
                movementAbilitySpawnPoint = transform;

            // Equip the initial mask if set
            if (currentMask != null)
                EquipMask(currentMask);
        }

        /// <summary>
        /// Equips a new mask, replacing the current weapon and movement ability.
        /// </summary>
        public void EquipMask(Mask mask)
        {
            if (mask == null || !mask.IsValid())
            {
                Debug.LogError("MaskManager: Cannot equip invalid mask!");
                return;
            }

            // Unequip current mask
            UnequipCurrentMask();

            currentMask = mask;

            // Spawn weapon
            SpawnWeapon();

            // Spawn movement ability
            SpawnMovementAbility();

            if (showDebugLogs)
            {
                Debug.Log($"MaskManager: Equipped mask '{mask.maskName}' (Weapon: {weaponComponent?.GetType().Name}, Movement: {movementAbilityComponent?.GetType().Name})");
            }
        }

        /// <summary>
        /// Unequips the current mask, destroying weapon and movement ability instances.
        /// </summary>
        public void UnequipCurrentMask()
        {
            if (weaponInstance != null)
            {
                Destroy(weaponInstance);
                weaponInstance = null;
                weaponComponent = null;
            }

            if (movementAbilityInstance != null)
            {
                Destroy(movementAbilityInstance);
                movementAbilityInstance = null;
                movementAbilityComponent = null;
            }
        }

        private void SpawnWeapon()
        {
            if (currentMask.weaponPrefab == null)
                return;

            weaponInstance = Instantiate(currentMask.weaponPrefab, weaponSpawnPoint.position, weaponSpawnPoint.rotation, weaponSpawnPoint);
            weaponComponent = weaponInstance.GetComponent<BaseWeapon>();

            if (weaponComponent == null)
            {
                Debug.LogError($"MaskManager: Weapon prefab '{currentMask.weaponPrefab.name}' does not have a BaseWeapon component!");
                Destroy(weaponInstance);
                return;
            }

            // Set the owner to this player
            weaponComponent.SetOwner(gameObject);
        }

        private void SpawnMovementAbility()
        {
            if (currentMask.movementAbilityPrefab == null)
                return;

            movementAbilityInstance = Instantiate(currentMask.movementAbilityPrefab, movementAbilitySpawnPoint.position, movementAbilitySpawnPoint.rotation, movementAbilitySpawnPoint);
            movementAbilityComponent = movementAbilityInstance.GetComponent<BaseAbility>();

            if (movementAbilityComponent == null)
            {
                Debug.LogError($"MaskManager: Movement ability prefab '{currentMask.movementAbilityPrefab.name}' does not have a BaseAbility component!");
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
                return false;

            Vector3 targetPosition = GetTargetPosition();
            bool used = weaponComponent.TryUseFireAbility(targetPosition);

            if (showDebugLogs && used)
            {
                Debug.Log($"MaskManager: Used combat ability (Target: {targetPosition})");
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

            Vector3 targetPosition = GetTargetPosition();
            bool used = weaponComponent.TryUseAltFireAbility(targetPosition);

            if (showDebugLogs && used)
            {
                Debug.Log($"MaskManager: Used alt combat ability (Target: {targetPosition})");
            }

            return used;
        }

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
        /// Gets the target position based on mouse raycast or forward direction.
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            if (useMouseTargeting && mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, targetingRaycastMask))
                {
                    return hit.point;
                }
            }

            // Fallback to forward direction
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
        public Mask CurrentMask => currentMask;
        public BaseWeapon CurrentWeapon => weaponComponent;
        public BaseAbility CurrentMovementAbility => movementAbilityComponent;
        public bool HasMask => currentMask != null;
        public bool CanUseCombatAbility => weaponComponent != null;
        public bool CanUseMovementAbility => movementAbilityComponent != null && movementAbilityComponent.CanUse;

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

            // Draw targeting line
            Gizmos.color = Color.yellow;
            Vector3 targetPos = GetTargetPosition();
            Gizmos.DrawLine(transform.position, targetPos);
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
