using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyPyramid : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("AI")]
    [SerializeField] private Transform player;
    [SerializeField] private float stopDistance = 3f;

    [Header("Explosion / Teleport")]
    [SerializeField] private GameObject spawnPrefab; // What spawns after death
    [SerializeField] private float countdownTime = 3f; // Time before teleport & spawn

    private NavMeshAgent agent;
    private Renderer rend;

    private bool isCountingDown = false;
    private float countdownTimer;

    private Color startColor = new Color(1f, 0.8f, 0f); // Dark yellow
    private Color endColor = Color.white; // White

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (rend != null)
        {
            rend.material.color = startColor;
        }
    }

    private void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Move towards player until within stopDistance
        if (!isCountingDown)
        {
            if (distance > stopDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                agent.isStopped = true;
                StartCountdown();
            }
        }
        else
        {
            // Countdown logic
            countdownTimer -= Time.deltaTime;

            // Lerp color from dark yellow to white
            if (rend != null)
            {
                rend.material.color = Color.Lerp(endColor, startColor, countdownTimer / countdownTime);
            }

            if (countdownTimer <= 0f)
            {
                StartCoroutine(TeleportAndSpawn());
            }
        }

        // Debug damage key
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999);
        }
    }

    private void StartCountdown()
    {
        if (!isCountingDown)
        {
            isCountingDown = true;
            countdownTimer = countdownTime;
        }
    }

    private IEnumerator TeleportAndSpawn()
    {
        isCountingDown = false;

        // Optional small delay before teleport
        yield return new WaitForSeconds(1f);

        // Teleport to player
        if (player != null)
        {
            transform.position = player.position;
        }

        // Spawn prefab
        if (spawnPrefab != null)
        {
            Instantiate(spawnPrefab, transform.position, Quaternion.identity);
        }

        // Destroy self
        Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
