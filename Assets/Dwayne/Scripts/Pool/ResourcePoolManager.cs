using UnityEngine;
using System;
using System.Collections.Generic;
using Pool;

namespace Pool
{
    /// <summary>
    /// Central manager for all resource pools in the game.
    /// Singleton pattern for global access.
    /// Mirrors ObjectPoolManager but for non-GameObject resources.
    /// </summary>
    public class ResourcePoolManager : MonoBehaviour
    {
        public static ResourcePoolManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool logPoolOperations = false;

        // Runtime pools - stored as object to support generic types
        private Dictionary<string, object> pools;

        // Track pool types for cleanup
        private Dictionary<string, Type> poolTypes;

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

            InitializePools();
        }

        #region Initialization

        private void InitializePools()
        {
            pools = new Dictionary<string, object>();
            poolTypes = new Dictionary<string, Type>();

            Debug.Log("ResourcePoolManager: Initialized");
        }

        #endregion

        #region Pool Registration

        /// <summary>
        /// Register an existing pool with the manager.
        /// </summary>
        public void RegisterPool<TResource>(string poolId, ResourcePool<TResource> pool)
            where TResource : class, IPoolableResource<TResource>
        {
            if (string.IsNullOrEmpty(poolId))
            {
                Debug.LogError("ResourcePoolManager: Cannot register pool with null or empty ID!");
                return;
            }

            if (pool == null)
            {
                Debug.LogError($"ResourcePoolManager: Cannot register null pool '{poolId}'!");
                return;
            }

            if (pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"ResourcePoolManager: Pool '{poolId}' already registered! Skipping.");
                return;
            }

            pools.Add(poolId, pool);
            poolTypes.Add(poolId, typeof(TResource));

            if (logPoolOperations)
            {
                Debug.Log($"ResourcePoolManager: Registered pool '{poolId}'");
            }
        }

        /// <summary>
        /// Create and register a new pool.
        /// </summary>
        public ResourcePool<TResource> CreatePool<TResource>(
            string poolId,
            Func<TResource> factory,
            int initialCapacity = 0,
            int maxSize = 0,
            bool allowExpansion = true)
            where TResource : class, IPoolableResource<TResource>
        {
            if (pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"ResourcePoolManager: Pool '{poolId}' already exists!");
                return GetPool<TResource>(poolId);
            }

            var pool = new ResourcePool<TResource>(poolId, factory, initialCapacity, maxSize, allowExpansion);
            RegisterPool(poolId, pool);

            return pool;
        }

        #endregion

        #region Get/Return Resources

        /// <summary>
        /// Get a resource from a pool by pool ID.
        /// </summary>
        public TResource Get<TResource>(string poolId)
            where TResource : class, IPoolableResource<TResource>
        {
            var pool = GetPool<TResource>(poolId);
            if (pool == null)
            {
                return null;
            }

            TResource resource = pool.Get();

            if (resource != null && logPoolOperations)
            {
                Debug.Log($"ResourcePoolManager: Got resource from pool '{poolId}'");
            }

            return resource;
        }

        /// <summary>
        /// Return a resource to its pool.
        /// </summary>
        public void Return<TResource>(string poolId, TResource resource)
            where TResource : class, IPoolableResource<TResource>
        {
            if (resource == null)
            {
                Debug.LogWarning("ResourcePoolManager: Cannot return null resource!");
                return;
            }

            var pool = GetPool<TResource>(poolId);
            if (pool == null)
            {
                return;
            }

            pool.Return(resource);

            if (logPoolOperations)
            {
                Debug.Log($"ResourcePoolManager: Returned resource to pool '{poolId}'");
            }
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// Get a specific pool by ID.
        /// </summary>
        public ResourcePool<TResource> GetPool<TResource>(string poolId)
            where TResource : class, IPoolableResource<TResource>
        {
            if (!pools.TryGetValue(poolId, out object poolObj))
            {
                Debug.LogWarning($"ResourcePoolManager: Pool '{poolId}' not found!");
                return null;
            }

            if (poolObj is ResourcePool<TResource> typedPool)
            {
                return typedPool;
            }

            Debug.LogError($"ResourcePoolManager: Pool '{poolId}' is not of type ResourcePool<{typeof(TResource).Name}>!");
            return null;
        }

        /// <summary>
        /// Check if a pool exists.
        /// </summary>
        public bool HasPool(string poolId)
        {
            return pools.ContainsKey(poolId);
        }

        /// <summary>
        /// Expand a pool.
        /// </summary>
        public void ExpandPool<TResource>(string poolId, int count)
            where TResource : class, IPoolableResource<TResource>
        {
            var pool = GetPool<TResource>(poolId);
            pool?.Expand(count);
        }

        /// <summary>
        /// Return all active resources in a pool.
        /// </summary>
        public void ReturnAllInPool<TResource>(string poolId)
            where TResource : class, IPoolableResource<TResource>
        {
            var pool = GetPool<TResource>(poolId);
            pool?.ReturnAll();
        }

        /// <summary>
        /// Clear and dispose a specific pool.
        /// </summary>
        public void ClearPool(string poolId)
        {
            if (!pools.TryGetValue(poolId, out object poolObj))
            {
                Debug.LogWarning($"ResourcePoolManager: Pool '{poolId}' not found!");
                return;
            }

            // Use reflection to call Clear() since we don't know the generic type
            var clearMethod = poolObj.GetType().GetMethod("Clear");
            clearMethod?.Invoke(poolObj, null);

            pools.Remove(poolId);
            poolTypes.Remove(poolId);

            if (logPoolOperations)
            {
                Debug.Log($"ResourcePoolManager: Cleared pool '{poolId}'");
            }
        }

        /// <summary>
        /// Clear and dispose all pools.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in pools)
            {
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, null);
            }

            pools.Clear();
            poolTypes.Clear();

            Debug.Log("ResourcePoolManager: Cleared all pools");
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Static helper to get resource from pool.
        /// </summary>
        public static TResource GetFromPool<TResource>(string poolId)
            where TResource : class, IPoolableResource<TResource>
        {
            if (Instance != null)
            {
                return Instance.Get<TResource>(poolId);
            }

            Debug.LogError("ResourcePoolManager: No instance found!");
            return null;
        }

        /// <summary>
        /// Static helper to return resource to pool.
        /// </summary>
        public static void ReturnToPool<TResource>(string poolId, TResource resource)
            where TResource : class, IPoolableResource<TResource>
        {
            if (Instance != null)
            {
                Instance.Return(poolId, resource);
            }
            else
            {
                Debug.LogError("ResourcePoolManager: No instance found!");
            }
        }

        /// <summary>
        /// Static helper to check if pool exists.
        /// </summary>
        public static bool PoolExists(string poolId)
        {
            return Instance != null && Instance.HasPool(poolId);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            ClearAllPools();
        }

        #endregion
    }
}
