using System;
using UnityEngine;
using Interfaces;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    // IDamagable
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;

    public event Action<float, Vector3, object> OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null)
    {
        if (!IsAlive || amount <= 0f)
            return 0f;

        float actualDamage = Mathf.Min(amount, currentHealth);
        currentHealth -= actualDamage;

        Debug.Log($"Player took {actualDamage} damage. Current health: {currentHealth}");

        OnDamaged?.Invoke(actualDamage, hitPoint, source);

        if (currentHealth <= 0f)
        {
            Die();
        }

        return actualDamage;
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnDeath?.Invoke();

        if (Managers.GameManager.Instance != null)
            Managers.GameManager.Instance.OnPlayerDeath();
    }

    public int GetCurrentHealth() => (int)currentHealth;
    public int GetMaxHealth() => (int)maxHealth;
}
