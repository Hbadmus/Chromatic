using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private float respawnDelay = 0.5f;

    private float lastContactDamageTime = -999f;
    private float lastHazardDamageTime = -999f;
    private float contactDamageCooldown = 2f;
    private float hazardDamageCooldown = 3f;

    public static PlayerHealth Instance;

    protected override void Awake()
    {
        base.Awake();

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
        TakeDamage(damage);
    }

    public void TakeHazardDamage(float damage)
    {
        if (Time.time - lastHazardDamageTime >= hazardDamageCooldown)
        {
            lastHazardDamageTime = Time.time;
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
        IsDead = false;
        SetHealth(MaxHealth);

        RespawnManager.Instance.RespawnPlayer(gameObject, transform.position);
    }
}