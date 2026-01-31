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
}
