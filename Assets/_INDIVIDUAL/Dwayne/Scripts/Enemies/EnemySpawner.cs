using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Enemies
{
    /// <summary>
    /// Generic EnemySpawner stub for editor tools compatibility.
    /// Replace with your actual implementation.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public float spawnInterval = 2f;
        public float spawnDistance = 20f;
        public int maxEnemies = 50;

        [Header("Current State")]
        public int currentEnemyCount = 0;
        public bool isSpawning = true;

        private float nextSpawnTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isSpawning) return;
            int cap = GameManager.Instance != null ? GameManager.Instance.GetMaxEnemies() : maxEnemies;
            if (currentEnemyCount >= cap || (GameManager.Instance != null && GameManager.Instance.GetCurrentEnemyCount() >= cap)) return;
            if (Time.time < nextSpawnTime) return;

            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        public void SpawnEnemy()
        {
            if (enemyPrefabs.Count == 0) return;

            Vector3 spawnPosition = GetSpawnPosition();
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

            Instantiate(prefab, spawnPosition, Quaternion.identity);
            currentEnemyCount++;
            GameManager.Instance?.NotifyEnemySpawned();
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePosition = transform != null ? transform.position : Vector3.zero;
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;
            return basePosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        public void OnEnemyDied()
        {
            currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        public void StartSpawning()
        {
            isSpawning = true;
        }
    }
}
