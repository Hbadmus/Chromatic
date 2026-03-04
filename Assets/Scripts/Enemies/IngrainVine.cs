using UnityEngine;

public class IngrainVine : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;

    private float currentHealth;
    private GreenSentinelBoss boss;
    private SpriteRenderer sprite;

    private void Awake()
    {
        currentHealth = maxHealth;
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float t = Mathf.PingPong(Time.time * 0.5f, 1f);
        sprite.color = Color.Lerp(Color.gray, Color.green, t * 0.3f);
    }

    public void SetBoss(GreenSentinelBoss bossRef)
    {
        boss = bossRef;
    }

    public void OnHit(float damage, Color color)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            DestroyVine();
        }
    }

    private void DestroyVine()
    {
        if (boss != null)
        {
            boss.RemoveIngrainVine(gameObject);
        }

        Destroy(gameObject);
    }
}