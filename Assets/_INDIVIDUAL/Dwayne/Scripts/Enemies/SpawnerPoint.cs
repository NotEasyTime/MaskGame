using UnityEngine;
using Managers;

namespace Enemies
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("Enemy prefab to spawn.")]
        public GameObject prefab;
        [Tooltip("Max of this enemy type from this spawn point. E.g. 20 = at most 20 Spheres. -1 = unlimited for this type.")]
        [Min(-1)]
        public int maxAmount = -1;
    }

    /// <summary>
    /// A spawn point that spawns from a configurable array of enemy prefabs, each with its own max amount.
    /// Respects GameManager for when spawning is allowed (game ready + under global enemy cap).
    /// </summary>
    public class SpawnerPoint : MonoBehaviour
    {
        [Header("Enemy Prefabs (per-index amount)")]
        [Tooltip("Each entry: prefab + max spawns for that type (-1 = no limit).")]
        public SpawnEntry[] spawnEntries = new SpawnEntry[0];

        [Header("Spawn Mode")]
        [Tooltip("If true, spawn once when game is ready. Otherwise spawn on an interval.")]
        public bool spawnOnceOnStart = false;

        [Header("Interval Spawn (when not spawn once)")]
        public float spawnInterval = 2f;
        [Tooltip("Cap on total enemies from this point (all types combined). Per-entry Max Amount still limits each type. -1 = no total cap (only per-type limits apply).")]
        public int maxSpawnedFromThisPoint = -1;

        [Header("Position")]
        [Tooltip("Random radius around this transform. 0 = exact position.")]
        public float spawnRadius = 0f;

        private float _nextSpawnTime;
        private int _totalSpawnedCount;
        private int[] _spawnedPerIndex;

        private void Start()
        {
            if (spawnEntries != null && spawnEntries.Length > 0)
                _spawnedPerIndex = new int[spawnEntries.Length];
        }

        private void Update()
        {
            if (!CanSpawnWithGameManager()) return;
            if (spawnEntries == null || spawnEntries.Length == 0) return;
            if (maxSpawnedFromThisPoint >= 0 && _totalSpawnedCount >= maxSpawnedFromThisPoint) return;
            if (!HasAnySlotAvailable()) return;

            if (spawnOnceOnStart)
            {
                if (SpawnOne())
                    enabled = false;
                return;
            }

            if (Time.time < _nextSpawnTime) return;
            SpawnOne();
            _nextSpawnTime = Time.time + spawnInterval;
        }

        /// <summary>
        /// Returns true when GameManager allows spawning (game ready and under global enemy cap).
        /// </summary>
        private bool CanSpawnWithGameManager()
        {
            if (GameManager.Instance == null) return false;
            if (!GameManager.IsGameReady) return false;
            if (GameManager.Instance.GetCurrentEnemyCount() >= GameManager.Instance.GetMaxEnemies()) return false;
            return true;
        }

        private bool HasAnySlotAvailable()
        {
            if (_spawnedPerIndex == null) return true;
            for (int i = 0; i < spawnEntries.Length; i++)
            {
                if (spawnEntries[i].prefab == null) continue;
                if (spawnEntries[i].maxAmount >= 0 && _spawnedPerIndex[i] >= spawnEntries[i].maxAmount) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Spawn a single enemy from the entries, respecting per-index max amounts and GameManager.
        /// Returns true if an enemy was spawned.
        /// </summary>
        public bool SpawnOne()
        {
            if (!CanSpawnWithGameManager()) return false;
            if (spawnEntries == null || spawnEntries.Length == 0) return false;

            int index = PickAvailableIndex();
            if (index < 0) return false;

            GameObject prefab = spawnEntries[index].prefab;
            if (prefab == null) return false;

            Vector3 pos = GetSpawnPosition();
            Instantiate(prefab, pos, Quaternion.identity);
            _totalSpawnedCount++;
            if (_spawnedPerIndex != null && index < _spawnedPerIndex.Length)
                _spawnedPerIndex[index]++;
            GameManager.Instance?.NotifyEnemySpawned();
            return true;
        }

        /// <summary>
        /// Picks a random index that still has spawns left (respects maxAmount per entry).
        /// </summary>
        private int PickAvailableIndex()
        {
            int count = 0;
            for (int i = 0; i < spawnEntries.Length; i++)
            {
                if (spawnEntries[i].prefab == null) continue;
                if (spawnEntries[i].maxAmount >= 0 && _spawnedPerIndex != null && i < _spawnedPerIndex.Length && _spawnedPerIndex[i] >= spawnEntries[i].maxAmount)
                    continue;
                count++;
            }
            if (count == 0) return -1;

            int pick = Random.Range(0, count);
            for (int i = 0; i < spawnEntries.Length; i++)
            {
                if (spawnEntries[i].prefab == null) continue;
                if (spawnEntries[i].maxAmount >= 0 && _spawnedPerIndex != null && i < _spawnedPerIndex.Length && _spawnedPerIndex[i] >= spawnEntries[i].maxAmount)
                    continue;
                if (pick == 0) return i;
                pick--;
            }
            return -1;
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePos = transform.position;
            if (spawnRadius <= 0f) return basePos;
            Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
            return basePos + new Vector3(circle.x, 0f, circle.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius > 0f ? spawnRadius : 0.5f);
        }
    }
}
