using Unity.Mathematics;
using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 0.5f;
    private Rigidbody2D rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(damage);
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player died");
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        // position
        transform.position = respawnPoint.position;

        // velocity
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // health
        CurrentHealth = MaxHealth;
        IsDead = false;
    }
}
