using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    [SerializeField] private int damage = 10;

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
