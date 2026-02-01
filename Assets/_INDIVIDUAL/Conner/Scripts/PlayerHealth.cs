using System;
using Dwayne.Masks;
using UnityEngine;
using Interfaces;
using Managers;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Audio")]
    [Tooltip("Play when player takes damage")]
    [SerializeField] private AudioClip hitReceivedSound;

    // IDamagable
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;
    
    public MaskManager maskManager;

    public event Action<float, Vector3, object> OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, object source = null)
    {
        //Debug.Log("TAKE DAMAGE CALLEDCALLED");
        //if (!IsAlive || amount <= 0f) return 0f;
        
     
        maskManager.TakeDamage(amount, hitPoint, hitDirection, source);
        Debug.Log("MASK SHOULD HAVE BEEN CALLED");
       

        float actualDamage = Mathf.Min(amount, currentHealth);

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
