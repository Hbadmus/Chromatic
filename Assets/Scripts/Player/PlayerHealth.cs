using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float respawnDelay = 0.5f;
    private Rigidbody2D rb;
    private float lastContactDamageTime = -999f;
    private float contactDamageCooldown = 2f;
    public static PlayerHealth Instance;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        
        // to create the instance of the player health
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

public void Kill()
    {
        SetHealth(0f);
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player died");
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        // health
        SetHealth(MaxHealth);
        IsDead = false;

        RespawnManager.Instance.RespawnPlayer(gameObject, transform.position);
    }
}
