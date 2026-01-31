using UnityEngine;

namespace Pool
{
    /// <summary>
    /// Configuration for an object pool
    /// Serializable for inspector configuration
    /// </summary>
    [System.Serializable]
    public class PoolConfig
    {
        [Header("Pool Setup")]
        [Tooltip("Prefab to pool (must not be null)")]
        public GameObject prefab;

        [Tooltip("Optional custom name for the pool (auto-generated from prefab if empty)")]
        public string poolName = "";

        [Header("Pool Size")]
        [Tooltip("How many objects to create at startup")]
        [Range(0, 1000)]
        public int preloadCount = 50;

        [Tooltip("Maximum pool size (0 = unlimited)")]
        [Range(0, 10000)]
        public int maxSize = 200;

        [Tooltip("Allow pool to grow beyond preload count if needed")]
        public bool allowExpansion = true;

        [Header("Organization")]
        [Tooltip("Optional parent transform for pooled objects (keeps hierarchy clean)")]
        public Transform container;

        /// <summary>
        /// Get the pool name (from custom name or prefab name)
        /// </summary>
        public string GetPoolName()
        {
            if (!string.IsNullOrEmpty(poolName))
                return poolName;

            if (prefab != null)
                return prefab.name;

            return "UnnamedPool";
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (prefab == null)
            {
                errorMessage = "Prefab is null";
                return false;
            }

            if (preloadCount < 0)
            {
                errorMessage = "Preload count cannot be negative";
                return false;
            }

            if (maxSize < 0)
            {
                errorMessage = "Max size cannot be negative";
                return false;
            }

            if (maxSize > 0 && preloadCount > maxSize)
            {
                errorMessage = $"Preload count ({preloadCount}) exceeds max size ({maxSize})";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
}
