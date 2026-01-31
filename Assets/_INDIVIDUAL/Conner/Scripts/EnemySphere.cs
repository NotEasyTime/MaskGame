using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemySphere : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth;

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

    [Header("Movement")]
    [SerializeField] private float downForce = 20f;

    [Header("Slime Pop")]
    [SerializeField] private float popHeight = 1.2f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float splitSpreadRadius = 2f;

    private NavMeshAgent agent;
    private Renderer rend;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();

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
    
    private void Start()
    {
        ApplyColorShift();
    }
    
    private void ApplyColorShift()
    {
        if (rend != null)
        {

            Color baseColor = rend.material.color;
            float tintProgress = 1f - ((float)splitDepth / 3f);
            rend.material.color = Color.Lerp(baseColor, Color.white, tintProgress * 0.8f);
        }
    }

    private void Update()
    {
        if (agent != null && agent.isOnNavMesh && player != null)
        {
            agent.SetDestination(player.position);
        }

        // PRESS K TO KILL
        if(Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999);
        }
    }

    // =====================
    // DAMAGE / DEATH
    // =====================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        currentHealth -= damage;

        // Apply knockback
        if (knockbackDistance > 0)
        {
            Vector3 knockbackDir = (transform.position - damageSourcePosition).normalized;
            knockbackDir.y = 0; // keep on ground
            Vector3 targetPos = transform.position + knockbackDir * knockbackDistance;
            StartCoroutine(KnockbackEffect(targetPos));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (splitDepth > 0 && splitPrefab != null)
        {
            Split();
        }

        Destroy(gameObject);
    }

    // =====================
    // SPLITTING
    // =====================

    private void Split()
    {
        for (int i = 0; i < splitCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * splitSpreadRadius;
            offset.y = 0f;

            Vector3 targetPos = transform.position + offset;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                GameObject newSphere = Instantiate(
                    splitPrefab,
                    transform.position,
                    Quaternion.identity
                );

                EnemySphere sphere = newSphere.GetComponent<EnemySphere>();
                NavMeshAgent newAgent = newSphere.GetComponent<NavMeshAgent>();

                if (sphere != null)
                {
                    sphere.splitDepth = splitDepth - 1;
                    sphere.player = player;
                }

                // Scale BEFORE movement
                newSphere.transform.localScale = transform.localScale * sizeMultiplier;

                if (newAgent != null)
                {
                    newAgent.Warp(transform.position);
                    newAgent.ResetPath();
                }

                // Slime hop animation
                sphere.StartCoroutine(
                    sphere.SlimePop(newSphere.transform, transform.position, hit.position)
                );
            }
        }
    }

    // =====================
    // SLIME POP (NO PHYSICS)
    // =====================

    private IEnumerator SlimePop(Transform slime, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * popHeight;

            slime.position = pos;
            yield return null;
        }

        slime.position = end;
    }

    // =====================
    // KNOCKBACK EFFECT
    // =====================

    private IEnumerator KnockbackEffect(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        if (agent != null)
        {
            agent.enabled = false; // temporarily disable NavMeshAgent
        }

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        if (agent != null)
        {
            agent.enabled = true; // re-enable NavMeshAgent
        }
    }

    // =====================
    // PLAYER CONTACT DAMAGE
    // =====================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage);
            }

            // Knock sphere back away from player
            if (knockbackDistance > 0)
            {
                Vector3 knockbackDir = (transform.position - other.transform.position).normalized;
                knockbackDir.y = 0;
                Vector3 targetPos = transform.position + knockbackDir * knockbackDistance;
                StartCoroutine(KnockbackEffect(targetPos));
            }
        }
    }
}
