using UnityEngine;
using System.Collections.Generic;

namespace Pool
{
    /// <summary>
    /// Generic object pool for reusing GameObjects
    /// Eliminates Instantiate/Destroy overhead for performance
    /// 
    /// Cache-optimized with hot/cold split:
    /// - HOT: Dense arrays for per-frame operations (Get/Return)
    /// - COLD: Dictionary for occasional lookups (Contains/validation)
    /// </summary>
    public class ObjectPool
    {
        private GameObject prefab;
        private Transform container;
        private int maxSize;
        private bool allowExpansion;

        // HOT PATH - Dense arrays for cache locality
        private List<GameObject> allObjects;           // Dense array - sequential access
        private Queue<int> availableIndices;          // Indices instead of GameObject refs
        private bool[] activeFlags;                   // Dense bitmask - O(1) active check by index
        private int activeCount;                       // Cached count - no iteration needed

        // COLD PATH - Map for occasional lookups only
        private Dictionary<GameObject, int> objectToIndex;  // Only used for Return() validation

        // Stats
        public int TotalCount => allObjects.Count;
        public int ActiveCount => activeCount;
        public int AvailableCount => availableIndices.Count;
        public string PoolName { get; private set; }

        /// <summary>
        /// Create a new object pool
        /// </summary>
        public ObjectPool(GameObject prefab, int preloadCount, int maxSize = 0, bool allowExpansion = true, Transform container = null)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.allowExpansion = allowExpansion;
            this.container = container;

            PoolName = prefab != null ? prefab.name : "UnnamedPool";

            // Initialize hot path structures
            allObjects = new List<GameObject>();
            availableIndices = new Queue<int>();
            activeFlags = new bool[preloadCount > 0 ? preloadCount : 16]; // Start with reasonable size
            activeCount = 0;
            
            // Initialize cold path lookup
            objectToIndex = new Dictionary<GameObject, int>();

            // Pre-create objects
            if (preloadCount > 0)
            {
                Preload(preloadCount);
            }
        }

        /// <summary>
        /// Pre-create objects at initialization
        /// </summary>
        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewObject();
            }

            Debug.Log($"ObjectPool [{PoolName}]: Preloaded {count} objects");
        }

        /// <summary>
        /// Get an object from the pool
        /// HOT PATH - Uses dense array access for cache locality
        /// </summary>
        public GameObject Get()
        {
            int index;
            GameObject obj;

            // Check if we have available objects (HOT: Queue operation on indices)
            if (availableIndices.Count > 0)
            {
                index = availableIndices.Dequeue();
                obj = allObjects[index];
            }
            else
            {
                // Pool is empty
                if (!allowExpansion || (maxSize > 0 && allObjects.Count >= maxSize))
                {
                    Debug.LogWarning($"ObjectPool [{PoolName}]: Pool exhausted! (Max: {maxSize}, Active: {activeCount})");
                    return null;
                }

                // Create new object
                obj = CreateNewObject();
                index = allObjects.Count - 1; // New object is at the end
            }

            // Activate object (HOT: Dense array write)
            obj.SetActive(true);
            SetActiveFlag(index, true);
            activeCount++;

            // Call lifecycle hook
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }

            return obj;
        }

        /// <summary>
        /// Get object at specific position and rotation
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// Uses Dictionary lookup (COLD) but then dense array access (HOT)
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"ObjectPool [{PoolName}]: Attempting to return null object!");
                return;
            }

            // COLD PATH: Dictionary lookup to get index
            if (!objectToIndex.TryGetValue(obj, out int index))
            {
                Debug.LogWarning($"ObjectPool [{PoolName}]: Object '{obj.name}' not from this pool!");
                return;
            }

            // HOT PATH: Dense array check
            if (!activeFlags[index])
            {
                Debug.LogWarning($"ObjectPool [{PoolName}]: Object '{obj.name}' already returned to pool!");
                return;
            }

            // Call lifecycle hook
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }

            // Deactivate and return to pool (HOT: Dense array operations)
            obj.SetActive(false);
            SetActiveFlag(index, false);
            activeCount--;
            availableIndices.Enqueue(index);
        }

        /// <summary>
        /// Expand pool by creating more objects
        /// </summary>
        public void Expand(int count)
        {
            if (maxSize > 0 && allObjects.Count + count > maxSize)
            {
                count = maxSize - allObjects.Count;
                if (count <= 0)
                {
                    Debug.LogWarning($"ObjectPool [{PoolName}]: Cannot expand, at max size!");
                    return;
                }
            }

            for (int i = 0; i < count; i++)
            {
                CreateNewObject();
            }

            Debug.Log($"ObjectPool [{PoolName}]: Expanded by {count} objects (Total: {allObjects.Count})");
        }

        /// <summary>
        /// Return all active objects to pool
        /// HOT PATH: Iterates dense array sequentially
        /// </summary>
        public void ReturnAll()
        {
            int returned = 0;
            
            // Iterate dense array - cache friendly sequential access
            for (int i = 0; i < allObjects.Count; i++)
            {
                if (activeFlags[i] && allObjects[i] != null)
                {
                    Return(allObjects[i]);
                    returned++;
                }
            }

            Debug.Log($"ObjectPool [{PoolName}]: Returned all active objects ({returned})");
        }

        /// <summary>
        /// Clear and destroy all objects in pool
        /// </summary>
        public void Clear()
        {
            // Iterate dense array sequentially
            foreach (GameObject obj in allObjects)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            availableIndices.Clear();
            allObjects.Clear();
            objectToIndex.Clear();
            activeCount = 0;
            
            // Reset activeFlags array
            System.Array.Clear(activeFlags, 0, activeFlags.Length);

            Debug.Log($"ObjectPool [{PoolName}]: Cleared and destroyed all objects");
        }

        /// <summary>
        /// Create a new object and add to pool
        /// </summary>
        private GameObject CreateNewObject()
        {
            if (prefab == null)
            {
                Debug.LogError($"ObjectPool [{PoolName}]: Prefab is null!");
                return null;
            }

            GameObject obj = Object.Instantiate(prefab, container);
            obj.name = $"{prefab.name}_{allObjects.Count}";
            obj.SetActive(false);

            int index = allObjects.Count;
            allObjects.Add(obj);
            
            // Ensure activeFlags array is large enough
            if (index >= activeFlags.Length)
            {
                // Grow array by 1.5x (common growth factor)
                int newSize = activeFlags.Length + (activeFlags.Length >> 1);
                System.Array.Resize(ref activeFlags, newSize);
            }
            
            activeFlags[index] = false; // Start as inactive
            availableIndices.Enqueue(index);
            
            // COLD PATH: Register in lookup map
            objectToIndex[obj] = index;

            return obj;
        }

        /// <summary>
        /// Check if object belongs to this pool
        /// COLD PATH: Dictionary lookup (O(1) but cache miss)
        /// </summary>
        public bool Contains(GameObject obj)
        {
            return obj != null && objectToIndex.ContainsKey(obj);
        }
        
        /// <summary>
        /// Helper to set active flag with bounds checking
        /// </summary>
        private void SetActiveFlag(int index, bool active)
        {
            if (index >= 0 && index < activeFlags.Length)
            {
                activeFlags[index] = active;
            }
        }
    }

    /// <summary>
    /// Interface for objects that need lifecycle hooks
    /// Implement this on MonoBehaviours that use pooling
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when object is retrieved from pool
        /// Use this to reset state, re-enable components, etc.
        /// </summary>
        void OnSpawnFromPool();

        /// <summary>
        /// Called when object is returned to pool
        /// Use this to cleanup, stop coroutines, disable components, etc.
        /// </summary>
        void OnReturnToPool();
    }
}
