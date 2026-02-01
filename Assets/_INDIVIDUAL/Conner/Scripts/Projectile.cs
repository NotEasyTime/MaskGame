using UnityEngine;
using Dwayne.Interfaces;
using Interfaces;

public class Projectile : MonoBehaviour, IProjectile
{
    [Header("Projectile")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float defaultDamage = 10f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private LayerMask hitMask = ~0;

    private float damage;
    private float speed;
    private Vector3 direction;
    private GameObject owner;
    private float spawnTime;
    private bool launched;

    public float Damage => damage;
    public float MaxLifetime => maxLifetime;
    public LayerMask HitMask => hitMask;

    public void Launch(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner = null)
    {
        transform.position = origin;
        transform.rotation = Quaternion.LookRotation(direction);
        this.direction = direction.normalized;
        this.speed = speed > 0f ? speed : defaultSpeed;
        this.damage = damage > 0f ? damage : defaultDamage;
        this.owner = owner;
        spawnTime = Time.time;
        launched = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (!launched)
            return;

        transform.position += direction * speed * Time.deltaTime;

        if (Time.time - spawnTime >= maxLifetime)
        {
            OnExpire();
        }
    }

    public void OnHit(Collider other, Vector3 point, Vector3 normal)
    {
        var damageable = other.GetComponent<IDamagable>();
        if (damageable != null && damageable.IsAlive && damage > 0f)
        {
            damageable.TakeDamage(damage, point, -normal, owner != null ? owner : gameObject);
        }

        Destroy(gameObject);
    }

    public void OnExpire()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!launched || collision.contacts.Length == 0)
            return;

        ContactPoint contact = collision.contacts[0];
        OnHit(collision.collider, contact.point, contact.normal);
    }
}
