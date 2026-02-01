using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 0.5f;

    private Rigidbody2D rb;
    private float lastContactDamageTime = -999f;
    private float contactDamageCooldown = 2f;
    public static PlayerHealth Instance;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TakeContactDamage(float damage)
    {
        if (Time.time - lastContactDamageTime >= contactDamageCooldown)
        {
            lastContactDamageTime = Time.time;
            TakeDamage(damage);
        }
    }

    public void TakeShockwaveDamage(float damage)
    {
        TakeDamage(damage);
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player died");
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        transform.position = respawnPoint.position;

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        SetHealth(MaxHealth);
        IsDead = false;
    }
}