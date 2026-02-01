using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using Interfaces;

public class EnemySphere : MonoBehaviour, IDamagable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float currentHealth;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public event Action<float, Vector3, object> OnDamaged;
    public event Action OnDeath;

    [Header("Splitting")]
    [SerializeField] private GameObject splitPrefab;
    [SerializeField] private int splitCount = 2;
    [SerializeField] private int splitDepth = 2;
    [SerializeField] private float sizeMultiplier = 0.6f;

    [Header("AI")]
    [SerializeField] private Transform player;

    [Header("Combat")]
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float knockbackDistance = 2f;
    [SerializeField] private float knockbackDuration = 0.2f;
    
    [Header("Slime Pop")]
    [SerializeField] private float popHeight = 1.2f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float splitSpreadRadius = 2f;
    
    [SerializeField] private float teleportCheckHeight = 4f;
    [SerializeField] private float teleportRadius = 1.5f;
    [SerializeField] private float teleportCooldown = 1f;

    private float lastTeleportTime;


    private NavMeshAgent agent;
    private Renderer rend;

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

    private void Start() => ApplyColorShift();

    private void ApplyColorShift()
    {
        if (rend != null)
        {
            float tintProgress = 1f - ((float)splitDepth / 3f);
            rend.material.color = Color.Lerp(rend.material.color, Color.white, tintProgress * 0.8f);
        }
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh || player == null)
            return;

        agent.SetDestination(player.position);

        if (ShouldTeleport())
        {
            TryTeleport();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999, transform.position, Vector3.zero);
        }
    }
    
    bool ShouldTeleport()
    {
        if (Time.time - lastTeleportTime < teleportCooldown)
            return false;

        if (!agent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(player.position, path);

        return path.status != NavMeshPathStatus.PathComplete;
    }

    
    void TryTeleport()
    {
        if (NavMesh.SamplePosition(player.position, out NavMeshHit navHit, teleportRadius, NavMesh.AllAreas))
        {
            agent.enabled = false;
            agent.Warp(navHit.position);
            agent.enabled = true;

            lastTeleportTime = Time.time;
        }
    }
    



    // ==========================================
    // IDAMAGABLE IMPLEMENTATION
    // ==========================================

    public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null)
    {
        if (!IsAlive) return 0;

        currentHealth -= amount;
        
        // Trigger Event
        OnDamaged?.Invoke(amount, hitPoint, source);

        // Apply knockback using the hit direction provided by the interface
        if (knockbackDistance > 0)
        {
            Vector3 targetPos = transform.position + (hitDirection.normalized * knockbackDistance);
            targetPos.y = transform.position.y; // Keep level
            StartCoroutine(KnockbackEffect(targetPos));
        }

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

        if (splitDepth > 0 && splitPrefab != null)
        {
            Split();
        } 
        
        Enemies.EnemySpawner.Instance.OnEnemyDied();
        Managers.GameManager.Instance.OnEnemyKilled();

        Destroy(gameObject);

    }

    // =====================
    // SPLITTING LOGIC
    // =====================

    private void Split()
    {
        for (int i = 0; i < splitCount; i++)
        {
            Vector3 offset = UnityEngine.Random.insideUnitSphere * splitSpreadRadius;
            offset.y = 0f;
            Vector3 targetPos = transform.position + offset;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                GameObject newSphere = Instantiate(splitPrefab, transform.position, Quaternion.identity);
                EnemySphere sphere = newSphere.GetComponent<EnemySphere>();
                NavMeshAgent newAgent = newSphere.GetComponent<NavMeshAgent>();

                if (sphere != null)
                {
                    sphere.splitDepth = splitDepth - 1;
                    sphere.player = player;
                
                    // CRITICAL: Scale the agent properties too so small slimes move correctly
                    if (newAgent != null)
                    {
                        newAgent.radius *= sizeMultiplier;
                        newAgent.height *= sizeMultiplier;
                        // Disable agent during the "Pop" jump so it doesn't fight the transform
                        newAgent.enabled = false; 
                    }

                    newSphere.transform.localScale = transform.localScale * sizeMultiplier;
                
                    // Start the pop, passing the agent so we can re-enable it at the end
                    sphere.StartCoroutine(sphere.SlimePop(newSphere.transform, transform.position, hit.position, newAgent));
                }
            }
        }
    }

    private IEnumerator SlimePop(Transform slime, Vector3 start, Vector3 end, NavMeshAgent agentToEnable)
    {
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
        
            // Parabola jump logic
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * popHeight;
            slime.position = pos;
            yield return null;
        }

        slime.position = end;

        // RE-ENABLE AGENT HERE
        if (agentToEnable != null)
        {
            agentToEnable.enabled = true;
            // Warp ensures the agent's internal simulation position matches the visual transform
            agentToEnable.Warp(end); 
            if (player != null) agentToEnable.SetDestination(player.position);
        }
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(contactDamage);

            // Self-knockback when hitting player
            Vector3 pushDir = (transform.position - other.transform.position).normalized;
            StartCoroutine(KnockbackEffect(transform.position + pushDir * knockbackDistance));
        }
    }
}