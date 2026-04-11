using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private float respawnDelay = 0.5f;
    [SerializeField] private HurtFlash hurtFlash;
    [SerializeField] private float flashMinInterval = 0.12f;

    private float lastContactDamageTime = -999f;
    private float lastHazardDamageTime = -999f;
    private float contactDamageCooldown = 2f;
    private float hazardDamageCooldown = 3f;

    private float lastFlashTime = -999f;
    private float previousHealth;

    public static PlayerHealth Instance;

    protected override void Awake()
    {
        base.Awake();

        if (hurtFlash == null)
        {
            hurtFlash = GetComponent<HurtFlash>();
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        previousHealth = CurrentHealth;

        OnHealthChanged += HandleHealthChanged;
        OnDied += HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        OnHealthChanged -= HandleHealthChanged;
        OnDied -= HandlePlayerDeath;
    }

    private void HandleHealthChanged()
    {
        if (!IsDead && CurrentHealth < previousHealth)
        {
            if (hurtFlash != null && Time.time - lastFlashTime >= flashMinInterval)
            {
                lastFlashTime = Time.time;
                hurtFlash.PlayFlash();
            }
        }

        previousHealth = CurrentHealth;
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

    private void Respawn()
    {
        IsDead = false;
        SetHealth(MaxHealth);
        previousHealth = CurrentHealth;

        RespawnManager.Instance.RespawnPlayer(gameObject, transform.position);
    }
}