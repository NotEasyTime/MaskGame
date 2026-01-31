using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Pool
{
    #region Enums

    /// <summary>
    /// Represents the current state of a poolable resource.
    /// Used to track resource lifecycle without allocations.
    /// </summary>
    public enum ResourceState : byte
    {
        Uninitialized = 0,
        Available = 1,
        InUse = 2,
        Disposed = 3
    }

    #endregion

    #region Core Interfaces

    /// <summary>
    /// Core interface for poolable non-GameObject resources.
    /// Supports deterministic cleanup and pool-aware lifecycle management.
    /// Designed for NativeArrays, ComputeBuffers, Materials, RenderTextures, etc.
    /// </summary>
    /// <typeparam name="T">The type of resource being pooled</typeparam>
    public interface IPoolableResource<T> where T : class
    {
        /// <summary>
        /// The underlying resource instance.
        /// May be null if resource has been disposed.
        /// </summary>
        T Resource { get; }

        /// <summary>
        /// Current state of the resource in the pool lifecycle.
        /// </summary>
        ResourceState State { get; }

        /// <summary>
        /// Unique identifier for this resource instance within the pool.
        /// Used for tracking and debugging. Set by the pool.
        /// </summary>
        int PoolInstanceId { get; set; }

        /// <summary>
        /// Called when resource is retrieved from the pool for use.
        /// Reset resource state, clear previous data, prepare for new use.
        /// MUST NOT allocate memory in this method for hot path performance.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called when resource is returned to the pool.
        /// Release references, clear temporary data, prepare for reuse.
        /// MUST NOT allocate memory in this method for hot path performance.
        /// </summary>
        void OnReturn();

        /// <summary>
        /// Final cleanup when pool is destroyed or resource is permanently removed.
        /// Release all managed and unmanaged resources (IDisposable pattern).
        /// This is the only place where resource destruction should occur.
        /// </summary>
        void OnDispose();

        /// <summary>
        /// Check if the resource is still valid and usable.
        /// Used by pool to verify resource integrity before spawning.
        /// </summary>
        /// <returns>True if resource is valid, false if it should be disposed and recreated</returns>
        bool IsValid();
    }

    /// <summary>
    /// Extended lifecycle hooks for resources that need additional control.
    /// Optional interface - implement only if needed for specific resource types.
    /// </summary>
    public interface IPoolableResourceLifecycle
    {
        /// <summary>
        /// Called during pool initialization or when pool pre-warms resources.
        /// Use for one-time expensive initialization that should happen before gameplay.
        /// </summary>
        /// <param name="config">Configuration data for prewarming</param>
        void OnPrewarm(in ResourcePrewarmConfig config);

        /// <summary>
        /// Called when the pool expands and creates new resources.
        /// Useful for batch initialization or resource grouping.
        /// </summary>
        /// <param name="expansionInfo">Information about the expansion event</param>
        void OnPoolExpand(in PoolExpansionInfo expansionInfo);

        /// <summary>
        /// Called before OnSpawn to allow early rejection or state preparation.
        /// Return false to indicate this resource should not be spawned.
        /// </summary>
        /// <returns>True if spawn should proceed, false to skip this resource</returns>
        bool OnPreSpawn();

        /// <summary>
        /// Called after OnReturn to perform deferred cleanup operations.
        /// Runs after the resource is safely back in the pool.
        /// </summary>
        void OnPostReturn();
    }

    /// <summary>
    /// Validation support for poolable resources.
    /// Useful for editor-time validation and runtime integrity checks.
    /// </summary>
    public interface IPoolableResourceValidation
    {
        /// <summary>
        /// Validate resource configuration and state.
        /// Called by pool during integrity checks or in editor.
        /// </summary>
        /// <param name="errors">List to append validation errors to (avoids allocation)</param>
        /// <returns>True if resource passes validation</returns>
        bool OnValidate(List<string> errors);

        /// <summary>
        /// Perform runtime integrity check without side effects.
        /// Should be fast enough for periodic runtime checks.
        /// </summary>
        /// <returns>True if resource integrity is maintained</returns>
        bool CheckIntegrity();
    }

    #endregion

    #region Hot/Cold Data Interfaces

    /// <summary>
    /// Interface for hot data - frequently accessed per-frame data.
    /// Implementations should be structs for cache-friendly iteration.
    /// Store in contiguous arrays for optimal CPU cache utilization.
    /// </summary>
    /// <typeparam name="T">The hot data struct type</typeparam>
    public interface IHotData<T> where T : struct
    {
        /// <summary>
        /// The hot data value. Direct access for performance.
        /// </summary>
        ref T Data { get; }

        /// <summary>
        /// Reset hot data to default state without allocation.
        /// Called on spawn/return for quick state reset.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Interface for cold data - rarely accessed metadata and configuration.
    /// Implementations are classes, accessed only on initialization/validation.
    /// </summary>
    /// <typeparam name="T">The cold data class type</typeparam>
    public interface IColdData<T> where T : class
    {
        /// <summary>
        /// The cold data reference. Accessed infrequently.
        /// </summary>
        T Metadata { get; }

        /// <summary>
        /// Initialize cold data with configuration.
        /// Called once during resource creation or pool prewarm.
        /// </summary>
        void Initialize(T config);
    }

    #endregion

    #region Supporting Structs

    /// <summary>
    /// Configuration passed during resource prewarming.
    /// Readonly struct to avoid allocation during pool initialization.
    /// </summary>
    public readonly struct ResourcePrewarmConfig
    {
        public readonly int BatchIndex;
        public readonly int BatchSize;
        public readonly int TotalPrewarmCount;
        public readonly bool IsEditorPrewarm;

        public ResourcePrewarmConfig(int batchIndex, int batchSize, int totalPrewarmCount, bool isEditorPrewarm)
        {
            BatchIndex = batchIndex;
            BatchSize = batchSize;
            TotalPrewarmCount = totalPrewarmCount;
            IsEditorPrewarm = isEditorPrewarm;
        }
    }

    /// <summary>
    /// Information about pool expansion events.
    /// Readonly struct to avoid allocation.
    /// </summary>
    public readonly struct PoolExpansionInfo
    {
        public readonly int PreviousCapacity;
        public readonly int NewCapacity;
        public readonly int ExpansionAmount;

        public PoolExpansionInfo(int previousCapacity, int newCapacity)
        {
            PreviousCapacity = previousCapacity;
            NewCapacity = newCapacity;
            ExpansionAmount = newCapacity - previousCapacity;
        }
    }

    /// <summary>
    /// Example hot data struct for compute resources.
    /// Packed for optimal cache line usage.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ResourceHotData
    {
        public int Width;
        public int Height;
        public int Depth;
        public int Stride;
        public int ElementCount;
        public int LastFrameUsed;
        public ResourceState State;
        private byte _padding1;
        private byte _padding2;
        private byte _padding3;
    }

    /// <summary>
    /// Example cold data for resource metadata.
    /// Contains strings and configuration that rarely changes.
    /// </summary>
    public class ResourceColdData
    {
        public string ResourceName;
        public string PoolName;
        public DateTime CreationTime;
        public int CreationFrame;
        public string DebugInfo;
        public Dictionary<string, object> CustomMetadata;
    }

    #endregion

    #region Pool Interface

    /// <summary>
    /// Generic pool interface for non-GameObject resources.
    /// Mirrors ObjectPool API but for arbitrary resource types.
    /// </summary>
    /// <typeparam name="TResource">The poolable resource type</typeparam>
    public interface IResourcePool<TResource> where TResource : class, IPoolableResource<TResource>
    {
        /// <summary>
        /// Pool identifier for lookup and debugging.
        /// </summary>
        string PoolId { get; }

        /// <summary>
        /// Total number of resources in pool (available + in use).
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Number of resources currently in use.
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// Number of resources available for spawning.
        /// </summary>
        int AvailableCount { get; }

        /// <summary>
        /// Get a resource from the pool.
        /// Returns null if pool is exhausted and cannot expand.
        /// </summary>
        TResource Get();

        /// <summary>
        /// Return a resource to the pool.
        /// </summary>
        /// <param name="resource">Resource to return</param>
        void Return(TResource resource);

        /// <summary>
        /// Pre-create resources at initialization.
        /// </summary>
        /// <param name="count">Number of resources to prewarm</param>
        void Prewarm(int count);

        /// <summary>
        /// Expand pool capacity.
        /// </summary>
        /// <param name="count">Number of resources to add</param>
        void Expand(int count);

        /// <summary>
        /// Return all active resources to pool.
        /// </summary>
        void ReturnAll();

        /// <summary>
        /// Dispose all resources and clear pool.
        /// </summary>
        void Clear();
    }

    #endregion
}
