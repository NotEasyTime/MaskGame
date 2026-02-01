using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Managers;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Pool
{
    /// <summary>
    /// Central manager for all object pools in the game
    /// Singleton pattern for global access.
    /// Defers pool initialization when in a menu scene (no NavMesh) until a game scene loads.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

        [Header("Organization")]
        [SerializeField] private Transform poolsContainer; // Parent for all pool containers

        [Header("Debug")]
#pragma warning disable CS0414 // Field is assigned but never used - reserved for future debug UI implementation
        [SerializeField] private bool showDebugInfo = true;
#pragma warning restore CS0414
        [SerializeField] private bool logPoolOperations = false;

        // Runtime pools
        private Dictionary<string, ObjectPool> pools;
        private Dictionary<GameObject, string> objectToPoolName; // Track which pool an object belongs to
        private bool _poolsInitialized;
        private bool _deferPoolInit;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            string currentScene = SceneManager.GetActiveScene().name;
            if (GameManager.Instance != null && !GameManager.Instance.IsGameScene(currentScene))
            {
                _deferPoolInit = true;
                pools = new Dictionary<string, ObjectPool>();
                objectToPoolName = new Dictionary<GameObject, string>();
                SceneManager.sceneLoaded += OnSceneLoaded;
                return;
            }

            InitializePools();
            _poolsInitialized = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_deferPoolInit || _poolsInitialized) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsGameScene(scene.name)) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            _deferPoolInit = false;
            InitializePools();
            _poolsInitialized = true;
        }

        #region Initialization

        private void InitializePools()
        {
            pools = new Dictionary<string, ObjectPool>();
            objectToPoolName = new Dictionary<GameObject, string>();

            // Create pools container if not assigned
            if (poolsContainer == null)
            {
                GameObject containerObj = new GameObject("PoolsContainer");
                containerObj.transform.SetParent(transform);
                poolsContainer = containerObj.transform;
            }

            // Initialize pools from config
            foreach (PoolConfig config in poolConfigs)
            {
                if (!config.IsValid(out string error))
                {
                    Debug.LogError($"ObjectPoolManager: Invalid pool config - {error}");
                    continue;
                }

                CreatePool(config);
            }

            Debug.Log($"ObjectPoolManager: Initialized {pools.Count} pools");
        }

        private void CreatePool(PoolConfig config)
        {
            string poolName = config.GetPoolName();

            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' already exists! Skipping.");
                return;
            }

            // Create container for this pool if not provided
            Transform container = config.container;
            if (container == null)
            {
                GameObject containerObj = new GameObject($"{poolName}_Pool");
                containerObj.transform.SetParent(poolsContainer);
                container = containerObj.transform;
            }

            // Create pool
            ObjectPool pool = new ObjectPool(
                config.prefab,
                config.preloadCount,
                config.maxSize,
                config.allowExpansion,
                container
            );

            pools.Add(poolName, pool);

            if (logPoolOperations)
            {
                Debug.Log($"ObjectPoolManager: Created pool '{poolName}' (Preloaded: {config.preloadCount})");
            }
        }

        #endregion

        #region Get/Return Objects

        /// <summary>
        /// Get object from pool by pool name
        /// </summary>
        public GameObject Get(string poolName)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"ObjectPoolManager: Pool '{poolName}' not found!");
                return null;
            }

            GameObject obj = pools[poolName].Get();

            if (obj != null)
            {
                // Track which pool this object belongs to
                if (!objectToPoolName.ContainsKey(obj))
                {
                    objectToPoolName.Add(obj, poolName);
                }

                if (logPoolOperations)
                {
                    Debug.Log($"ObjectPoolManager: Got object from pool '{poolName}'");
                }
            }

            return obj;
        }

        /// <summary>
        /// Get object at specific position and rotation
        /// </summary>
        public GameObject Get(string poolName, Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get(poolName);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Return object to its pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("ObjectPoolManager: Cannot return null object!");
                return;
            }

            // Find which pool this object belongs to
            if (!objectToPoolName.TryGetValue(obj, out string poolName))
            {
                Debug.LogWarning($"ObjectPoolManager: Object '{obj.name}' not tracked! Cannot return to pool.");
                return;
            }

            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"ObjectPoolManager: Pool '{poolName}' not found!");
                return;
            }

            pools[poolName].Return(obj);

            if (logPoolOperations)
            {
                Debug.Log($"ObjectPoolManager: Returned object to pool '{poolName}'");
            }
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// Get a specific pool by name
        /// </summary>
        public ObjectPool GetPool(string poolName)
        {
            if (pools.TryGetValue(poolName, out ObjectPool pool))
            {
                return pool;
            }

            Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' not found!");
            return null;
        }

        /// <summary>
        /// Check if pool exists
        /// </summary>
        public bool HasPool(string poolName)
        {
            return pools.ContainsKey(poolName);
        }

        /// <summary>
        /// Create a pool at runtime
        /// </summary>
        public void CreatePoolRuntime(GameObject prefab, int preloadCount, int maxSize = 0, bool allowExpansion = true)
        {
            if (prefab == null)
            {
                Debug.LogError("ObjectPoolManager: Cannot create pool with null prefab!");
                return;
            }

            string poolName = prefab.name;

            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' already exists!");
                return;
            }

            PoolConfig config = new PoolConfig
            {
                prefab = prefab,
                poolName = poolName,
                preloadCount = preloadCount,
                maxSize = maxSize,
                allowExpansion = allowExpansion
            };

            CreatePool(config);
        }

        /// <summary>
        /// Expand a pool
        /// </summary>
        public void ExpandPool(string poolName, int count)
        {
            if (pools.TryGetValue(poolName, out ObjectPool pool))
            {
                pool.Expand(count);
            }
            else
            {
                Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' not found!");
            }
        }

        /// <summary>
        /// Return all active objects in a pool
        /// </summary>
        public void ReturnAllInPool(string poolName)
        {
            if (pools.TryGetValue(poolName, out ObjectPool pool))
            {
                pool.ReturnAll();
            }
            else
            {
                Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' not found!");
            }
        }

        /// <summary>
        /// Clear and destroy a pool
        /// </summary>
        public void ClearPool(string poolName)
        {
            if (pools.TryGetValue(poolName, out ObjectPool pool))
            {
                pool.Clear();
                pools.Remove(poolName);
            }
            else
            {
                Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' not found!");
            }
        }

        /// <summary>
        /// Clear and destroy all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }

            pools.Clear();
            objectToPoolName.Clear();

            Debug.Log("ObjectPoolManager: Cleared all pools");
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Static helper to get object from pool
        /// </summary>
        public static GameObject GetFromPool(string poolName)
        {
            if (Instance != null)
            {
                return Instance.Get(poolName);
            }

            Debug.LogError("ObjectPoolManager: No instance found!");
            return null;
        }

        /// <summary>
        /// Static helper to get object at position
        /// </summary>
        public static GameObject GetFromPool(string poolName, Vector3 position, Quaternion rotation)
        {
            if (Instance != null)
            {
                return Instance.Get(poolName, position, rotation);
            }

            Debug.LogError("ObjectPoolManager: No instance found!");
            return null;
        }

        /// <summary>
        /// Static helper to return object to pool
        /// </summary>
        public static void ReturnToPool(GameObject obj)
        {
            if (Instance != null)
            {
                Instance.Return(obj);
            }
            else
            {
                Debug.LogError("ObjectPoolManager: No instance found!");
            }
        }

        #endregion

        #region Debug UI - DISABLED for Canvas UI

        /*private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));

            GUILayout.Label("<b>Object Pools</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });

            foreach (var kvp in pools)
            {
                ObjectPool pool = kvp.Value;
                GUILayout.Label($"{kvp.Key}: {pool.ActiveCount}/{pool.TotalCount} active");
            }

            GUILayout.EndArea();
        }*/

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ClearAllPools();
        }

        #endregion
    }
}
