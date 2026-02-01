using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System; // Required for Action
using Interfaces;
using Dwayne.Effects;
using Managers;

public class EnemyPyramid : MonoBehaviour, IDamagable
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
    [SerializeField] private float stopDistance = 3f;

    [Header("Explosion / Teleport")]
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private float countdownTime = 3f;

    [Header("Combat")]
    [SerializeField] private float knockbackDistance = 2f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private NavMeshAgent agent;
    private Renderer rend;
    private float baseSpeed;
    private SpeedEffect speedEffect;

    private bool isCountingDown = false;
    private float countdownTimer;

    private Color startColor = new Color(1f, 0.8f, 0f); // Dark yellow
    private Color endColor = Color.white; 

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();
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

    private void Start()
    {
        if (rend != null) rend.material.color = startColor;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.IsGameReady)
            return;
        // Stop logic if dead or missing player
        if (!IsAlive || player == null || agent == null || !agent.isOnNavMesh) return;

        // Apply speed effect modifier
        if (speedEffect != null && agent != null)
        {
            agent.speed = baseSpeed * speedEffect.GetMovementMultiplier();
        }

        float distance = Vector3.Distance(transform.position, player.position);

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
            countdownTimer -= Time.deltaTime;

            if (rend != null)
            {
                rend.material.color = Color.Lerp(endColor, startColor, countdownTimer / countdownTime);
            }

            if (countdownTimer <= 0f)
            {
                StartCoroutine(TeleportAndSpawn());
            }
        }

        // DEBUG KILL
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999, transform.position, Vector3.zero);
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
        yield return new WaitForSeconds(1f);

        // Safety check in case it was killed during the 1s delay
        if (!IsAlive) yield break;

        if (player != null)
        {
            // Use Warp for NavMesh-friendly teleportation
            if (agent != null) agent.Warp(player.position);
            else transform.position = player.position;
        }

        if (spawnPrefab != null)
        {
            Instantiate(spawnPrefab, transform.position, Quaternion.identity);
        }

        // We call Die() instead of just Destroy to trigger the OnDeath event
        Die();
    }

    // ==========================================
    // IDAMAGABLE IMPLEMENTATION
    // ==========================================

    public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null)
    {
        if (!IsAlive) return 0;

        currentHealth -= amount;
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
        StopAllCoroutines();
        Destroy(gameObject);
        Enemies.EnemySpawner.Instance.OnEnemyDied();
        Managers.GameManager.Instance.OnEnemyKilled();
    }
}