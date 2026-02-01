using UnityEngine;
using UnityEngine.AI;
using System; // Required for Action
using Interfaces;

public class EnemyCube : MonoBehaviour, IDamagable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float currentHealth;

    // IDamagable Implementation Properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    // IDamagable Events
    public event Action<float, Vector3, object> OnDamaged;
    public event Action OnDeath;

    [Header("AI")]
    [SerializeField] private Transform player;
    [SerializeField] private float fireRate = 1f; 
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f; 
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.5f, 1f); 

    private NavMeshAgent agent;
    private float fireCooldown = 0f;
    private bool playerInRange = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Update()
    {
        // Don't process logic if dead or missing components
        if (!IsAlive || player == null || agent == null || !agent.isOnNavMesh)
            return;

        if (playerInRange)
        {
            agent.isStopped = true;
            RotateTowards(player.position);
            Shoot();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            RotateTowards(agent.steeringTarget); 
        }

        // DEBUG KILL
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999, transform.position, Vector3.zero);
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
                Vector3 targetCenter = player.position + Vector3.up; 
                Vector3 fireDirection = (targetCenter - spawnPos).normalized;
                rb.linearVelocity = fireDirection * projectileSpeed;
            }

            fireCooldown = 1f / fireRate;
        }
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // Keep rotation upright
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    // ==========================================
    // IDAMAGABLE IMPLEMENTATION
    // ==========================================

    public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null)
    {
        if (!IsAlive) return 0;

        currentHealth -= amount;
        
        // Trigger the damaged event for UI or FX systems
        OnDamaged?.Invoke(amount, hitPoint, source);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
            Enemies.EnemySpawner.Instance.OnEnemyDied();
        }

        return amount;
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    // ===== TRIGGER DETECTION =====
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}