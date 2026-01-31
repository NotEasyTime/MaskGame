using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Projectile mode for weapons.
    /// </summary>
    public enum ProjectileMode
    {
        Hitscan,
        Projectile,
        Hybrid
    }

    /// <summary>
    /// Generic Projectile stub for editor tools compatibility.
    /// Replace with your actual implementation.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float speed = 50f;
        public float damage = 10f;
        public float lifetime = 5f;
        public float gravity = 0f;
        public bool homing = false;
        public float homingStrength = 5f;
        public bool piercing = false;
        public int maxPierceCount = 3;

        private float spawnTime;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spawnTime = Time.time;
        }

        private void Start()
        {
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * speed;
            }
        }

        private void Update()
        {
            // Lifetime check
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Apply gravity
            if (gravity > 0 && rb != null)
            {
                rb.linearVelocity += Vector3.down * gravity * Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Add collision handling logic here
        }
    }

    /// <summary>
    /// Generic FirePointGizmo stub for visualizing weapon fire points in editor.
    /// </summary>
    public class FirePointGizmo : MonoBehaviour
    {
        public Color gizmoColor = Color.red;
        public float gizmoSize = 0.1f;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);
            Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
        }
    }
}
