using UnityEngine;
using UnityEngine.AI;
using System; // Required for Action
using System.Collections;
using Interfaces;
using Dwayne.Effects;
using Dwayne.Interfaces;
using Managers;

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

    [Header("Combat")]
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float knockbackDistance = 2f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private NavMeshAgent agent;
    private float fireCooldown = 0f;
    private bool playerInRange = false;
    private float baseSpeed;
    private SpeedEffect speedEffect;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        speedEffect = GetComponent<SpeedEffect>();

        if (agent != null)
        {
            baseSpeed = agent.speed;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Update()
    {
        // Wait until game is ready (player spawned, game scene initialized)
        if (GameManager.Instance == null || !GameManager.IsGameReady)
            return;
        // Don't process logic if dead or missing components
        if (!IsAlive || player == null || agent == null || !agent.isOnNavMesh)
            return;

        // Apply speed effect modifier
        if (speedEffect != null && agent != null)
        {
            agent.speed = baseSpeed * speedEffect.GetMovementMultiplier();
        }

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
            Vector3 targetCenter = player.position + Vector3.up;
            Vector3 fireDirection = (targetCenter - spawnPos).normalized;

            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(fireDirection));
            var projectile = projectileObj.GetComponent<IProjectile>();
            if (projectile != null)
            {
                projectile.Launch(spawnPos, fireDirection, projectileSpeed, projectileDamage, gameObject);
            }
            else
            {
                Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
                if (rb != null)
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

        // Apply knockback using the hit direction
        if (knockbackDistance > 0 && hitDirection != Vector3.zero)
        {
            Vector3 targetPos = transform.position + (hitDirection.normalized * knockbackDistance);
            targetPos.y = transform.position.y;
            StartCoroutine(KnockbackEffect(targetPos));
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
            Enemies.EnemySpawner.Instance.OnEnemyDied();
            Managers.GameManager.Instance.OnEnemyKilled();
        }

        return amount;
    }

    private IEnumerator KnockbackEffect(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        if (agent != null) agent.enabled = false;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.ResetPath();
        }
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