using UnityEngine;
using System.Collections;

public class AOEDamage : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float damageRadius = 3f;

    private void Start()
    {
        // Damage player immediately if in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);
        
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Player"))
            {
                PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
                if (playerHealth != null && playerHealth.IsAlive)
                {
                    playerHealth.TakeDamage(damage, transform.position, Vector3.zero, null);
                }
            }
        }

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
}
