using System;
using UnityEngine;

public abstract class Health : MonoBehaviour
{
    public event Action OnHealthChanged;
    public event Action OnDied;
    [SerializeField] private float maxHealth = 100f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; protected set; }
    public bool IsDead { get; protected set; }

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
        NotifyHealthChanged();
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damage <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Debug.Log(gameObject + "take damage: " + damage);

        NotifyHealthChanged();

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        if (amount <= 0f) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        NotifyHealthChanged();
    }

    protected void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, MaxHealth);
        NotifyHealthChanged();
    }

    protected virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;
        OnDied?.Invoke();
    }

    protected void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }
}
