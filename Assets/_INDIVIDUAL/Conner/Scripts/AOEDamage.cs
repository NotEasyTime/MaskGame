using UnityEngine;
using System.Collections;

public class AOEDamage : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float lifetime = 2f;

    private void Start()
    {
        // Damage player immediately if in trigger
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>()?.radius ?? 1f);
        
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Player"))
            {
                PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
}
