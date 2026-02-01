using UnityEngine;
using Dwayne.Abilities;

namespace Dwayne.Testing
{
    /// <summary>
    /// Simple test spawner for abilities. Assign an ability prefab and press the test key to spawn and use it.
    /// Supports both regular abilities and projectile abilities.
    /// </summary>
    public class AbilityTestSpawner : MonoBehaviour
    {
        [Header("Ability Setup")]
        [Tooltip("The ability prefab to spawn and test")]
        [SerializeField] private GameObject abilityPrefab;

        [Header("Test Settings")]
        [Tooltip("Key to press to spawn and use the ability")]
        [SerializeField] private KeyCode testKey = KeyCode.T;

        [Tooltip("The user who will use the ability (usually the player)")]
        [SerializeField] private GameObject user;

        [Tooltip("Use mouse raycast for target position, otherwise use forward direction")]
        [SerializeField] private bool useMouseTarget = true;

        [Tooltip("Layer mask for mouse raycast")]
        [SerializeField] private LayerMask raycastMask = ~0;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private GameObject spawnedAbility;
        private BaseAbility abilityComponent;

        void Start()
        {
            // If no user is set, try to find the player
            if (user == null)
            {
                user = GameObject.FindGameObjectWithTag("Player");
                if (user == null)
                    user = this.gameObject;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                SpawnAndUseAbility();
            }
        }

        /// <summary>
        /// Spawns the ability and immediately uses it.
        /// </summary>
        public void SpawnAndUseAbility()
        {
            if (abilityPrefab == null)
            {
                Debug.LogError("AbilityTestSpawner: No ability prefab assigned!");
                return;
            }

            if (user == null)
            {
                Debug.LogError("AbilityTestSpawner: No user assigned!");
                return;
            }

            // Spawn the ability
            SpawnAbility();

            // Use the ability (works for both regular and projectile abilities)
            if (abilityComponent != null)
            {
                Vector3 targetPosition = GetTargetPosition();
                bool success = abilityComponent.Use(user, targetPosition);

                if (showDebugLogs)
                {
                    string abilityType = abilityComponent is ProjectileAbility ? "Projectile Ability" : "Ability";
                    Debug.Log($"{abilityType} used: {success} | {abilityComponent.GetType().Name} | Cooldown: {abilityComponent.CooldownDuration}s | Target: {targetPosition}");
                }
            }
        }

        /// <summary>
        /// Spawns the ability GameObject and gets the BaseAbility component.
        /// </summary>
        private void SpawnAbility()
        {
            // Destroy previous if exists
            if (spawnedAbility != null)
            {
                Destroy(spawnedAbility);
            }

            // Spawn new ability
            spawnedAbility = Instantiate(abilityPrefab, user.transform.position, Quaternion.identity);

            // Get the BaseAbility component
            abilityComponent = spawnedAbility.GetComponent<BaseAbility>();

            if (abilityComponent == null)
            {
                Debug.LogError($"AbilityTestSpawner: Ability prefab '{abilityPrefab.name}' does not have a BaseAbility component!");
            }
            else if (showDebugLogs)
            {
                string abilityType = abilityComponent is ProjectileAbility ? "Projectile Ability" : "Ability";
                Debug.Log($"Spawned {abilityType}: {abilityComponent.GetType().Name} (Element: {abilityComponent.ElementType})");
            }
        }

        /// <summary>
        /// Gets the target position based on mouse raycast or forward direction.
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            if (useMouseTarget)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastMask))
                {
                    return hit.point;
                }
            }

            // Fallback to forward direction
            return user.transform.position + user.transform.forward * 10f;
        }

        void OnDrawGizmos()
        {
            if (user != null && Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 targetPos = GetTargetPosition();
                Gizmos.DrawLine(user.transform.position, targetPos);
                Gizmos.DrawWireSphere(targetPos, 0.5f);
            }
        }
    }
}
