using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    public GameObject enemyType1;
    public GameObject enemyType2;
    public GameObject enemyType3;

    [Header("Spawn Settings")]
    [Tooltip("Maximum number of enemies alive at once")]
    public int maxEnemies = 10;

    [Tooltip("Spawn radius around the player")]
    public float minSpawnDistance = 5f;
    public float maxSpawnDistance = 15f;

    [Tooltip("Time between spawn attempts in seconds")]
    public float spawnInterval = 2f;

    [Header("Spawn Chances (0 to 1)")]
    [Range(0f, 1f)]
    public float chanceEnemy1 = 0.4f;
    [Range(0f, 1f)]
    public float chanceEnemy2 = 0.3f;
    [Range(0f, 1f)]
    public float chanceEnemy3 = 0.3f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        InvokeRepeating(nameof(SpawnEnemy), 0f, spawnInterval);
    }

    private void SpawnEnemy()
    {
        spawnedEnemies.RemoveAll(e => e == null);

        if (spawnedEnemies.Count >= maxEnemies)
            return;

        float randomValue = Random.value; 
        GameObject enemyToSpawn;

        // By using a final 'else', we guarantee enemyType3 spawns 
        // if the value is above the first two thresholds.
        if (randomValue < chanceEnemy1)
        {
            enemyToSpawn = enemyType1;
        }
        else if (randomValue < (chanceEnemy1 + chanceEnemy2))
        {
            enemyToSpawn = enemyType2;
        }
        else 
        {
            enemyToSpawn = enemyType3;
        }

        if (enemyToSpawn == null) return;

        // Position logic...
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        // Use the player's position if this script is on the player, 
        // or a specific player reference.
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y) * distance;

        GameObject spawned = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
        spawnedEnemies.Add(spawned);
    }
}
