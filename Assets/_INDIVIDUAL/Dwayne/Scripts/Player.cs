using System;
using UnityEngine;
using Interfaces;
using Managers;

namespace Dwayne
{
    /// <summary>
    /// Player component that implements IDamagable so enemies can deal damage.
    /// Add this to the player GameObject.
    /// </summary>
    public class Player : MonoBehaviour, IDamagable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Invincibility")]
        [Tooltip("Brief invincibility after taking damage")]
        [SerializeField] private float invincibilityDuration = 0.5f;
        private float lastDamageTime = float.NegativeInfinity;

        [Header("Death")]
        [Tooltip("Delay before triggering death callbacks")]
        [SerializeField] private float deathDelay = 0f;

        // IDamagable implementation
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0f;

        public event Action<float, Vector3, object> OnDamaged;
        public event Action OnDeath;

        // Additional events for UI/effects
        public event Action<float, float> OnHealthChanged; // current, max

        private bool isDead = false;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null)
        {
            if (isDead || amount <= 0f)
                return 0f;

            // Check invincibility
            if (Time.time < lastDamageTime + invincibilityDuration)
                return 0f;

            // Calculate actual damage (clamped to current health)
            float actualDamage = Mathf.Min(amount, currentHealth);
            currentHealth -= actualDamage;
            lastDamageTime = Time.time;

            Debug.Log($"Player took {actualDamage} damage from {source}. Health: {currentHealth}/{maxHealth}");

            // Fire events
            OnDamaged?.Invoke(actualDamage, hitPoint, source);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Check for death
            if (currentHealth <= 0f && !isDead)
            {
                isDead = true;
                if (deathDelay > 0f)
                    Invoke(nameof(Die), deathDelay);
                else
                    Die();
            }

            return actualDamage;
        }

        /// <summary>
        /// Simplified TakeDamage for basic damage without hit info.
        /// </summary>
        public float TakeDamage(float amount)
        {
            return TakeDamage(amount, transform.position, Vector3.zero, null);
        }

        private void Die()
        {
            Debug.Log("Player died!");
            OnDeath?.Invoke();

            // Notify GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDeath();
            }
        }

        /// <summary>
        /// Heal the player by the specified amount.
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log($"Player healed {amount}. Health: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Fully restore health.
        /// </summary>
        public void FullHeal()
        {
            Heal(maxHealth - currentHealth);
        }

        /// <summary>
        /// Reset player state (for respawning).
        /// </summary>
        public void Reset()
        {
            isDead = false;
            currentHealth = maxHealth;
            lastDamageTime = float.NegativeInfinity;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Set max health (useful for upgrades/buffs).
        /// </summary>
        public void SetMaxHealth(float newMax, bool healToFull = false)
        {
            maxHealth = newMax;
            if (healToFull)
                currentHealth = maxHealth;
            else
                currentHealth = Mathf.Min(currentHealth, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
