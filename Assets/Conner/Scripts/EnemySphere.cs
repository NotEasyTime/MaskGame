using UnityEngine;
using UnityEngine.AI;

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
    private NavMeshAgent agent;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (player != null && agent != null)
        {
            agent.SetDestination(player.position);
        }

        //PRESS K TO KILL
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999);

        }

        if (agent != null)
        {
            if (!agent.isOnNavMesh)
            {
                Debug.Log("NOT ON NAVMESH", this);
            }
        }

        if (player != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }
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
        if (splitDepth > 0 && splitPrefab != null)
        {
            Split();
        }

        Destroy(gameObject);
    }

    private void Split()
    {
        for (int i = 0; i < splitCount; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 2f;
            randomDirection.y = 0f;
            randomDirection += transform.position;

            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
            {
                GameObject newSphere = Instantiate(
                    splitPrefab,
                    hit.position,
                    Quaternion.identity
                );

                EnemySphere sphereScript = newSphere.GetComponent<EnemySphere>();

                if (sphereScript != null)
                {
                    sphereScript.splitDepth = splitDepth - 1;
                    sphereScript.player = player;
                }

                NavMeshAgent newAgent = newSphere.GetComponent<NavMeshAgent>();

                if (newAgent != null)
                {
                    newAgent.enabled = false; //disable BEFORE scaling, so important
                }

                //newSphere.transform.localScale = transform.localScale * sizeMultiplier;

                if (newAgent != null)
                {
                    newAgent.enabled = true;

                    //Force agent onto navmesh
                    newAgent.Warp(hit.position);

                    //Clear any broken paths
                    newAgent.ResetPath();

                    //make smaller ones slightly faster
                    newAgent.speed *= 1.15f;
                }
            }
            else
            {
                Debug.LogWarning("Failed to find NavMesh position for split!", this);
            }
        }
    }
}
