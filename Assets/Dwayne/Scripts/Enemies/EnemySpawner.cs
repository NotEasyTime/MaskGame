using System.Collections.Generic;
using UnityEngine;

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
        private Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (!isSpawning || currentEnemyCount >= maxEnemies) return;
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
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePosition = playerTransform != null ? playerTransform.position : Vector3.zero;
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
