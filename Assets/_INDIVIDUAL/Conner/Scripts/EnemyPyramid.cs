using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System; // Required for Action
using Interfaces;

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

    private NavMeshAgent agent;
    private Renderer rend;

    private bool isCountingDown = false;
    private float countdownTimer;

    private Color startColor = new Color(1f, 0.8f, 0f); // Dark yellow
    private Color endColor = Color.white; 

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();
        
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
        // Stop logic if dead or missing player
        if (!IsAlive || player == null || agent == null || !agent.isOnNavMesh) return;

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

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        return amount;
    }

    private void Die()
    {
        OnDeath?.Invoke();
        StopAllCoroutines();
        Destroy(gameObject);
        Enemies.EnemySpawner.Instance.OnEnemyDied();
    }
}