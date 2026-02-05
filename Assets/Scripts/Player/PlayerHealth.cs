using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private float respawnDelay = 0.5f;
    private float lastContactDamageTime = -999f;
    private float contactDamageCooldown = 2f;
    public static PlayerHealth Instance;

    protected override void Awake()
    {
        base.Awake();

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

        OnDied += HandlePlayerDeath;
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

    private void HandlePlayerDeath()
    {
        Debug.Log("Player died");
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        // health
        IsDead = false;
        SetHealth(MaxHealth);

        RespawnManager.Instance.RespawnPlayer(gameObject, transform.position);
    }
}
