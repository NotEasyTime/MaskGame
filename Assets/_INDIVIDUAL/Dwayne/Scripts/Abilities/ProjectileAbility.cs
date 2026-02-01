using UnityEngine;
using Dwayne.Interfaces;
using Interfaces;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Base class for abilities that are also projectiles.
    /// Combines ability behavior (DoUse) with projectile movement and impact.
    /// The ability flies through space and triggers its effect on impact.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ProjectileAbility : BaseAbility, IProjectile
    {
        [Header("Projectile")]
        [SerializeField] protected float defaultSpeed = 20f;
        [SerializeField] protected float maxLifetime = 5f;
        [SerializeField] protected float gravity = 0f;
        [SerializeField] protected LayerMask projectileHitMask = ~0;

        [Header("Projectile Behavior")]
        [Tooltip("Destroy projectile on hit")]
        [SerializeField] protected bool destroyOnHit = true;

        [Tooltip("Destroy projectile on expire")]
        [SerializeField] protected bool destroyOnExpire = true;

        protected float damage;
        protected float speed;
        protected Vector3 direction;
        protected GameObject projectileOwner;
        protected float spawnTime;
        protected Rigidbody rb;
        protected bool launched;
    

        public virtual float Damage => damage;
        public virtual float MaxLifetime => maxLifetime;
        public virtual LayerMask HitMask => projectileHitMask;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // We handle gravity manually
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        protected virtual void OnEnable()
        {
            launched = false;
        }

        /// <summary>
        /// Launch the projectile ability from origin along direction.
        /// </summary>
        public virtual void Launch(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner = null)
        {
            this.damage = damage;
            this.speed = speed > 0 ? speed : defaultSpeed;
            this.direction = direction.normalized;
            this.projectileOwner = owner;
            this.spawnTime = Time.time;
            this.launched = true;

            transform.position = origin;
            transform.forward = this.direction;

            if (rb != null)
                rb.linearVelocity = this.direction * this.speed;

            // Spawn VFX when projectile launches
            if (projectileVFX != null)
            {
                GameObject vfx = SpawnVFX(projectileVFX, transform.position, transform.rotation);
                if (vfx != null)
                    vfx.transform.SetParent(transform); // Attach VFX to projectile
            }
        }

        protected virtual void Update()
        {
            if (!launched)
                return;

            // Check lifetime expiration
            if (Time.time - spawnTime >= maxLifetime)
            {
                OnExpire();
                return;
            }

            // Apply gravity
            if (gravity != 0f && rb != null)
            {
                rb.linearVelocity += Vector3.down * (gravity * Time.deltaTime);

                // Update forward direction to face velocity direction
                if (rb.linearVelocity.sqrMagnitude > 0.01f)
                    transform.forward = rb.linearVelocity.normalized;
            }
        }

        /// <summary>
        /// Called when projectile hits something. Triggers the ability effect.
        /// </summary>
        public virtual void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            if (!launched)
                return;

            // Spawn impact VFX
            SpawnImpactVFX(point, normal);

            // Trigger ability effect on hit
            // Use the projectile owner as the user, and hit point as target
            if (projectileOwner != null)
            {
                DoUse(projectileOwner, point);
            }

            // Apply damage to damageable targets
            var damageable = other.GetComponent<IDamagable>();
            if (damageable != null && damageable.IsAlive && damage > 0f)
            {
                damageable.TakeDamage(damage, point, -direction, projectileOwner);
            }

            // Destroy projectile on hit if configured
            if (destroyOnHit)
            {
                launched = false;
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when projectile expires without hitting anything.
        /// </summary>
        public virtual void OnExpire()
        {
            if (!launched)
                return;

            launched = false;

            if (destroyOnExpire)
                Destroy(gameObject);
        }

        /// <summary>
        /// Helper for OnTriggerEnter to check and trigger OnHit.
        /// </summary>
        protected void TryHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Ignore owner
            if (other.gameObject == projectileOwner)
                return;

            // Check layer mask
            if (((1 << other.gameObject.layer) & projectileHitMask) == 0)
                return;

            OnHit(other, hitPoint, hitNormal);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            TryHit(other, transform.position, -direction);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];
                TryHit(collision.collider, contact.point, contact.normal);
            }
        }
    }
}
