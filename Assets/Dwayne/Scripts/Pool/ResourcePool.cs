using UnityEngine;
using System;
using System.Collections.Generic;
using Pool;

namespace Pool
{
    /// <summary>
    /// Generic object pool for reusable non-GameObject resources.
    /// Eliminates allocation overhead for performance-critical systems.
    /// Mirrors ObjectPool API but works with arbitrary resource types.
    /// </summary>
    /// <typeparam name="TResource">The poolable resource type</typeparam>
    public class ResourcePool<TResource> : IResourcePool<TResource>
        where TResource : class, IPoolableResource<TResource>
    {
        private readonly Func<TResource> factory;
        private readonly int maxSize;
        private readonly bool allowExpansion;

        // Pool storage
        private Queue<TResource> availableResources;
        private HashSet<TResource> activeResources;
        private List<TResource> allResources;

        // Instance ID counter
        private int nextInstanceId;

        // Stats
        public int TotalCount => allResources.Count;
        public int ActiveCount => activeResources.Count;
        public int AvailableCount => availableResources.Count;
        public string PoolId { get; private set; }

        /// <summary>
        /// Create a new resource pool.
        /// </summary>
        /// <param name="poolId">Unique identifier for this pool</param>
        /// <param name="factory">Factory function to create new resources</param>
        /// <param name="initialCapacity">Number of resources to prewarm</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="allowExpansion">Allow pool to grow beyond initial capacity</param>
        public ResourcePool(string poolId, Func<TResource> factory, int initialCapacity = 0, int maxSize = 0, bool allowExpansion = true)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory), "ResourcePool: Factory function cannot be null!");
            }

            this.PoolId = string.IsNullOrEmpty(poolId) ? $"ResourcePool_{Guid.NewGuid():N}" : poolId;
            this.factory = factory;
            this.maxSize = maxSize;
            this.allowExpansion = allowExpansion;
            this.nextInstanceId = 0;

            availableResources = new Queue<TResource>();
            activeResources = new HashSet<TResource>();
            allResources = new List<TResource>();

            // Pre-create resources
            if (initialCapacity > 0)
            {
                Prewarm(initialCapacity);
            }
        }

        /// <summary>
        /// Pre-create resources at initialization.
        /// </summary>
        public void Prewarm(int count)
        {
            int actualCount = count;
            if (maxSize > 0 && allResources.Count + count > maxSize)
            {
                actualCount = maxSize - allResources.Count;
            }

            if (actualCount <= 0)
            {
                return;
            }

            bool isEditor = Application.isEditor && !Application.isPlaying;

            for (int i = 0; i < actualCount; i++)
            {
                TResource resource = CreateNewResource();

                // Call prewarm lifecycle hook if supported
                if (resource is IPoolableResourceLifecycle lifecycle)
                {
                    var config = new ResourcePrewarmConfig(i, actualCount, count, isEditor);
                    lifecycle.OnPrewarm(in config);
                }
            }

            Debug.Log($"ResourcePool [{PoolId}]: Prewarmed {actualCount} resources");
        }

        /// <summary>
        /// Get a resource from the pool.
        /// </summary>
        public TResource Get()
        {
            TResource resource = null;

            // Try to get from available resources
            while (availableResources.Count > 0)
            {
                resource = availableResources.Dequeue();

                // Validate resource
                if (resource == null || !resource.IsValid())
                {
                    // Resource is invalid, remove from all and try next
                    if (resource != null)
                    {
                        resource.OnDispose();
                        allResources.Remove(resource);
                    }
                    resource = null;
                    continue;
                }

                // Check pre-spawn hook
                if (resource is IPoolableResourceLifecycle lifecycle)
                {
                    if (!lifecycle.OnPreSpawn())
                    {
                        // Resource rejected spawn, return to available and try next
                        availableResources.Enqueue(resource);
                        resource = null;
                        continue;
                    }
                }

                break;
            }

            // No available resource found, try to create new
            if (resource == null)
            {
                if (!allowExpansion || (maxSize > 0 && allResources.Count >= maxSize))
                {
                    Debug.LogWarning($"ResourcePool [{PoolId}]: Pool exhausted! (Max: {maxSize}, Active: {activeResources.Count})");
                    return null;
                }

                resource = CreateNewResource();

                // Dequeue immediately since we just added it
                availableResources.Dequeue();
            }

            // Activate resource
            resource.OnSpawn();
            activeResources.Add(resource);

            return resource;
        }

        /// <summary>
        /// Return a resource to the pool.
        /// </summary>
        public void Return(TResource resource)
        {
            if (resource == null)
            {
                Debug.LogWarning($"ResourcePool [{PoolId}]: Attempting to return null resource!");
                return;
            }

            if (!activeResources.Contains(resource))
            {
                Debug.LogWarning($"ResourcePool [{PoolId}]: Resource not from this pool! (ID: {resource.PoolInstanceId})");
                return;
            }

            // Call return lifecycle hook
            resource.OnReturn();

            // Remove from active, add to available
            activeResources.Remove(resource);
            availableResources.Enqueue(resource);

            // Call post-return hook if supported
            if (resource is IPoolableResourceLifecycle lifecycle)
            {
                lifecycle.OnPostReturn();
            }
        }

        /// <summary>
        /// Expand pool by creating more resources.
        /// </summary>
        public void Expand(int count)
        {
            if (count <= 0)
            {
                return;
            }

            int previousCapacity = allResources.Count;
            int actualCount = count;

            if (maxSize > 0 && allResources.Count + count > maxSize)
            {
                actualCount = maxSize - allResources.Count;
                if (actualCount <= 0)
                {
                    Debug.LogWarning($"ResourcePool [{PoolId}]: Cannot expand, at max size!");
                    return;
                }
            }

            for (int i = 0; i < actualCount; i++)
            {
                TResource resource = CreateNewResource();

                // Call expansion lifecycle hook if supported
                if (resource is IPoolableResourceLifecycle lifecycle)
                {
                    var info = new PoolExpansionInfo(previousCapacity, allResources.Count);
                    lifecycle.OnPoolExpand(in info);
                }
            }

            Debug.Log($"ResourcePool [{PoolId}]: Expanded by {actualCount} resources (Total: {allResources.Count})");
        }

        /// <summary>
        /// Return all active resources to pool.
        /// </summary>
        public void ReturnAll()
        {
            // Create copy to avoid modification during iteration
            var activeList = new List<TResource>(activeResources);

            foreach (TResource resource in activeList)
            {
                if (resource != null)
                {
                    Return(resource);
                }
            }

            Debug.Log($"ResourcePool [{PoolId}]: Returned all active resources ({activeList.Count})");
        }

        /// <summary>
        /// Dispose all resources and clear pool.
        /// </summary>
        public void Clear()
        {
            foreach (TResource resource in allResources)
            {
                if (resource != null)
                {
                    resource.OnDispose();
                }
            }

            availableResources.Clear();
            activeResources.Clear();
            allResources.Clear();

            Debug.Log($"ResourcePool [{PoolId}]: Cleared and disposed all resources");
        }

        /// <summary>
        /// Check if resource belongs to this pool.
        /// </summary>
        public bool Contains(TResource resource)
        {
            return allResources.Contains(resource);
        }

        /// <summary>
        /// Create a new resource and add to pool.
        /// </summary>
        private TResource CreateNewResource()
        {
            TResource resource = factory();

            if (resource == null)
            {
                Debug.LogError($"ResourcePool [{PoolId}]: Factory returned null!");
                return null;
            }

            resource.PoolInstanceId = nextInstanceId++;

            allResources.Add(resource);
            availableResources.Enqueue(resource);

            return resource;
        }
    }
}
