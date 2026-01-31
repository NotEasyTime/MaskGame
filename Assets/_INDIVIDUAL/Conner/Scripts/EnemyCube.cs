using UnityEngine;
using UnityEngine.AI;

public class EnemyCube : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth;

    [Header("AI")]
    [SerializeField] private Transform player;
    [SerializeField] private float fireRate = 1f; // shots per second
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f; // speed to rotate toward player
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.5f, 1f); // offset from cube center

    private NavMeshAgent agent;
    private float fireCooldown = 0f;
    private bool playerInRange = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        if (playerInRange)
        {
            // Player is in range → stop moving and rotate to face player
            agent.isStopped = true;
            RotateTowards(player.position);
            Shoot();
        }
        else
        {
            // Player out of range → chase player
            agent.isStopped = false;
            agent.SetDestination(player.position);
            RotateTowards(agent.steeringTarget); // smoothly face movement direction
        }

        // PRESS K TO KILL
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999);
        }
    }

    private void Shoot()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            Vector3 spawnPos = transform.position + transform.TransformDirection(spawnOffset);
            GameObject projectile = Instantiate(projectilePrefab, spawnPos, transform.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = transform.forward * projectileSpeed;
            }
            else
            {
                projectile.AddComponent<Projectile>().speed = projectileSpeed;
            }

            fireCooldown = 1f / fireRate;
        }
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // keep rotation horizontal
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
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

    // ===== TRIGGER DETECTION =====
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}