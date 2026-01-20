using UnityEngine;

public abstract class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    public bool IsDead { get; private set; }

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damage <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Debug.Log(gameObject + "take damage: " + damage);

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
    }

    protected virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;
    }
}
