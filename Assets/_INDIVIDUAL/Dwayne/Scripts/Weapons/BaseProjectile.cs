using Dwayne.Interfaces;
using UnityEngine;
using Pool;

namespace Dwayne.Weapons
{
    /// <summary>
    /// Base class for projectiles implementing IProjectile and IPoolable.
    /// Handles movement, lifetime, and hit/expire; subclasses can override for homing, piercing, etc.
    /// Automatically uses ObjectPoolManager for pooling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BaseProjectile : MonoBehaviour, IProjectile, IPoolable
    {
        [Header("Projectile")]
        [SerializeField] protected float defaultSpeed = 50f;
        [SerializeField] protected float maxLifetime = 5f;
        [SerializeField] protected LayerMask hitMask = ~0;
        [SerializeField] protected float gravity = 0f;

        protected float damage;
        protected float speed;
        protected Vector3 direction;
        protected GameObject owner;
        protected float spawnTime;
        protected Rigidbody rb;
        protected bool launched;

        public virtual float Damage => damage;
        public virtual float MaxLifetime => maxLifetime;
        public virtual LayerMask HitMask => hitMask;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        protected virtual void OnEnable()
        {
            launched = false;
        }

        /// <summary>
        /// Called when projectile is spawned from pool.
        /// Override to add custom reset logic.
        /// </summary>
        public virtual void OnSpawnFromPool()
        {
            launched = false;
            spawnTime = 0f;
            damage = 0f;
            speed = defaultSpeed;
            direction = Vector3.forward;
            owner = null;
        }

        /// <summary>
        /// Called when projectile is returned to pool.
        /// Override to add custom cleanup logic.
        /// </summary>
        public virtual void OnReturnToPool()
        {
            launched = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public virtual void Launch(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner = null)
        {
            this.damage = damage;
            this.speed = speed > 0 ? speed : defaultSpeed;
            this.direction = direction.normalized;
            this.owner = owner;
            spawnTime = Time.time;
            launched = true;

            transform.position = origin;
            transform.forward = this.direction;
            if (rb != null)
                rb.linearVelocity = this.direction * this.speed;
        }

        protected virtual void Update()
        {
            if (!launched)
                return;

            if (Time.time - spawnTime >= maxLifetime)
            {
                OnExpire();
                return;
            }

            if (gravity != 0f && rb != null)
                rb.linearVelocity += Vector3.down * (gravity * Time.deltaTime);
        }

        /// <summary>
        /// Call this from OnTriggerEnter/OnCollisionEnter with the collision data to trigger OnHit.
        /// </summary>
        protected void TryHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Skip owner
            if (other.gameObject == owner)
                return;

            // Check layer mask
            if (((1 << other.gameObject.layer) & hitMask) == 0)
                return;

            OnHit(other, hitPoint, hitNormal);
        }

        /// <summary>
        /// Return this projectile to the pool.
        /// Always use this instead of Destroy().
        /// </summary>
        protected virtual void ReturnToPool()
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError($"BaseProjectile: ObjectPoolManager not found! Cannot return projectile '{name}' to pool.");
                Destroy(gameObject);
                return;
            }

            launched = false;
            ObjectPoolManager.ReturnToPool(gameObject);
        }

        public abstract void OnHit(Collider other, Vector3 point, Vector3 normal);
        public abstract void OnExpire();
    }
}
